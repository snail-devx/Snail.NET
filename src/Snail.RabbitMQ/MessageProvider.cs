using System.Net;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Snail.Abstractions;
using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Logging.Extensions;
using Snail.Abstractions.Setting.Extensions;
using Snail.Abstractions.Web.Interfaces;
using Snail.RabbitMQ.Components;
using Snail.RabbitMQ.Exceptions;
using Snail.Utilities.Common.Extensions;

namespace Snail.RabbitMQ
{
    /// <summary>
    /// <see cref="IMessageProvider"/> 消息提供程序RabbitMQ实现
    /// </summary>
    /// <remarks>作为默认实现类，并提供命名实现</remarks>
    [Component<IMessageProvider>(Lifetime = LifetimeType.Singleton)]
    [Component<IMessageProvider>(Lifetime = LifetimeType.Singleton, Key = DIKEY_RabbitMQ)]
    public class MessageProvider : IMessageProvider
    {
        #region 属性变量
        /// <summary>
        /// 应用程序实例
        /// </summary>
        private readonly IApplication _app;
        /// <summary>
        /// RabbitMQ管理器
        /// </summary>
        private readonly RabbitManager _manager;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        public MessageProvider(IApplication app)
        {
            _app = ThrowIfNull(app);
            _manager = app.ResolveRequired<RabbitManager>();
        }
        #endregion

        #region IMessageProvider
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="type">消息类型：如mq、pubsub</param>
        /// <param name="message">发送的消息数据</param>
        /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机等信息</param>
        /// <param name="server">消息服务器地址：消息发送哪里</param>
        /// <returns>处理使用成功，成功返回true，否则返回false</returns>
        async Task<bool> IMessageProvider.Send<T>(MessageType type, T message, IMessageOptions options, IServerOptions server)
        {
            ThrowIfNull(options);
            ThrowIfNull(server);
            //  发送消息到交换机：声明交换机和消息路由；若交换机为空则为默认交换机
            ChannelProxy? channel = null;
            try
            {
                channel = await _manager.GetChannel(isSend: true, server);
                channel.OnError += (title, reason) =>
                {
                    title = $"发送[{type.ToString()}]消息:{title}";
                    _manager.FileLogger.Error(title, $"{reason}{Environment.NewLine}\t{options.AsJson()}{Environment.NewLine}\t{server.ToString()}");
                };
                //  定义交换机，初始化路由
                string exchange = await DeclareExchange(channel.Object, type, options);
                string routing = GetRouting(options);
                //  发送消息
                //      组装消息body
                BasicProperties props = new BasicProperties()
                {
                    ContentType = "text/plain",/*内容类型：文本数据*/
                    ContentEncoding = "utf-8",
                    DeliveryMode = DeliveryModes.Persistent,/*交付模式：持久化*/
                };
                string dataStr = message?.AsJson() ?? "null";/*兼容发送消息为null的情况*/
                byte[] body = dataStr.AsBytes();
                await channel.Object.BasicPublishAsync(exchange, routing, mandatory: false, props, body);
            }
            finally
            {
                //  回收信道
                if (channel != null)
                {
                    (channel as IPoolObject).Used();
                }
            }
            //  不报错则true；后期再考虑其他的
            return true;
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="type">消息类型：如mq、pubsub</param>
        /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机、重视次数等信息</param>
        /// <param name="receiver">消息接收器；用于处理具体消息；接收到消息后，执行此委托</param>
        /// <param name="server">消息服务器地址：接收的消息来自哪里</param>
        /// <returns>消息接收器注册成功，返回true；否则返回false</returns>
        async Task<bool> IMessageProvider.Receive<T>(MessageType type, IReceiveOptions options, Func<T, Task<bool>> receiver, IServerOptions server)
        {
            /*  基本处理逻辑：
             *      1、接收消息时信道和链接出现错误，记录到本地文件中；
             *      2、接收消息，若转换数据失败，记录默认日志；
             *      3、接收消息，若调用到处理器dealer，则不用记录日志了，但监听强制应答异常ForceAskException
             */
            ThrowIfNull(receiver);
            ThrowIfNull(options);
            ThrowIfNull(server);
            string logTitle = $"Received.Start   Server:{server.ToString()}";
            string opName = $"接收[{type.ToString()}]消息：{options.AsJson()}";

            //  1、基于信道，构建交换机、队列
            Action<string, string> onChannelError = (title, reason) =>
            {
                title = $"接收[{type.ToString()}]消息:{title}";
                _manager.FileLogger.Error(title, $"{reason}{Environment.NewLine}\t{opName}{Environment.NewLine}\t{server.ToString()}");
            };
            ChannelProxy? channel = null;
            string queue;
            try
            {
                channel = await _manager.GetChannel(isSend: false, server);
                channel.OnError += onChannelError;
                await DeclareExchange(channel.Object, type, options);
                queue = await DeclareQueue(channel.Object, type, options);
            }
            catch (Exception ex)
            {
                _manager.FileLogger.Error(logTitle, opName, ex);
                throw;
            }
            //  2、构建消息接收处理器
            ReceiverProxy<T> proxy = new ReceiverProxy<T>(options.Attempt, receiver);
            AsyncEventHandler<BasicDeliverEventArgs> onReceived = async (sender, args) =>
            {

                bool isSuccess = false, dataConverted = false;
                IChannel tmpChanel = (sender as AsyncEventingBasicConsumer)!.Channel;
                string? dataStr = null;
                try
                {
                    dataStr = args.Body.ToArray().AsString();
                    T message = BuildMessage<T>(dataStr);
                    dataConverted = true;
                    //  执行消息接收；做好异常拦截
                    isSuccess = await proxy.OnReceive(message);
                }
                catch (ForceAskException)
                {
                    isSuccess = true;
                }
                //  其他异常不做处理，要记录日志，使用消息中间件
                catch
                { }
                //  若是转换消息数据失败，则强制成功
                if (dataConverted == false)
                {
                    _manager.FileLogger.Error(opName, $"转换数据失败，消息强制成功。目标类型：{typeof(T).FullName}。数据：{dataStr}");
                    isSuccess = true;
                }
                //  消息处理是否成功
                if (isSuccess == true)
                {
                    await tmpChanel.BasicAckAsync(args.DeliveryTag, multiple: true);
                }
                else
                {
                    await tmpChanel.BasicRejectAsync(args.DeliveryTag, requeue: true);
                }
            };
            //  3、构建消息消费者，接收消息；基于并行逻辑，确保每个消息的接收器都是独立的
            try
            {
                for (var i = 0; i < Math.Max(options.Concurrent, 1); i++)
                {
                    if (channel == null)
                    {
                        channel = await _manager.GetChannel(isSend: false, server);
                        channel.OnError += onChannelError;
                    }
                    AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel.Object);
                    consumer.ReceivedAsync += onReceived;
                    //  接收消息：每次接收1个
                    await channel.Object.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
                    await channel.Object.BasicConsumeAsync(queue, autoAck: false, consumer);
                    //  执行完成后，将信道清理掉，方便下次循环使用
                    channel = null;
                }
            }
            catch (Exception ex)
            {
                _manager.FileLogger.Error(logTitle, opName, ex);
                throw;
            }
            //  无错误返回true，表示接收逻辑执行成功
            return true;
        }
        #endregion

        #region 继承方法
        /// <summary>
        /// 获取交换机名称
        /// </summary>
        /// <param name="options"></param>
        /// <remarks>开发环境下，加【机器名】后缀，做到区分分发</remarks>
        /// <returns></returns>
        protected virtual string GetExchange(IMessageOptions options)
            => ReBuildNameByEnvironment(options.Exchange);
        /// <summary>
        /// 获取路由名称
        /// </summary>
        /// <param name="options"></param>
        /// <remarks>开发环境下，加【机器名】后缀，做到区分分发</remarks>
        /// <returns></returns>
        protected virtual string GetRouting(IMessageOptions options)
            => ReBuildNameByEnvironment(options.Routing);
        /// <summary>
        /// 获取队列名称
        /// </summary>
        /// <param name="options"></param>
        /// <remarks>开发环境下，加【机器名】后缀，做到区分分发</remarks>
        /// <returns></returns>
        protected virtual string GetQueue(IReceiveOptions options)
            => ReBuildNameByEnvironment(options.Queue);

        /// <summary>
        /// 定义交换机
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="type"></param>
        /// <param name="options"></param>
        protected virtual async Task<string> DeclareExchange(IChannel channel, MessageType type, IMessageOptions options)
        {
            string exchange = GetExchange(options);
            if (string.IsNullOrEmpty(options.Exchange) == false)
            {
                string exchangeType = type switch
                {
                    MessageType.MQ => ExchangeType.Direct,
                    MessageType.PubSub => ExchangeType.Fanout,
                    _ => throw new NotImplementedException($"不支持的消息类型：{type.ToString()}"),
                };
                await channel.ExchangeDeclareAsync(exchange: exchange, exchangeType, durable: true, autoDelete: false, arguments: null);
            }
            return exchange;
        }
        /// <summary>
        /// 定义队列
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <returns>队列名称</returns>
        protected virtual async Task<string> DeclareQueue(IChannel channel, MessageType type, IReceiveOptions options)
        {
            ThrowIfNullOrEmpty(options.Queue);
            //  定义队列；非生产环境，则加上机器名，实现 开发机器 仅消费自己发出的消息；
            string queueName = GetQueue(options);
            {
                //  临时消息队列，用完就删除，随机队列名；后期再支持上
                //channel.QueueDeclare().QueueName
                //  声明队列：queue：队列名/消息名；durable：是否持久化  exclusive：独占的队列 autoDelete：自动删除队列 arguments：其他参数
                var ok = await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                queueName = ok.QueueName;
            }
            //  有交换机，则做绑定
            if (string.IsNullOrEmpty(options.Exchange) == false)
            {
                string routing = GetRouting(options);
                string exchange = GetExchange(options);
                await channel.QueueBindAsync(queueName, exchange, routing, arguments: null);
            }

            return queueName;
        }

        /// <summary>
        /// 基于消息数据反序列化构建消息实体对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageBody"></param>
        /// <returns></returns>
        protected virtual T BuildMessage<T>(string messageBody)
        {
            return string.IsNullOrEmpty(messageBody)
                ? default!
                : messageBody.As<T>();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 基于环境信息重构名称；若为开发环境，自动追加机器名称
        /// </summary>
        /// <param name="name"></param>
        /// <returns>若name为空，则返回string.Empty；否则返回基于环境构建的name新值</returns>
        private string ReBuildNameByEnvironment(string? name)
        {
            if (string.IsNullOrEmpty(name) == false)
            {
                name = _app.IsProduction()
                    ? name
                    : $"{name}:{Dns.GetHostName()}";
            }
            return name ?? string.Empty;
        }
        #endregion
    }
}

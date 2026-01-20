using K4os.Compression.LZ4;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Snail.Abstractions;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Logging.Extensions;
using Snail.Abstractions.Setting.Extensions;
using Snail.Abstractions.Web.Interfaces;
using Snail.Message.Components;
using Snail.RabbitMQ.Components;
using Snail.RabbitMQ.Exceptions;
using Snail.Utilities.Common.Extensions;
using Snail.Utilities.Common.Interfaces;
using static System.Environment;

namespace Snail.RabbitMQ;

/// <summary>
/// <see cref="IMessageProvider"/> 消息提供程序RabbitMQ实现
/// </summary>
/// <remarks>作为默认实现类，并提供命名实现</remarks>
[Component<IMessageProvider>]
[Component<IMessageProvider>(Key = DIKEY_RabbitMQ)]
public class MessageProvider : IMessageProvider
{
    #region 属性变量
    /// <summary>
    /// Key值：压缩类型
    /// </summary>
    protected const string KEY_CompressionType = "compression.type";
    /// <summary>
    /// 应用程序实例
    /// </summary>
    protected readonly IApplication App;
    /// <summary>
    /// RabbitMQ管理器
    /// </summary>
    protected readonly RabbitManager Manager;
    /// <summary>
    /// 消息序列化器
    /// </summary>
    protected readonly IMessageSerializer Serializer;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    public MessageProvider(IApplication app)
    {
        App = ThrowIfNull(app);
        Manager = app.ResolveRequired<RabbitManager>();
        //  消息序列化器：若为空则使用默认的
        Serializer = app.Resolve<IMessageSerializer>() ?? new MessageSerializer();
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
    async Task<bool> IMessageProvider.Send<T>(MessageType type, T message, ISendOptions options, IServerOptions server)
    {
        ThrowIfNull(options);
        ThrowIfNull(server);
        //  发送消息到交换机：声明交换机和消息路由；若交换机为空则为默认交换机
        ChannelProxy? proxy = null;
        void onChannelError(string title, string reason)
        {
            title = $"发送[{type}]消息：{title}";
            App.LogErrorFile(title, $"消息配置：{options.GetString()} ；服务器：{server}{NewLine}\t{reason}");
        }
        try
        {
            proxy = await Manager.GetChannel(isSend: true, server);
            proxy.OnError += onChannelError;
            //  定义交换机，初始化路由
            string exchange = await DeclareExchange(proxy.Object, type, options);
            string routing = GetRouting(options);
            //  发送消息
            //      组装消息body
            BasicProperties props = new()
            {
                ContentType = "text/plain",/*内容类型：文本数据*/
                ContentEncoding = "utf-8",
                DeliveryMode = DeliveryModes.Persistent,/*交付模式：持久化*/
            };
            byte[] body = Serializer.Serialize(message);
            //      压缩消息数据
            if (options.Compressible == true)
            {
                body = Compress(body, out string format);
                props.Headers = new Dictionary<string, object?>()
                {
                    [KEY_CompressionType] = format ?? string.Empty,
                };
            }
            await proxy.Object.BasicPublishAsync(exchange, routing, mandatory: false, props, body);
        }
        finally
        {
            //  回收信道
            if (proxy != null)
            {
                (proxy as IPoolable).Used();
            }
        }
        //  不报错则true；后期再考虑其他的
        return true;
    }

    /// <summary>
    /// 接收消息
    /// </summary>
    /// <param name="type">消息类型：如mq、pubsub</param>
    /// <param name="receiver">消息接收器；用于处理具体消息；接收到消息后，执行此委托</param>
    /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机、重视次数等信息</param>
    /// <param name="server">消息服务器地址：接收的消息来自哪里</param>
    /// <returns>消息接收器注册成功，返回true；否则返回false</returns>
    async Task<bool> IMessageProvider.Receive<T>(MessageType type, Func<T, Task<bool>> receiver, IReceiveOptions options, IServerOptions server)
    {
        /*  基本处理逻辑：
         *      1、接收消息时信道和链接出现错误，记录到本地文件中；
         *      2、接收消息，若转换数据失败，记录默认日志；
         *      3、接收消息，若调用到处理器dealer，则不用记录日志了，但监听强制应答异常ForceAskException
         */
        ThrowIfNull(receiver);
        ThrowIfNull(options);
        ThrowIfNull(server);
        //  1、基于信道，构建交换机、队列
        ChannelProxy? proxy = null;
        string queue;
        void onChannelError(string title, string reason)
        {
            App.LogErrorFile($"接收[{type}]消息：{title}", $"消息配置：{options.GetString()} ；服务器：{server}{NewLine}\t{reason}{NewLine}");
        }
        try
        {
            proxy = await Manager.GetChannel(isSend: false, server);
            proxy.OnError += onChannelError;
            await DeclareExchange(proxy.Object, type, options);
            queue = await DeclareQueue(proxy.Object, type, options);
        }
        catch (Exception ex)
        {
            App.LogErrorFile($"接收[{type}]消息：初始化消息队列失败", $"消息配置：{options.GetString()} ；服务器：{server}", ex);
            throw;
        }
        //  2、构建消息接收处理器
        Func<T, Task<bool>> receiveHandler = new ReceiverProxy<T>(options.Attempt, receiver).OnReceive;
        async Task onReceived(object sender, BasicDeliverEventArgs args)
        {
            bool isSuccess = false, dataConverted = false;
            IChannel tmpChanel = (sender as AsyncEventingBasicConsumer)!.Channel;
            string? dataStr = null;
            try
            {
                byte[] body = args.Body.ToArray();
                //  进行解压缩处理 compress
                if (args.BasicProperties?.Headers?.TryGetValue(KEY_CompressionType, out object? compress) == true)
                {
                    string? format = compress is byte[] bytes ? bytes.AsString() : null;
                    if (IsNullOrEmpty(format) == false)
                    {
                        body = Decompress(body, format);
                    }
                }
                //  执行消息接收：进行数据转换，做好异常拦截
                T message = Serializer.Deserialize<T>(body);
                dataConverted = true;
                isSuccess = await receiveHandler(message);
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
                string content = $"消息配置：{options.GetString()} ；服务器：{server}{NewLine}\t目标类型：{typeof(T).FullName}。数据：{dataStr}";
                App.LogErrorFile($"接收[{type}]消息：转换数据失败，消息强制成功消费", content);
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
        }
        //  3、构建消息消费者，接收消息；基于并行逻辑，确保每个消息的接收器都是独立的
        try
        {
            for (var i = 0; i < Math.Max(options.Concurrent, 1); i++)
            {
                if (proxy == null)
                {
                    proxy = await Manager.GetChannel(isSend: false, server);
                    proxy.OnError += onChannelError;
                }
                AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(proxy.Object);
                consumer.ReceivedAsync += onReceived;
                //  接收消息：每次接收1个；强制约束 consumerTag，避免自动重连时发生变化，影响关闭时销毁
                await proxy.Object.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
                string consumerTag = Guid.NewGuid().ToString();
                await proxy.Object.BasicConsumeAsync(queue, autoAck: false, consumerTag: consumerTag, consumer: consumer);
                //  接收应用程序关闭事件，停止继续接收消息
                App.OnStop += async () =>
                {
                    await proxy.Object.BasicCancelAsync(consumerTag);
                    (proxy as IPoolable).Used();
                };
            }
        }
        catch (Exception ex)
        {
            App.LogErrorFile($"接收[{type}]消息：启动消息接收失败", $"消息配置：{options.GetString()} ；服务器：{server}", ex);
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
        => Manager.ReBuildNameByEnvironment(options.Exchange);
    /// <summary>
    /// 获取路由名称
    /// </summary>
    /// <param name="options"></param>
    /// <remarks>开发环境下，加【机器名】后缀，做到区分分发</remarks>
    /// <returns></returns>
    protected virtual string GetRouting(IMessageOptions options)
        => Manager.ReBuildNameByEnvironment(options.Routing);
    /// <summary>
    /// 获取队列名称
    /// </summary>
    /// <param name="options"></param>
    /// <remarks>开发环境下，加【机器名】后缀，做到区分分发</remarks>
    /// <returns></returns>
    protected virtual string GetQueue(IReceiveOptions options)
        => Manager.ReBuildNameByEnvironment(options.Queue);

    /// <summary>
    /// 定义交换机
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="type"></param>
    /// <param name="options"></param>
    protected virtual async Task<string> DeclareExchange(IChannel channel, MessageType type, IMessageOptions options)
    {
        string exchange = GetExchange(options);
        if (IsNullOrEmpty(options.Exchange) == false)
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
    /// 压缩消息数据
    /// </summary>
    /// <param name="body"></param>
    /// <param name="format">使用的压缩格式</param>
    /// <returns></returns>
    protected virtual byte[] Compress(in byte[] body, out string format)
    {
        format = "lz4";
        return LZ4Pickler.Pickle(body);
    }
    /// <summary>
    /// 解压缩消息数据
    /// </summary>
    /// <param name="body"></param>
    /// <param name="format">压缩格式</param>
    /// <returns></returns>
    protected virtual byte[] Decompress(in byte[] body, in string format)
    {
        switch (format)
        {
            //  LZ4 格式解压缩
            case "lz4":
                return LZ4Pickler.Unpickle(body);
            //  其他格式，默认不支持
            default:
                throw new NotSupportedException($"不支持的压缩格式：{format}");
        }
    }
    #endregion
}

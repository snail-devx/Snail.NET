using Snail.Abstractions.Message.DataModels;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;
using Snail.Utilities.Common.Extensions;

namespace Snail.Abstractions.Message.Extensions;

/// <summary>
/// <see cref="IMessenger" />扩展方法
/// </summary>
public static class MessengerExtensions
{
    #region 公共方法
    /// <summary>
    /// 发送消息：<see cref="IMessageOptions"/>基于<paramref name="message"/>做默认构建 <br />
    ///     1、简化操作逻辑，贴合我之前的使用习惯 <br />
    ///     2、MQ消息：发送到默认交换机，路由为<paramref name="message"/> <br />
    ///     3、PubSub消息：发送到<paramref name="message"/>交换机，路由为空 <br />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="messenger"></param>
    /// <param name="type">消息类型</param>
    /// <param name="message">消息名称；基于此自动构建<see cref="IMessageOptions"/></param>
    /// <param name="data">消息附带数据</param>
    /// <param name="context">消息附带上下文</param>
    /// <returns></returns>
    public static Task<bool> Send<T>(this IMessenger messenger, MessageType type, string message, T? data, IDictionary<string, string>? context = null)
    {
        ThrowIfNullOrEmpty(message);
        MessageOptions options;
        switch (type)
        {
            //  MQ消息：发送消息到默认交换机，路由采用消息名称
            case MessageType.MQ:
                options = new MessageOptions()
                {
                    Exchange = null,
                    Routing = message,
                };
                break;
            //  PubSub消息：发送到交换机，路由默认null
            case MessageType.PubSub:
                options = new MessageOptions()
                {
                    Exchange = message,
                    Routing = null,
                };
                break;
            default:
                throw new NotSupportedException($"不支持的消息类型：{type}");
        }
        return messenger.Send(type, new MessageData() { Name = message, Data = data?.AsJson(), Context = context }, options);
    }

    /// <summary>
    /// 接收消息：<see cref="IReceiveOptions"/>基于<paramref name="message"/>和<paramref name="name"/>做默认构建 <br />
    ///     1、简化操作逻辑，贴合我之前的使用习惯；自动重视3次，并发1 <br />
    ///     2、MQ消息：从到默认交换机接收数据，路由为<paramref name="message"/>，队列名<paramref name="message"/><br />
    ///     3、PubSub消息：从<paramref name="message"/>交换机接收数据，路由为空,队列名<paramref name="message"/>+<paramref name="name"/>组合，确保唯一；<br />
    /// </summary>
    /// <param name="messenger"></param>
    /// <param name="type">消息类型</param>
    /// <param name="name">接收器名称；PubSub时必传，和<paramref name="message"/>合并构建<see cref="IReceiveOptions.Queue"/></param>
    /// <param name="message">消息名称；基于此自动构建<see cref="IMessageOptions"/></param>
    /// <param name="receiver">消息处理委托</param>
    /// <returns></returns>
    public static Task<bool> Receive(this IMessenger messenger, MessageType type, string? name, string message, Func<MessageData, Task<bool>> receiver)
    {
        ThrowIfNullOrEmpty(message);
        ThrowIfNull(receiver);
        ReceiveOptions options;
        switch (type)
        {
            //  MQ消息：
            case MessageType.MQ:
                options = new ReceiveOptions()
                {
                    Queue = message,
                    Routing = message,
                    Exchange = null,
                    Attempt = 3,
                    Concurrent = 1,
                };
                break;
            //  PubSub消息：构建唯一队列，确保消息被不同接收器消费
            case MessageType.PubSub:
                ThrowIfNullOrEmpty(name);
                options = new ReceiveOptions()
                {
                    Queue = $"{name}:{message}",
                    Routing = null,
                    Exchange = message,
                    Attempt = 3,
                    Concurrent = 1,
                };
                break;
            default:
                throw new NotSupportedException($"为支持的消息类型：{type}");
        }
        return messenger.Receive(type, options, receiver);
    }
    #endregion
}

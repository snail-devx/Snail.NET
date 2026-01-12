using Snail.Abstractions.Message.Interfaces;

namespace Snail.Message.Components;
/// <summary>
/// 默认的消息序列化器
/// </summary>
[Component<IMessageSerializer>]
public class MessageSerializer : IMessageSerializer
{
    #region IMessageSerializer
    /// <summary>
    /// 序列化消息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual byte[] Serialize<T>(T message)
    {
        //string dataStr = message?.AsJson() ?? "null";/*兼容发送消息为null的情况*/
        //byte[] body = dataStr.AsBytes();

        /*兼容发送消息为null的情况*/
        string dataStr = message?.AsJson() ?? "null";
        return dataStr.AsBytes();
    }

    /// <summary>
    /// 反序列化消息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="body"></param>
    /// <returns></returns>
    public virtual T Deserialize<T>(byte[] body)
    {
        //dataStr = args.Body.ToArray().AsString();
        //T message =  string.IsNullOrEmpty(messageBody)
        //  ? default!
        //  : messageBody.As<T>();

        string bodyStr = body.AsString();
        return IsNullOrEmpty(bodyStr) ? default! : bodyStr.As<T>();
    }
    #endregion
}
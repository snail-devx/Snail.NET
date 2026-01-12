namespace Snail.Abstractions.Message.Interfaces;
/// <summary>
/// 接口约束：消息序列化器
/// <para>1、发送消息前，进行消息序列化；接收消息时，进行消息发序列化</para>
/// <para>2、用于配合<see cref="IMessageProvider"/>使用，在Provider中处理消息时进行自定义处理</para>
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// 序列化消息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <returns></returns>
    byte[] Serialize<T>(T message);

    /// <summary>
    /// 反序列化消息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="body"></param>
    /// <returns></returns>
    T Deserialize<T>(byte[] body);
}
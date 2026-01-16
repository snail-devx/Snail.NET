namespace Snail.Abstractions.Message.Interfaces;
/// <summary>
/// 接口约束：消息发送配置选项
/// </summary>
public interface ISendOptions : IMessageOptions
{
    /// <summary>
    /// 是否压缩发送的消息
    /// <para>1、为true时，使用LZ进行消息数据压缩</para>
    /// </summary>
    bool Compress { get; }

    /// <summary>
    ///  进行消息处理时，禁用消息中间件
    /// <para>1、为true时，发送/接收消息时不执行配置好的消息中间件</para>
    /// <para>2、满足在一些特定业务场景下，无需中间件处理消息，直接原生对接消息服务器</para>
    /// </summary>
    bool DisableMiddleware { get; }
}
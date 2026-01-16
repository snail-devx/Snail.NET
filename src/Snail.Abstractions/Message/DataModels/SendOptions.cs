using Snail.Abstractions.Message.Interfaces;

namespace Snail.Abstractions.Message.DataModels;
/// <summary>
/// 发送消息配置选项
/// </summary>
public sealed class SendOptions : ISendOptions
{
    /// <summary>
    /// 消息交换机名称
    /// </summary>
    public string? Exchange { init; get; }
    /// <summary>
    /// 消息交换机和队列之间的路由
    /// </summary>
    public string? Routing { init; get; }

    /// <summary>
    /// 是否压缩发送的消息
    /// <para>1、为true时，使用LZ进行消息数据压缩</para>
    /// </summary>
    public bool Compress { init; get; }

    /// <summary>
    ///  进行消息处理时，禁用消息中间件
    /// <para>1、为true时，发送/接收消息时不执行配置好的消息中间件</para>
    /// <para>2、满足在一些特定业务场景下，无需中间件处理消息，直接原生对接消息服务器</para>
    /// </summary>
    public bool DisableMiddleware { init; get; }
}
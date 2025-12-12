using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;
using Snail.Abstractions.Web.Attributes;

namespace Snail.Abstractions.Message.Attributes;

/// <summary>
/// 特性标签：消息接收器
/// <para>1、接收什么消息、消息重视、并发等配置</para>
/// <para>2、配合<see cref="ServerAttribute"/>标签，同步配置消息服务器地址</para>
/// </summary>
/// <remarks>简化版接收器参照：<see cref="MQReceiverAttribute"/>和<see cref="PubSubReceiverAttribute"/></remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ReceiverAttribute : Attribute, IReceiveOptions
{
    #region 属性变量
    /// <summary>
    /// 消息类型
    /// </summary>
    public required MessageType Type { init; get; }
    #endregion

    #region IReceiverOptions
    /// <summary>
    /// 消息交换机名称
    /// </summary>
    public string? Exchange { init; get; }
    /// <summary>
    /// 消息交换机和队列之间的路由
    /// </summary>
    public string? Routing { init; get; }
    /// <summary>
    /// 消息队列名称
    /// </summary>
    public required string Queue { init; get; }

    /// <summary>
    /// 接收消息的尝试次数
    /// <para>1、接收方发生异常后，尝试多少次后仍失败，则强制确认，避免消息堆积 </para>
    /// <para>2、== 0 失败则自动确认消费 </para>
    /// <para>3、&lt;0 不自动确认 </para>
    /// </summary>
    public int Attempt { init; get; }
    /// <summary>
    /// 消息接收器并发数量 <br />
    /// <para>1、当前接收器从<see cref="Queue"/>接收消息的并发量 </para>
    /// <para>2、大于1时生效，合理设置，提高消息消费效率 </para>
    /// </summary>
    public int Concurrent { init; get; }

    /// <summary>
    ///  进行消息处理时，禁用消息中间件
    /// <para>1、为true时，发送/接收消息时不执行配置好的消息中间件</para>
    /// <para>2、满足在一些特定业务场景下，无需中间件处理消息，直接原生对接消息服务器</para>
    /// </summary>
    public bool DisableMiddleware { init; get; }
    #endregion
}
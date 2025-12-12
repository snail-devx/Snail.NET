using Snail.Abstractions.Message.Interfaces;
using Snail.Abstractions.Web.Attributes;

namespace Snail.Abstractions.Message.Attributes;

/// <summary>
/// 特性标签：【发布订阅】消息接收器；针对特定属性做默认值处理
/// <para>1、基于消息名称，自动创建队列名称，初始化交换机等信息</para>
/// <para>2、<see cref="ReceiverAttribute"/>简化版处理，减少配置量</para>
/// <para>3、配合<see cref="ServerAttribute"/>标签，同步配置消息服务器地址</para>
/// </summary>
/// <remarks>默认处理：
/// <para>1、交换机=<see cref="_message"/>、路由=空、队列名=<see cref="_name"/>:<see cref="_message"/></para>
/// <para>2、重视次数=3；并发=1</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class PubSubReceiverAttribute : Attribute, IReceiveOptions
{
    #region 属性变量
    /// <summary>
    /// 接受器名称
    /// <para>1、确保<see cref="_name"/>+<see cref="_message"/>组合唯一；二者合并构建<see cref="IReceiveOptions.Queue"/></para>
    /// </summary>
    private string _name { init; get; }
    /// <summary>
    /// 接收消息名
    /// <para>1、确保<see cref="_name"/>+<see cref="_message"/>组合唯一；二者合并构建<see cref="IReceiveOptions.Queue"/></para>
    /// </summary>
    private string _message { init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="name">接受器名称</param>
    /// <param name="message">接收消息名称</param>
    public PubSubReceiverAttribute(string name, string message)
    {
        _name = ThrowIfNullOrEmpty(name);
        _message = ThrowIfNullOrEmpty(message);
    }
    #endregion

    #region IReceiverOptions
    /// <summary>
    /// 消息交换机名称；强制采用消息名称
    /// </summary>
    string? IMessageOptions.Exchange => _message;
    /// <summary>
    /// 消息交换机和队列之间的路由
    /// </summary>
    string? IMessageOptions.Routing => null;
    /// <summary>
    /// 接收消息的队列名称
    /// </summary>
    string IReceiveOptions.Queue => $"{_name}:{_message}";

    /// <summary>
    /// 接收消息的尝试次数
    /// <para>1、接收方发生异常后，尝试多少次后仍失败，则强制确认，避免消息堆积 </para>
    /// <para>2、&lt;= 0 不自动确认；直到处理成功 </para>
    /// </summary>
    public int Attempt { init; get; } = 3;
    /// <summary>
    /// 消息接收器并发数量 <br />
    /// <para>1、当前接收器从<see cref="IReceiveOptions.Queue"/>接收消息的并发量 </para>
    /// <para>2、大于1时生效，合理设置，提高消息消费效率 </para>
    /// </summary>
    public int Concurrent { init; get; } = 1;

    /// <summary>
    ///  进行消息处理时，禁用消息中间件
    /// <para>1、为true时，发送/接收消息时不执行配置好的消息中间件</para>
    /// <para>2、满足在一些特定业务场景下，无需中间件处理消息，直接原生对接消息服务器</para>
    /// </summary>
    public bool DisableMiddleware { init; get; }
    #endregion
}

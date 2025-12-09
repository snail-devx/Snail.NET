using Snail.Abstractions.Message.Delegates;

namespace Snail.Abstractions.Message;

/// <summary>
/// 接口约束：消息管理器 <br />
///     1、维护消息中间件 <br />
///     2、消息管理：能够发送、接收哪些消息；后期再提供，前期不做管控 <br />
/// </summary>
public interface IMessageManager
{
    /// <summary>
    /// 使用【发送消息】中间件
    /// </summary>
    /// <param name="name"></param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    IMessageManager Use(string? name, Func<SendDelegate, SendDelegate> middleware);
    /// <summary>
    /// 构建【发送消息】中间件执行委托
    /// </summary>
    /// <param name="start">入口委托；所有中间件都执行了，在执行此委托处理实际业务逻辑</param>
    /// <returns>执行委托</returns>
    SendDelegate Build(SendDelegate start);

    /// <summary>
    /// 使用【接收消息】中间件
    /// </summary>
    /// <param name="name">中间件名称；传入确切值，则会先查找同名中间件是否存在，若存在则替换到原先为止；否则始终追加</param>
    /// <param name="middleware">中间件</param>
    /// <returns>消息管理器自身，方便链式调用</returns>
    IMessageManager Use(string? name, Func<ReceiveDelegate, ReceiveDelegate> middleware);
    /// <summary>
    /// 构建【接收消息】中间件执行委托
    /// </summary>
    /// <param name="start">入口委托；所有中间件都执行了，在执行此委托处理实际业务逻辑</param>
    /// <returns>执行委托</returns>
    ReceiveDelegate Build(ReceiveDelegate start);
}

namespace Snail.Abstractions.Common.Interfaces;

/// <summary>
/// 接口约束：中间件代理器 
/// <para>1、实现洋葱模型，管道式编程 </para>
/// <para>2、支持Name命名，用于固话顺序使用 </para>
/// </summary>
/// <typeparam name="Middleware">中间件委托对象</typeparam>
public interface IMiddlewareProxy<Middleware> where Middleware : Delegate
{
    /// <summary>
    /// 使用中间件；中间件name为null
    /// </summary>
    /// <param name="middleware">中间件委托</param>
    /// <returns>代理器自身，方便链式调用</returns>
    IMiddlewareProxy<Middleware> Use(in Func<Middleware, Middleware> middleware)
        => Use(name: null, middleware);
    /// <summary>
    /// 使用中间件
    /// </summary>
    /// <param name="name">中间件名称；传入确切值，则会先查找同名中间件是否存在，若存在则替换到原先为止；否则始终追加</param>
    /// <param name="middleware">中间件委托；为null表示占位，此时<paramref name="name"/>不能为null</param>
    /// <returns>代理器自身，方便链式调用</returns>
    IMiddlewareProxy<Middleware> Use(in string? name, in Func<Middleware, Middleware>? middleware);

    /// <summary>
    /// 构建中间件执行委托
    /// </summary>
    /// <param name="start">入口委托；所有中间件都执行了，再执行此委托处理实际业务逻辑</param>
    /// <param name="onionMode">洋葱模式，越早use的中间件越早执行；否则越晚use的中间件越早执行</param>
    /// <returns>中间件执行委托</returns>
    Middleware Build(in Middleware start, in bool onionMode = true);
}

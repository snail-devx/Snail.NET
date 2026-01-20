using Snail.Abstractions.Common.Interfaces;

namespace Snail.Common.Components;

/// <summary>
/// 中间件代理器 
/// <para>1、实现洋葱模型，管道式编程 </para>
/// <para>2、支持Name命名，用于固化顺序使用 </para>
/// </summary>
/// <typeparam name="Middleware">中间件委托对象</typeparam>
[Component(From = typeof(IMiddlewareProxy<>))]
public class MiddlewareProxy<Middleware> : IMiddlewareProxy<Middleware> where Middleware : Delegate
{
    #region 属性变量
    /// <summary>
    /// 注册的【中间件】集合
    /// </summary>
    private readonly List<Tuple<string?, Func<Middleware, Middleware>?>> _middlewares = [];
    #endregion

    #region 构造方法
    /// <summary>
    /// 默认无参构造方法
    /// </summary>
    public MiddlewareProxy()
    { }
    #endregion

    #region IMiddlewareProxy<Middleware>
    /// <summary>
    /// 使用中间件
    /// </summary>
    /// <param name="name">中间件名称；传入确切值，则会先查找同名中间件是否存在，若存在则替换到原先为止；否则始终追加</param>
    /// <param name="middleware">中间件委托；为null表示占位，此时<paramref name="name"/>不能为null</param>
    /// <returns>代理器自身，方便立案时调用</returns>
    IMiddlewareProxy<Middleware> IMiddlewareProxy<Middleware>.Use(in string? name, in Func<Middleware, Middleware>? middleware)
    {
        //  同时为null无效
        if (string.IsNullOrEmpty(name) == true && middleware == null)
        {
            string msg = $"{nameof(name)}和{nameof(middleware)}不能同时为空";
            throw new ArgumentException(msg);
        }
        Tuple<string?, Func<Middleware, Middleware>?> tuple = new(name, middleware);
        //  基于name查找，查到了直接替换
        if (name?.Length > 0)
        {
            for (int index = 0; index < _middlewares.Count; index++)
            {
                if (_middlewares[index].Item1 == name)
                {
                    _middlewares[index] = tuple;
                    return this;
                }
            }
        }
        //  追加中间件
        _middlewares.Add(tuple);
        return this;
    }

    /// <summary>
    /// 构建中间件执行委托
    /// </summary>
    /// <param name="start">入口委托；所有中间件都执行了，再执行此委托处理实际业务逻辑</param>
    /// <param name="onionMode">洋葱模式，越早use的中间件越早执行；否则越晚use的中间件越早执行</param>
    /// <returns>中间件执行委托</returns>
    Middleware IMiddlewareProxy<Middleware>.Build(in Middleware start, in bool onionMode)
    {
        ThrowIfNull(start);
        //  剔除为null的数据，洋葱模型则反序处理
        var middles = onionMode == true
                ? _middlewares.Where(tlp => tlp.Item2 != null).Reverse()
                : _middlewares.Where(tlp => tlp.Item2 != null);
        //  构建委托中间件
        Middleware ret = start;
        foreach (var (_, middleware) in middles)
        {
            ret = middleware!(ret);
        }
        return ret;
    }
    #endregion
}

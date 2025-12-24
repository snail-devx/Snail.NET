using Snail.Abstractions.Common.Interfaces;
using Snail.Utilities.Common.Extensions;

namespace Snail.Abstractions.Common.DataModels;

/// <summary>
/// 池对象基类；对<see cref="IPoolObject"/>做基础实现，简化复用
/// </summary>
public abstract class PoolObject : Disposable, IPoolObject
{
    #region 属性变量
    /// <summary>
    /// 闲置时间 
    /// <para>1、从什么时候开始闲置了；超过配置的闲置时间则自动回收 </para>
    /// </summary>
    protected DateTime IdleTime = DateTime.UtcNow;
    #endregion

    #region IPoolObject
    /// <summary>
    /// 闲置时间 
    /// <para>1、从什么时候开始闲置了；超过配置的闲置时间则自动回收 </para>
    /// </summary>
    DateTime IPoolObject.IdleTime { set => IdleTime = value; get => IdleTime; }
    #endregion

    #region 继承方法
    /// <summary>
    /// 对象释放
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed == false)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
            }
            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
        }
        //  执行基类回收
        base.Dispose(disposing);
    }
    #endregion
}

/// <summary>
/// 池对象；包装<typeparamref name="T"/>以支持的对象池中使用
/// </summary>
/// <typeparam name="T">要代理放入对象池中的数据类型</typeparam>
public class PoolObject<T> : PoolObject, IPoolObject where T : notnull
{
    #region 属性变量
    /// <summary>
    /// 对象实例
    /// </summary>
    public readonly T Object;
    #endregion

    #region 构造方法
    /// <summary>
    /// 默认无参构造方法
    /// </summary>
    public PoolObject(T obj)
    {
        Object = ThrowIfNull(obj);
    }
    #endregion

    #region 父类重写
    /// <summary>
    /// 销毁对象
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed == false)
        {
            if (disposing)
            {
                Object.TryDispose();
            }
            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
        }
        //  执行基类回收
        base.Dispose(disposing);
    }
    #endregion
}

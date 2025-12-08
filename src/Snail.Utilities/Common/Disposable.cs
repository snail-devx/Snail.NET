namespace Snail.Utilities.Common;
/// <summary>
/// 可销毁对象
/// </summary>
/// <remarks>定义基类，方便复用，减少重复代码</remarks>
public abstract class Disposable : IDisposable
{
    #region 属性变量
    /// <summary>
    /// 是否已经释放过了
    /// </summary>
    protected bool IsDisposed { private set; get; }
    #endregion

    #region IDisposable
    /// <summary>
    /// 对象释放
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed == false)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
            }
            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
            IsDisposed = true;
        }
    }

    // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
    // ~PoolObjectProxy()
    // {
    //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
    //     Dispose(disposing: false);
    // }

    /// <summary>
    /// 
    /// </summary>
    void IDisposable.Dispose()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}

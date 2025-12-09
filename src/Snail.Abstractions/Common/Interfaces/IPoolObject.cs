namespace Snail.Abstractions.Common.Interfaces;

/// <summary>
/// 接口约束：池对象
/// </summary>
public interface IPoolObject : IDisposable
{
    /// <summary>
    /// 闲置时间 <br />
    ///     1、从什么时候开始处理闲置状态；超过配置的闲置时间则自动回收<br />
    /// </summary>
    DateTime IdleTime { protected set; get; }
    /// <summary>
    /// 对象是否处于闲置状态
    /// </summary>
    bool IsIdle => IdleTime != default;

    /// <summary>
    /// 使用对象
    /// </summary>
    /// <remarks>和<see cref="Used"/>配合使用，注意线程并发影响</remarks>
    IPoolObject Using()
    {
        IdleTime = default;
        return this;
    }
    /// <summary>
    /// 对象使用完了
    /// </summary>
    /// <remarks>和<see cref="Using"/>配合使用，注意线程并发影响</remarks>
    void Used()
    {
        IdleTime = DateTime.UtcNow;
    }
}

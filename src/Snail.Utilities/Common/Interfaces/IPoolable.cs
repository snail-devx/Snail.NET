namespace Snail.Utilities.Common.Interfaces;
/// <summary>
/// 接口约束：可池化对象
/// <para>1、约束对象闲置时间等</para>
/// </summary>
public interface IPoolable : IDisposable
{
    /// <summary>
    /// 闲置时间
    /// <para>1、从什么时候开始闲置了；超过配置的闲置时间则自动回收 </para>
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
    IPoolable Using()
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
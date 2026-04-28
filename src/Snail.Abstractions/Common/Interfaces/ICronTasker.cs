namespace Snail.Abstractions.Common.Interfaces;

/// <summary>
/// 接口约束：定时任务执行者
/// </summary>
public interface ICronTasker
{
    /// <summary>
    /// 任务执行间隔，后期支持Cron表达式
    /// </summary>
    public TimeSpan Interval { get; }

    /// <summary>
    /// 定时任务执行者标题
    /// <para>用于记录操作日志；默认【定时任务】</para>
    /// </summary>
    public string? Title => "定时任务";

    /// <summary>
    /// 是否记录跟踪日志
    /// <para>默认false，为true时记录任务运行信息，如耗时情况、、、</para>
    /// </summary>
    public bool TraceLog => false;

    /// <summary>
    /// 运行任务
    /// </summary>
    /// <param name="cancellationToken">任务取消标记，用于判断当前定时任务是否取消/结束了</param>
    /// <returns>若为同步操作，直接返回null；否则返回对应异步task，外部等待task执行完成后，再进行下一个执行间隔</returns>
    public Task? Run(CancellationToken cancellationToken);

    /// <summary>
    /// 停止运行任务时调用
    /// <para>默认无操作时，可忽略实现</para>
    /// </summary>
    /// <returns>停止时若涉及异步操作，可返回异步task对象，外部会等待完成</returns>
    public Task? Stop()
    {
        return null;
    }
}

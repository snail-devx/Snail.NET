using Snail.Abstractions.Common.Interfaces;

namespace Snail.Abstractions.Common.DataModels;
/// <summary>
/// 定时任务代理：代理定时任务接口，方便创建定时任务执行器
/// </summary>
public sealed class CronTaskProxy : ICronTasker
{
    #region 属性变量
    /// <summary>
    /// 要执行的操作
    /// </summary>
    public Func<CancellationToken, Task?> Operate { private init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="interval">定时任务执行间隔</param>
    /// <param name="operate">要执行的具体操作</param>
    /// <param name="title">定时任务标题</param>
    /// <param name="traceLog">是否记录跟踪日志</param>
    public CronTaskProxy(TimeSpan interval, Func<CancellationToken, Task?> operate, string? title = null, bool traceLog = false)
    {
        Interval = interval;
        Operate = ThrowIfNull(operate);
        Title = title;
        TraceLog = traceLog;
    }
    #endregion

    #region ICronTasker
    /// <summary>
    /// 定时任务执行间隔
    /// </summary>
    public TimeSpan Interval { private init; get; }
    /// <summary>
    /// 定时任务标题
    /// </summary>
    public string? Title { private init; get; }
    /// <summary>
    /// 是否记录跟踪日志
    /// <para>默认false，为true时记录任务运行信息，如耗时情况、、、</para>
    /// </summary>
    public bool TraceLog { private init; get; }

    /// <summary>
    /// 运行任务
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task? ICronTasker.Run(CancellationToken cancellationToken)
        => Operate?.Invoke(cancellationToken);
    #endregion
}
using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Logging.Extensions;
using Snail.Utilities.Common.Extensions;
using System.Diagnostics;

namespace Snail.Abstractions.Common.Extensions;

/// <summary>
/// <see cref="IApplication"/>的通用扩展方法
/// </summary>
public static class ApplicationExtensions
{
    #region IBootstrapper 扩展
    /// <summary>
    /// 添加【引导程序】服务
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplication AddBootstrapperService(this IApplication app)
    {
        //  服务注册完成后，加载所有的 引导程序 实例，执行应用程序引导
        app.OnRegistered += services =>
        {
            IEnumerable<IBootstrapper>? bootstrappers = services.Resolve<IEnumerable<IBootstrapper>>();
            if (bootstrappers != null)
            {
                foreach (var item in bootstrappers)
                {
                    item.Bootstrap(app);
                }
            }
        };
        //  应用启动时，将 引导程序 从DI中干掉，后续不会再用了
        app.OnRun += services =>
        {
            services.Unregister<IEnumerable<IBootstrapper>>().Unregister<IBootstrapper>();
        };

        return app;
    }
    #endregion

    #region ICronTasker 扩展
    /// <summary>
    /// 添加【定时任务执行者】服务
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplication AddCronTaskerService(this IApplication app)
    {
        //  服务注册完成后，加载所有的 定时任务实例 实例
        IEnumerable<ICronTasker>? taskers = null;
        app.OnRegistered += di =>
        {
            taskers = di.Resolve<IEnumerable<ICronTasker>>();
        };
        //  应用程序启动时：进行定时任务启动；启动后，从DI中干掉，后续不会再用了
        app.OnRun += di =>
        {
            if (taskers != null)
            {
                foreach (var tasker in taskers)
                {
                    RunCronTasker(app, tasker);
                }
            }
            di.Unregister<IEnumerable<ICronTasker>>().Unregister<ICronTasker>();
            taskers = null;
        };

        return app;
    }
    /// <summary>
    /// 运行一个【定时任务执行者】
    /// </summary>
    /// <param name="app"></param>
    /// <param name="tasker">定时任务执行者</param>
    /// <returns></returns>
    /// <remarks>请在<see cref="IApplication.OnRun"/>事件后执行此方法
    /// <para>1、复杂业务场景优先推荐使用<see cref="AddCronTaskerService"/>服务方式；</para>
    /// <para>2、简单业务场景推荐此方法，可将其他功能逻辑和定时逻辑集成到一个class组件</para>
    /// </remarks>
    public static IApplication RunCronTasker(this IApplication app, ICronTasker tasker)
    {
        ThrowIfNull(tasker);
        PeriodicTimer timer = new PeriodicTimer(tasker.Interval);
        Task task = RunCronTasker(app, timer, tasker);
        //  应用程序停止时，取消任务执行
        app.OnStop += async () =>
        {
            try
            {
                timer.Dispose();
                Task? tmpTask = tasker.Stop();
                if (tmpTask != null)
                {
                    await tmpTask;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                app.LogErrorFile($"停止[{tasker.GetTitle()}]发生异常", ex.Optimize().Message, ex);
            }
        };
        return app;
    }

    /// <summary>
    /// 运行任务
    /// </summary>
    /// <param name="app"></param>
    /// <param name="timer"></param>
    /// <param name="tasker"></param>
    /// <returns></returns>
    private static async Task RunCronTasker(IApplication app, PeriodicTimer timer, ICronTasker tasker)
    {
        //  运行任务时，忽略第一次运行，第一次为立马执行，和定时任务逻辑不符，后期可通过在 ICronTasker 添加属性，约束是否立马执行
        bool isFirstRunning = true;
        Stopwatch? stopwatch = tasker.TraceLog ? new Stopwatch() : null;
        while (app.CancellationToken.IsCancellationRequested == false && await timer.WaitForNextTickAsync(app.CancellationToken))
        {
            if (isFirstRunning == true)
            {
                isFirstRunning = false;
                continue;
            }
            //  执行实际定时任务，进行异常捕捉
            stopwatch?.Restart();
            try
            {
                Task? task = tasker.Run(app.CancellationToken);
                if (task != null)
                {
                    await task;
                }
                if (stopwatch != null)
                {
                    app.LogTraceFile($"运行[{tasker.GetTitle()}]完成", $"耗时：{stopwatch.ElapsedMilliseconds}毫秒");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                app.LogErrorFile($"运行[{tasker.GetTitle()}]发生异常", ex.Optimize().Message, ex);
            }
            stopwatch?.Stop();
        }
    }
    /// <summary>
    /// 获取任务标题
    /// </summary>
    /// <param name="tasker"></param>
    /// <returns></returns>
    private static string GetTitle(this ICronTasker tasker)
        => Default(tasker.Title, "定时任务")!;
    #endregion
}

using Snail.Abstractions.Common.Extensions;
using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Logging.Extensions;

namespace Snail.Test.Common;

/// <summary>
/// <see cref="ICronTasker"/>测试
/// </summary>
public class CronTaskerTest
{
    [Test]
    public async Task TestCronTask()
    {
        IApplication app = new Application();
        app.AddLogService().AddCronTaskerService();
        app.Run();


        await Task.Delay(TimeSpan.FromSeconds(4));
        Task? task = app.Stop();
        if (task != null)
        {
            await task;
        }
    }


    #region 私有类型
    [Component<ICronTasker>]
    private class CronTasker1 : ICronTasker
    {
        #region ICronTasker
        TimeSpan ICronTasker.Interval { get; } = TimeSpan.FromSeconds(2);
        Task? ICronTasker.Run(CancellationToken cancellationToken)
        {
            return Task.Delay(TimeSpan.FromSeconds(1));
        }
        #endregion
    }

    [Component<ICronTasker>]

    private class CronTasker2 : ICronTasker
    {
        #region ICronTasker
        string ICronTasker.Title => "测试异常任务";
        TimeSpan ICronTasker.Interval { get; } = TimeSpan.FromSeconds(2);
        Task? ICronTasker.Run(CancellationToken cancellationToken)
        {
            throw new Exception("测试异常");
        }
        #endregion
    }
    #endregion
}

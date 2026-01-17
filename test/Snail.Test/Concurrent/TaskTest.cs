using System.Diagnostics;

namespace Snail.Test.Concurrent;
/// <summary>
/// 
/// </summary>
public sealed class TaskTest
{

    /// <summary>
    /// 测试 CancelToken
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task TestCancelToken()
    {
        //  测试注册同步任务问题
        var cts = new CancellationTokenSource();
        cts.Token.Register(async () =>
        {
            await Task.Delay(1000);
            Debug.WriteLine("11111111111111");
            //Assert.Fail("1111111111111111111");
        });
        cts.Cancel();

        //OnStopAsync += async () => await Task.Delay(100);
        //OnStopAsync += async () => await Task.Delay(100);
        //OnStopAsync += async () => await Task.Delay(100);
        //OnStopAsync += async () => await Task.Delay(100);
        //OnStopAsync += async () => await Task.Delay(100);
        //OnStopAsync += async () => await Task.Delay(100);
        //Task task = OnStopAsync.Invoke();
        //await task;
    }


}
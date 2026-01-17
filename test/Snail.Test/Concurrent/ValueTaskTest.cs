namespace Snail.Test.Concurrent;

internal class ValueTaskTest
{
    #region 测试方法
    /// <summary>
    /// 测试ValueTask
    /// </summary>
    /// <returns></returns>
    [Test]
    public async ValueTask TestValueTask()
    {
        //  内部使用ValueTask
        var vt = GetIntValueTask();
        Assert.That(vt.IsCompleted == true);
        int iv = await vt;
        Assert.That(iv == 100);
        //  内部使用Task，会转换成ValueTask
        vt = GetIntTask();
        Assert.That(vt.IsCompleted == false);
        iv = await vt;
        Assert.That(iv == 1000);
    }
    #endregion


    #region 私有方法
    private ValueTask<int> GetIntValueTask()
    {
        return ValueTask.FromResult<int>(100);
    }

    private async ValueTask<int> GetIntTask()
    {
        await Task.Delay(1000);
        return 1000;
    }
    #endregion
}

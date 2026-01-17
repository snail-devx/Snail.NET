using Snail.Abstractions.Message.Interfaces;
using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Test.Message;
/// <summary>
/// 
/// </summary>
public sealed class TestRecovery : UnitTestApp
{
    #region 属性变量
    /// <summary>
    /// 消息提供程序
    /// </summary>
    private readonly IMessageProvider _provider;
    /// <summary>
    /// 消息服务器
    /// </summary>
    private readonly IServerOptions _server = new ServerOptions(workspace: "Test", code: "Test");
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    public TestRecovery()
    {
        _provider = App.ResolveRequired<IMessageProvider>(key: DIKEY_RabbitMQ);
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 测试RabbitMQ的自动恢复机制
    /// </summary>
    /// <returns></returns>
    [Test]
    async public Task TestAutoRecovery()
    {
        //await _provider.Receive<string>(MessageType.MQ, message =>
        //{
        //    return Task.FromResult(true);
        //}, new ReceiveOptions()
        //{
        //    Queue = "test-autorecover",
        //    Routing = "test-autorecover",
        //    Exchange = null,
        //    Attempt = 3,
        //    Concurrent = 1
        //}, _server);

        //bool need = true;
        //while (true)
        //{
        //    await Task.Delay(1000);
        //    if (need != true)
        //    {
        //        continue;
        //    }
        //    try
        //    {
        //        await _provider.Send(type: MessageType.MQ, message: Guid.NewGuid().ToString(), new MessageOptions()
        //        {
        //            Exchange = null,
        //            Routing = "test-autorecover",
        //        }, _server);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.ToString());
        //    }
        //}

        await Task.CompletedTask;
    }
    #endregion
}
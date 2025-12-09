using Snail.Abstractions;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Logging;
using Snail.Abstractions.Logging.Attributes;
using Snail.Abstractions.Logging.Extensions;
using Snail.Abstractions.Web;
using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Interfaces;
using Snail.RabbitMQ.Components;
using Snail.Web;

namespace Snail.RabbitMQ;

/// <summary>
/// RabbitMQ管理器，管理服务器地址，进行一些基础逻辑实现
/// </summary>
[Component]
internal sealed class RabbitManager : ServerManager, IServerManager
{
    #region 属性变量
    /// <summary>
    /// 文件日志
    /// </summary>
    [Logger(ProviderKey = DIKEY_FileLogger)]
    internal required ILogger FileLogger { init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app">应用程序实例</param>
    public RabbitManager(IApplication app) : base(app, rsCode: "rabbitmq")
    { }
    #endregion

    #region 公共方法
    /// <summary>
    /// 获取信道
    /// </summary>
    /// <param name="server">服务器配置选项</param>
    /// <remarks>创建好的信道，外部自己监听信道事件<see cref="ChannelProxy.OnError"/>接收信道错误消息</remarks>
    /// <param name="isSend">true：发送消息；false：接收消息。用于读写分开</param>
    /// <returns></returns>
    public async Task<ChannelProxy> GetChannel(bool isSend, IServerOptions server)
    {
        //  查找服务器地址，构建RabbitMQ链接
        ServerDescriptor? descriptor = (this as IServerManager).GetServer(server);
        ThrowIfNull(descriptor);
        Action<string, string> connError = (title, reason) =>
        {
            FileLogger.Error($"RabbitMQ链接异常:{title}", $"{reason}{Environment.NewLine}\t{server.ToString()}");
        };
        //  先获取链接对象，基于链接对象；再构建信道对象
        FactoryProxy factory = FactoryProxy.GetFactory(descriptor!.Server);
        ChannelProxy channel = await factory.GetChanel(isSend, connError: (string title, string reason) =>
        {
            title = isSend ? $"发送消息:{title}" : $"接收消息:{title}";
            FileLogger.Error(title, $"{reason}{Environment.NewLine}\t{server.ToString()}");
        });
        return channel;
    }
    #endregion
}

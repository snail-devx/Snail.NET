using Snail.Abstractions;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Logging.Extensions;
using Snail.Abstractions.Setting.Extensions;
using Snail.Abstractions.Web;
using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Interfaces;
using Snail.RabbitMQ.Components;
using Snail.Web;
using System.Net;

namespace Snail.RabbitMQ;

/// <summary>
/// RabbitMQ管理器，管理服务器地址，进行一些基础逻辑实现
/// </summary>
[Component]
public class RabbitManager : ServerManager, IServerManager
{
    #region 属性变量
    /// <summary>
    /// 应用程序实例
    /// </summary>
    protected readonly IApplication App;
    /// <summary>
    /// 是否是生产环境
    /// </summary>
    protected readonly bool IsProduction;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app">应用程序实例</param>
    public RabbitManager(IApplication app) : base(app, rsCode: "rabbitmq")
    {
        App = ThrowIfNull(app);
        IsProduction = app.IsProduction;
    }
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
            App.LogErrorFile($"RabbitMQ链接异常:{title}", $"{reason}{Environment.NewLine}\t{server.ToString()}");
        };
        //  先获取链接对象，基于链接对象；再构建信道对象
        FactoryProxy factory = FactoryProxy.GetFactory(descriptor!.Server);
        ChannelProxy channel = await factory.GetChanel(isSend, connError: (string title, string reason) =>
        {
            title = isSend ? $"发送消息:{title}" : $"接收消息:{title}";
            App.LogErrorFile(title, $"{reason}{Environment.NewLine}\t{server.ToString()}");
        });
        return channel;
    }
    /// <summary>
    /// 基于环境信息重构名称；若为开发环境，自动追加机器名称
    /// </summary>
    /// <param name="name"></param>
    /// <returns>若name为空，则返回string.Empty；否则返回基于环境构建的name新值</returns>
    public string ReBuildNameByEnvironment(string? name)
    {
        if (string.IsNullOrEmpty(name) == false)
        {
            name = IsProduction
                ? name
                : $"{name}:{Dns.GetHostName()}";
        }
        return name ?? string.Empty;
    }
    #endregion
}

using RabbitMQ.Client;
using Snail.Common.Components;
using Snail.Utilities.Collections;

namespace Snail.RabbitMQ.Components;

/// <summary>
/// RabbitMQ链接工厂代理
/// </summary>
public sealed class FactoryProxy
{
    #region 属性变量
    /// <summary>
    /// 链接工厂映射字典
    /// </summary>
    private static readonly LockMap<string, FactoryProxy> _factoryMap = new();
    /// <summary>
    /// 链接工厂
    /// </summary>
    private readonly IConnectionFactory _factory;
    /// <summary>
    /// 发送消息 链接池；2小时无使用自动过期
    /// </summary>
    private readonly ObjectAsyncPool<ConnectionProxy> _sendPool = new(FromHours(2));
    /// <summary>
    /// 接收消息 链接池；2小时无使用自动过期
    /// </summary>
    private readonly ObjectAsyncPool<ConnectionProxy> _receivePool = new(FromHours(2));
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    public FactoryProxy(IConnectionFactory factory)
    {
        _factory = ThrowIfNull(factory);
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 获取RabbitMQ链接工厂
    /// </summary>
    /// <param name="serverAddress"></param>
    /// <returns></returns>
    public static FactoryProxy GetFactory(string serverAddress)
    {
        ThrowIfNullOrEmpty(serverAddress);
        return _factoryMap.GetOrAdd(serverAddress, address =>
        {
            IConnectionFactory factory = new ConnectionFactory()
            {
                Uri = new Uri(address),
                //  先采用系统默认，后续优化
                //RequestedConnectionTimeout = FromSeconds(60),//   链接超时时间，默认30s
                //RequestedChannelMax = 1000,// 每个连接的最大信道连接数；默认为2047
                //AutomaticRecoveryEnabled = true,//自动重连；默认为true
                RequestedHeartbeat = FromSeconds(10),// 心跳时间改为10，默认为60s
                //NetworkRecoveryInterval = FromSeconds(30),//网络故障恢复间隔时间；默认为5s
            };
            return new FactoryProxy(factory);
        });
    }

    /// <summary>
    /// 获取信道
    /// </summary>
    /// <param name="isSend">是发送消息吗</param>
    /// <param name="connError">RabbitMQ链接错误时的回调委托</param>
    /// <returns></returns>
    public async Task<ChannelProxy> GetChanel(bool isSend, Action<string, string> connError)
    {
        ThrowIfNull(connError);
        //  先获取链接对象，基于链接对象；再构建信道对象
        ChannelProxy? channel = null;
        await (isSend ? _sendPool : _receivePool).GetOrAdd(
            predicate: async proxy =>
            {
                channel = await proxy.GetChannel();
                return channel != null;
            },
            addFunc: async () =>
            {
                IConnection conn = await _factory.CreateConnectionAsync();
                ConnectionProxy proxy = new(conn);
                proxy.OnError += connError;
                channel = await proxy.GetChannel();
                return proxy;
            },
            autoUsing: true
        );
        return channel!;
    }
    #endregion
}

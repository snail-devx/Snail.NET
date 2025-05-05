using Snail.Abstractions.Identity;
using Snail.Abstractions.Identity.Interfaces;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Identity
{
    /// <summary>
    /// 主键Id生成器
    /// </summary>
    [Component<IIdGenerator>(Lifetime = LifetimeType.Transient)]
    public sealed class IdGenerator : IIdGenerator
    {
        #region 属性变量
        /// <summary>
        /// 服务器地址
        /// </summary>
        private readonly IServerOptions? _server;
        /// <summary>
        /// 主键Id提供程序
        /// </summary>
        private readonly IIdProvider _provider;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="app">应用程序实例</param>
        /// <param name="server">用于生成主键Id的服务器地址，和<see cref="IIdProvider"/>配合</param>
        /// <param name="provider">主键Id提供程序，负责进行id生成具体实现；为null则采用默认的</param>
        public IdGenerator(IApplication app, IServerOptions? server, IIdProvider? provider = null)
        {
            ThrowIfNull(app);
            _server = server;
            _provider = provider ?? app.ResolveRequired<IIdProvider>();
        }
        #endregion

        #region IIdGenerator
        /// <summary>
        /// 生成新的主键Id
        /// </summary>
        /// <param name="codeType">>编码类型；默认Default；Provider中可根据此做id区段区分；具体得看实现类是否支持</param>
        /// <returns>新的主键Id值</returns>
        string IIdGenerator.NewId(string? codeType)
            => _provider.NewId(codeType, _server);
        #endregion
    }
}

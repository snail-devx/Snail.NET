using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Database.Enumerations;

namespace Snail.Abstractions.Database.Extensions
{
    /// <summary>
    /// <see cref="IDbManager"/>扩展方法
    /// </summary>
    public static class DbManagerExtensions
    {
        #region 公共方法
        /// <summary>
        /// 获取服务器信息：workspace 为null
        /// </summary>
        /// <param name="manager">数据库管理器实例</param>
        /// <param name="dbCode">数据库编码</param>
        /// <param name="dbType">数据库类型</param>
        /// <returns></returns>
        public static DbServerDescriptor? GetServer(this IDbManager manager, string dbCode, DbType dbType)
            => manager.GetServer(new DbServerOptions() { Workspace = null, DbCode = dbCode, DbType = dbType });
        /// <summary>
        /// 获取服务器信息
        /// </summary>
        /// <param name="manager">数据库管理器实例</param>
        /// <param name="workspace">数据库服务器所属工作空间</param>
        /// <param name="dbCode">数据库编码</param>
        /// <param name="dbType">数据库类型</param>
        /// <returns></returns>
        public static DbServerDescriptor? GetServer(this IDbManager manager, string? workspace, string dbCode, DbType dbType)
            => manager.GetServer(new DbServerOptions() { Workspace = workspace, DbCode = dbCode, DbType = dbType });

        /// <summary>
        /// 尝试获取服务器信息：workspace为null<br />
        ///     1、在仅知道<paramref name="dbCode"/>时<br />
        ///     2、取服务器信息中，第一个匹配workspace为null，<paramref name="dbCode"/>的服务器信息<br />
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="dbCode"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        public static bool TryGetServer(this IDbManager manager, string dbCode, out DbServerDescriptor? server)
            => manager.TryGetServer(workspace: null, dbCode, out server);
        #endregion
    }
}

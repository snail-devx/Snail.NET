using Snail.Abstractions.Database.Enumerations;

namespace Snail.Abstractions.Database.Interfaces
{
    /// <summary>
    /// 接口约束：数据库服务器配置选项
    /// </summary>
    public interface IDbServerOptions
    {
        /// <summary>
        /// 所属工作空间
        /// </summary>
        string? Workspace { get; }

        /// <summary>
        /// 数据库类型
        /// </summary>
        DbType DbType { get; }

        /// <summary>
        /// 数据库编码
        /// </summary>
        string DbCode { get; }
    }
}

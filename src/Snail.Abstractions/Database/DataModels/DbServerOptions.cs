using Snail.Abstractions.Database.Enumerations;
using Snail.Abstractions.Database.Interfaces;

namespace Snail.Abstractions.Database.DataModels;

/// <summary>
/// 数据库服务器配置选项
/// </summary>
public class DbServerOptions : IDbServerOptions
{
    /// <summary>
    /// 所属工作空间
    /// </summary>
    public string? Workspace { init; get; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public required DbType DbType { init; get; }

    /// <summary>
    /// 数据库编码
    /// </summary>
    public required string DbCode { init; get; }
}

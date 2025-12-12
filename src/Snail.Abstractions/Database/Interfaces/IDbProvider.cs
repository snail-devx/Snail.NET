namespace Snail.Abstractions.Database.Interfaces;

/// <summary>
/// 数据库访问层接口
/// <para>1、约束数据库服务器配置 </para>
/// <para>2、【暂不支持】获取表结构视图等数据库相关信息 </para>
/// </summary>
public interface IDbProvider
{
    /// <summary>
    /// 数据库服务器配置选项
    /// </summary>
    protected IDbServerOptions DbServer { get; }
}

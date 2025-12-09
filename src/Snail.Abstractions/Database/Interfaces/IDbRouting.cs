namespace Snail.Abstractions.Database.Interfaces;

/// <summary>
/// 数据库路由接口
///     1、实现实体保存/插入时分片存储；直接在数据库实体上实现即可
///     2、具体能否实现分片存储，还得看数据库和具体<see cref="IDbModelProvider{DbModel}"/>实现类是否是否支持
/// </summary>
public interface IDbRouting
{
    /// <summary>
    /// 获取路由值
    /// </summary>
    /// <returns></returns>
    string GetRouting();
}

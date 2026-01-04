using Snail.Database.Attributes;
using Snail.Database.Components;

namespace Snail.Database.Interfaces;
/// <summary>
/// 接口约束：数据库缓存分析器
/// </summary>
public interface IDbCacheAnalyzer
{
    /// <summary>
    /// 获取主缓存Key
    /// <para>1、在Hash缓存时使用</para>
    /// </summary>
    /// <param name="proxy"></param>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="masterKey">特性标签指定的<see cref="DbCacheAttribute.MasterKey"/>值</param>
    /// <returns></returns>
    string GetMasterKey<DbModel>(DbModelProxy proxy, string? masterKey);

    /// <summary>
    /// 获取数据key
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="proxy"></param>
    /// <param name="model">数据实体实例</param>
    /// <param name="dataKeyPrefix">特性标签指定的<see cref="DbCacheAttribute.DataKeyPrefix"/>值</param>
    /// <returns></returns>
    string GetDataKey<DbModel>(DbModelProxy proxy, DbModel model, string? dataKeyPrefix) where DbModel : class;
    /// <summary>
    /// 获取数据key
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <typeparam name="IdType"></typeparam>
    /// <param name="proxy"></param>
    /// <param name="id">数据主键id</param>
    /// <param name="dataKeyPrefix">特性标签指定的<see cref="DbCacheAttribute.DataKeyPrefix"/>值</param>
    /// <returns></returns>
    string GetDataKey<DbModel, IdType>(DbModelProxy proxy, IdType id, string? dataKeyPrefix) where DbModel : class where IdType : notnull;
}
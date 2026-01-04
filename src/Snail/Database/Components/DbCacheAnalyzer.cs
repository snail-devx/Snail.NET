using Snail.Database.Attributes;
using Snail.Database.Interfaces;
using static Snail.Database.Components.DbModelProxy;

namespace Snail.Database.Components;
/// <summary>
/// 数据缓存分析
/// </summary>
public class DbCacheAnalyzer : IDbCacheAnalyzer
{
    #region 属性变量
    /// <summary>
    /// 默认实例
    /// </summary>
    public static readonly IDbCacheAnalyzer DEFAULT = new DbCacheAnalyzer();
    #endregion

    #region IDbCacheAnalyzer
    /// <summary>
    /// 获取主缓存Key
    /// <para>1、在Hash缓存时使用</para>
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="proxy"></param>
    /// <param name="masterKey">特性标签指定的<see cref="DbCacheAttribute.MasterKey"/>值</param>
    /// <returns></returns>
    string IDbCacheAnalyzer.GetMasterKey<DbModel>(DbModelProxy proxy, string? masterKey)
    {
        if (IsNullOrEmpty(masterKey) == true)
        {
            string msg = $"DbCacheAttribute.MasterKey值为空，无法进行缓存处理";
            throw new ArgumentNullException(msg);
        }
        return masterKey;
    }

    /// <summary>
    /// 获取数据key
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="proxy"></param>
    /// <param name="model">数据实体实例</param>
    /// <param name="dataKeyPrefix">特性标签指定的<see cref="DbCacheAttribute.DataKeyPrefix"/>值</param>
    /// <returns></returns>
    string IDbCacheAnalyzer.GetDataKey<DbModel>(DbModelProxy proxy, DbModel model, string? dataKeyPrefix)
    {
        string? idValue = ExtractDbFieldValue(proxy.PKField, model)?.ToString();
        return GetDataKey(proxy, idValue, dataKeyPrefix);
    }
    /// <summary>
    /// 获取数据key
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <typeparam name="IdType"></typeparam>
    /// <param name="proxy"></param>
    /// <param name="id">数据主键id</param>
    /// <param name="dataKeyPrefix">特性标签指定的<see cref="DbCacheAttribute.DataKeyPrefix"/>值</param>
    /// <returns></returns>
    string IDbCacheAnalyzer.GetDataKey<DbModel, IdType>(DbModelProxy proxy, IdType id, string? dataKeyPrefix)
    {
        string? idValue = BuildDbFieldValue(proxy.PKField, id)?.ToString();
        return GetDataKey(proxy, idValue, dataKeyPrefix);
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 获取数据key
    /// </summary>
    /// <param name="proxy"></param>
    /// <param name="idValue"></param>
    /// <param name="dataKeyPrefix"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static string GetDataKey(DbModelProxy proxy, string? idValue, string? dataKeyPrefix)
    {
        if (IsNullOrEmpty(idValue) == true)
        {
            string msg = $"DbModel实体主键Id值为空，无法进行缓存数据：{idValue}";
            throw new ArgumentException(msg);
        }
        if (IsNullOrEmpty(dataKeyPrefix) == false)
        {
            idValue = $"{dataKeyPrefix}{idValue}";
        }
        return idValue;
    }
    #endregion
}
using System;
using System.Collections.Generic;

namespace Snail.Aspect.Distribution.Utils;

/// <summary>
/// Cache面相切面编程助手方法；用于运行时辅助操作缓存数据
/// </summary>
public static class CacheAspectHelper
{
    #region 公共方法
    /// <summary>
    /// 组合缓存数据Key
    /// </summary>
    /// <param name="dataKey"></param>
    /// <param name="dataKeyPrefix"></param>
    /// <returns></returns>
    public static string CombineDataKey(string dataKey, string dataKeyPrefix)
    {
        return string.IsNullOrEmpty(dataKeyPrefix)
            ? dataKey
            : $"{dataKeyPrefix}{dataKey}";
    }
    /// <summary>
    /// 组合缓存数据Key
    /// </summary>
    /// <param name="dataKeys"></param>
    /// <param name="dataKeyPrefix"></param>
    /// <returns></returns>
    public static IList<string> CombineDataKey(IList<string> dataKeys, string dataKeyPrefix)
    {
        if (dataKeys?.Count > 0)
        {
            IList<string> cacheKeys = new List<string>(dataKeys.Count);
            for (int index = 0; index < dataKeys.Count; index++)
            {
                cacheKeys.Add(CombineDataKey(dataKeys[index], dataKeyPrefix));
            }
            return cacheKeys;
        }
        return dataKeys;
    }
    /// <summary>
    /// 组合缓存数据Key
    /// </summary>
    /// <param name="dataKeys"></param>
    /// <param name="dataKeyPrefix"></param>
    /// <returns></returns>
    public static string[] CombineDataKey(string[] dataKeys, string dataKeyPrefix)
    {
        if (dataKeys?.Length > 0)
        {
            string[] cacheKeys = new string[dataKeys.Length];
            for (int index = 0; index < dataKeys.Length; index++)
            {
                cacheKeys[index] = CombineDataKey(dataKeys[index], dataKeyPrefix);
            }
            return cacheKeys;
        }
        return dataKeys;
    }

    /// <summary>
    /// 构建缓存数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data">要缓存的数据对象</param>
    /// <param name="keyFunc">获取数据Key的委托</param>
    /// <param name="dataKeyPrefix">数据Key的前缀，无则忽略</param>
    /// <returns>保存缓存时所需的字典数据</returns>
    public static IDictionary<string, T> BuildCacheMap<T>(T data, Func<T, string> keyFunc, string dataKeyPrefix = null)
    {
        ThrowIfNull(data, "data");
        ThrowIfNull(keyFunc, "keyFunc");
        dataKeyPrefix = dataKeyPrefix == null ? string.Empty : dataKeyPrefix;
        return new Dictionary<string, T>()
        {
            {$"{dataKeyPrefix}{keyFunc(data)}",data}
        };
    }
    /// <summary>
    /// 构建缓存数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="datas">要缓存的数据对象</param>
    /// <param name="keyFunc">获取数据Key的委托</param>
    /// <param name="dataKeyPrefix">数据Key的前缀，无则忽略</param>
    /// <returns>保存缓存时所需的字典数据</returns>
    public static IDictionary<string, T> BuildCacheMap<T>(IList<T> datas, Func<T, string> keyFunc, string dataKeyPrefix = null)
    {
        ThrowIfNull(datas, "datas");
        ThrowIfNull(keyFunc, "keyFunc");
        dataKeyPrefix = dataKeyPrefix == null ? string.Empty : dataKeyPrefix;
        IDictionary<string, T> map = new Dictionary<string, T>();
        for (int index = 0; index < datas.Count; index++)
        {
            T data = datas[index];
            ThrowIfNull(data, $"datas[{index}] is null");
            map[$"{dataKeyPrefix}{keyFunc(data)}"] = data;
        }
        return map;
    }
    /// <summary>
    /// 构建缓存数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="datas">要缓存的数据对象</param>
    /// <param name="keyFunc">获取数据Key的委托</param>
    /// <param name="dataKeyPrefix">数据Key的前缀，无则忽略</param>
    /// <returns>保存缓存时所需的字典数据</returns>
    public static IDictionary<string, T> BuildCacheMap<T>(T[] datas, Func<T, string> keyFunc, string dataKeyPrefix = null)
    {
        ThrowIfNull(datas, "datas");
        ThrowIfNull(keyFunc, "keyFunc");
        dataKeyPrefix = dataKeyPrefix == null ? string.Empty : dataKeyPrefix;
        IDictionary<string, T> map = new Dictionary<string, T>();
        for (int index = 0; index < datas.Length; index++)
        {
            T data = datas[index];
            ThrowIfNull(data, $"datas[{index}] is null");
            map[$"{dataKeyPrefix}{keyFunc(data)}"] = data;
        }
        return map;
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 为空时报错
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="datas"></param>
    /// <param name="paramName"></param>
    private static void ThrowIfNull<T>(T datas, string paramName = null)
    {
        if (datas == null)
        {
            throw new ArgumentNullException(paramName, "is null");
        }
    }
    #endregion
}

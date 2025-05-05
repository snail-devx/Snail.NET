using System;
using Snail.Abstractions.Identity.Interfaces;
using Snail.Utilities.Common.Extensions;

namespace Snail.Abstractions.Identity.Extensions;

/// <summary>
/// <see cref="IIdentity"/>扩展方法
/// </summary>
public static class IIdentityExtensions
{
    #region 公共方法
    /// <summary>
    /// 将<paramref name="data"/>添加到字典中 <br />
    ///     1、<see cref="IIdentity.Id"/>作为字典的Key值 <br />
    ///     2、若key值为null，则忽略添加；若<paramref name="map"/>为null，则忽略
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="map"></param>
    /// <returns></returns>
    public static T AddTo<T>(this T data, IDictionary<string, T> map) where T : IIdentity
    {
        if (map != null && data.Id != null)
        {
            map[data.Id] = data;
            return data;
        }
        return data;
    }
    /// <summary>
    /// 将<paramref name="datas"/>数据添加到字典中 <br />
    ///     1、<see cref="IIdentity.Id"/>作为字典的Key值 <br />
    ///     2、若key值为null，则忽略添加；若<paramref name="map"/>为null，则忽略
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="datas"></param>
    /// <param name="map"></param>
    /// <returns></returns>
    public static IList<T> AddTo<T>(this IList<T> datas, IDictionary<string, T> map) where T : IIdentity
    {
        foreach (T data in datas)
        {
            data.AddTo(map);
        }
        return datas;
    }
    #endregion
}

using Snail.Abstractions.Identity.Interfaces;

namespace Snail.Abstractions.Identity.Extensions;

/// <summary>
/// <see cref="IIdentity"/>扩展方法
/// </summary>
public static class IIdentityExtensions
{
    #region 公共方法
    extension<T>(IList<T> datas) where T : IIdentity
    {
        /// <summary>
        /// 转为字典
        /// <para>1、key为<see cref="IIdentity.Id"/>值，value为数据自身</para>
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, T> ToDictionary()
        {
            Dictionary<string, T> map = new Dictionary<string, T>();
            foreach (var data in datas)
            {
                ThrowIfNull(data, "data为null，无法基于IIdentity取数据主键Id值");
                string key = ThrowIfNullOrEmpty(((IIdentity)data).Id);
                map[key] = data;
            }
            return map;
        }

        /// <summary>
        /// 将<paramref name="datas"/>数据添加到字典中
        /// <para>1、<see cref="IIdentity.Id"/>作为字典的Key值 </para>
        /// <para>2、若key值为null，则忽略添加；若<paramref name="map"/>为null，则忽略 </para>
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public IList<T> AddTo(IDictionary<string, T> map)
        {
            foreach (T data in datas)
            {
                data.AddTo(map);
            }
            return datas;
        }
    }

    extension<T>(T data) where T : IIdentity
    {
        /// <summary>
        /// 将<paramref name="data"/>添加到字典中
        /// <para>1、<see cref="IIdentity.Id"/>作为字典的Key值 </para>
        /// <para>2、若key值为null，则忽略添加；若<paramref name="map"/>为null，则忽略 </para>
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public T AddTo(IDictionary<string, T> map)
        {
            if (map != null && data.Id != null)
            {
                map[data.Id] = data;
                return data;
            }
            return data;
        }
    }
    #endregion
}

using System.Runtime.Serialization;

namespace Snail.Utilities.Common.Extensions
{
    /// <summary>
    /// 序列化信息助手类
    /// </summary>
    public static class SerializationInfoExtensions
    {
        #region 公共方法

        #region AddValue：尝试添加序列化；null或者空数据不加入
        /// <summary>
        /// 添加有效数据进行JSON序列化<br />
        ///     1、==null 不加入json序列化<br />
        ///     2、空字符串不加入JSON序列化<br />
        /// </summary>
        /// <param name="info">JSON序列化信息对象；存储对对象进行序列化或反序列化所需的全部数据</param>
        /// <param name="key">JSON的Key值</param>
        /// <param name="value">要进行序列化的数据</param>
        /// <returns>JSON序列化信息对象；方便链式调用</returns>
        public static SerializationInfo TryAddValue(this SerializationInfo info, string key, object? value)
        {
            //  无效数据，不予添加：null、空字符串、空集合
            bool isInValid = value == null || value is string str && str.Length == 0;
            if (isInValid == false)
            {
                info.AddValue(key, value);
            }
            return info;
        }

        /// <summary>
        /// 添加有效<see cref="string"/>进行JSON序列化<br />
        ///     1、null、空字符串不进行JSON序列化<br />
        /// </summary>
        /// <param name="info">JSON序列化信息对象；存储对对象进行序列化或反序列化所需的全部数据</param>
        /// <param name="key">JSON的Key值</param>
        /// <param name="value">要进行序列化的数据</param>
        /// <returns>JSON序列化信息对象；方便链式调用</returns>
        public static SerializationInfo TryAddValue(this SerializationInfo info, string key, string? value)
        {
            if (value?.Length > 0)
            {
                info.AddValue(key, value);
            }
            return info;
        }
        /// <summary>
        /// 添加有效数据<see cref="IList{T}"/>进行JSON序列化<br />
        ///     1、==null、空集合 不加入JSON序列化<br />
        /// </summary>
        /// <param name="info">JSON序列化信息对象；存储对对象进行序列化或反序列化所需的全部数据</param>
        /// <param name="key">JSON的Key值</param>
        /// <param name="value">要进行序列化的数据</param>
        /// <returns>JSON序列化信息对象；方便链式调用</returns>
        public static SerializationInfo TryAddValue<T>(this SerializationInfo info, string key, IList<T>? value)
        {
            if (value?.Count > 0)
            {
                info.AddValue(key, value);
            }
            return info;
        }
        /// <summary>
        /// 添加有效数据<typeparamref name="T"/>[]进行JSON序列化<br />
        ///     1、==null、空数组 不加入JSON序列化<br />
        /// </summary>
        /// <param name="info">JSON序列化信息对象；存储对对象进行序列化或反序列化所需的全部数据</param>
        /// <param name="key">JSON的Key值</param>
        /// <param name="value">要进行序列化的数据</param>
        /// <returns>JSON序列化信息对象；方便链式调用</returns>
        public static SerializationInfo TryAddValue<T>(this SerializationInfo info, string key, T[]? value)
        {
            if (value?.Length > 0)
            {
                info.AddValue(key, value);
            }
            return info;
        }
        #endregion

        #endregion
    }
}

using Newtonsoft.Json;

namespace Snail.Utilities.Common.Extensions;
/// <summary>
/// Object扩展；针对所有类型
/// </summary>
public static class ObjectExtensions
{
    #region 属性变量
    /// <summary>
    /// 忽略null值
    /// </summary>
    private static readonly JsonSerializerSettings _ignoreNullValue = new JsonSerializerSettings()
    {
        NullValueHandling = NullValueHandling.Ignore,
    };
    #endregion

    #region 序列化和反序列化
    /// <summary>
    /// 将对象序列化JSON字符串
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="serializerSettings">JSON序列化配置，默认null</param>
    /// <returns></returns>
    public static string AsJson<T>(this T obj, in JsonSerializerSettings? serializerSettings = null)
    {
        return serializerSettings == null
            ? JsonConvert.SerializeObject(obj, Formatting.None)
            : JsonConvert.SerializeObject(obj, serializerSettings);
    }
    /// <summary>
    /// 将对象序列化JSON字符串
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="ignoreNullValue">是否忽略null值，为true时，若key对应value为null，则忽略掉</param>
    /// <returns></returns>
    public static string AsJson<T>(this T obj, in bool ignoreNullValue)
    {
        return ignoreNullValue == true
            ? JsonConvert.SerializeObject(obj, Formatting.None, _ignoreNullValue)
            : JsonConvert.SerializeObject(obj, Formatting.None);
    }
    #endregion

    #region 字典、集合操作
    /// <summary>
    /// 将数据追加到列表中
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="list"></param>
    /// <returns>当前对象；方便链式调用</returns>
    public static T AddTo<T>(this T obj, IList<T> list)
    {
        list.Add(obj);
        return obj;
    }
    /// <summary>
    /// 将数据插入到列表中索引0位置
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public static T InsertTo<T>(this T obj, IList<T> list)
    {
        list?.Insert(0, obj);
        return obj;
    }
    /// <summary>
    /// 构建一个<see cref="List{T}"/>对象，并把当前obj加入集合中 <br />
    ///     1、解决到处 new <see cref="List{String}"/> { str }的问题
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static List<T> AsList<T>(this T obj) => [obj];
    /// <summary>
    /// 从<paramref name="list"/>中移除<paramref name="obj"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="list"></param>
    /// <returns>当前对象；方便链式调用</returns>
    public static T RemoveFrom<T>(this T obj, IList<T> list)
    {
        list.Remove(obj);
        return obj;
    }


    ///// <summary>
    ///// 设置字典值；内部使用 dict[key]=value赋值
    ///// </summary>
    ///// <typeparam name="TKey">字典Key类型</typeparam>
    ///// <typeparam name="TValue">字典Value类型</typeparam>
    ///// <param name="value">字典value</param>
    ///// <param name="dict">字典对象</param>
    ///// <param name="key">字典key</param>
    ///// <returns>value对象，方便做链式调用</returns>
    //public static TValue SetValueToDict<TKey, TValue>(this TValue value, in IDictionary<TKey, TValue> dict, in TKey key) where TKey : notnull
    //{
    //    ThrowIfNull(key);
    //    dict[key] = value;
    //    return value;
    //}
    #endregion

    #region 其他
    /// <summary>
    /// 尝试进行对象销毁。
    ///     1、若对象实现了IDisposable接口，则调用其Dispose方法
    /// </summary>
    /// <param name="obj"></param>
    public static void TryDispose(this object obj)
    {
        try
        {
            //  不能用as；1 as IDisposable 会报错
            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch { }
    }
    #endregion
}

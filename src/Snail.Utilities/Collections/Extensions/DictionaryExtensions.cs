namespace Snail.Utilities.Collections.Extensions
{
    /// <summary>
    /// 字典对象扩展
    /// </summary>
    public static class DictionaryExtensions
    {
        #region Any、ForEach
        /// <summary>
        /// 字典是否有值 <br />
        ///     1、不是null、不是空字典 <br />
        ///     2、直接使用自身类型属性判断；不用.Any <br />
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static bool Any<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
            => dictionary.Count != 0;

        /// <summary>
        /// 遍历字典
        /// </summary>
        /// <typeparam name="TKey">字典Key类型</typeparam>
        /// <typeparam name="TValue">字典Value类型</typeparam>
        /// <param name="dict">字典对象</param>
        /// <param name="each">遍历Action</param>
        public static void ForEach<TKey, TValue>(this IDictionary<TKey, TValue> dict, in Action<TKey, TValue> each) where TKey : notnull
        {
            /* each取消in，避免外部遍历时传入直接传入class中方法报错； CS1503 参数 2: 无法从“方法组”转换为“System.Action <System.Action<TKey,TValue>>” */
            ThrowIfNull(each);
            foreach (var kv in dict)
            {
                each(kv.Key, kv.Value);
            }
        }
        #endregion

        #region GetOrAdd、Set、Combine、Remove
        /// <summary>
        /// 从字典中获取值；取不到则添加 <br />
        ///     1、线程安全；唯一性；使用<paramref name="dict"/>做lock <br />
        ///     2、确保addFunc只会被调用一次；解决ConcurrentDictionary中委托调用多次的问题<br />
        ///     3、确保写入的同步，不会对字典数据进行遍历读取，否则会报错<br />
        /// </summary>
        /// <typeparam name="TKey">字典Key类型</typeparam>
        /// <typeparam name="TValue">字典Value类型</typeparam>
        /// <param name="dict">要操作的字典；取不到时，会对此对象加锁</param>
        /// <param name="key">取值key</param>
        /// <param name="addFunc">key不存在时，用于取值添加的委托</param>
        /// <returns>key对象的value值</returns>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, in TKey key, in Func<TKey, TValue> addFunc) where TKey : notnull
        {
            ThrowIfNull(key);
            ThrowIfNull(addFunc);
            if (dict.TryGetValue(key, out TValue? value) != true)
            {
                lock (dict)
                {
                    if (dict.TryGetValue(key, out value) != true)
                    {
                        value = addFunc(key);
                        dict.Add(key, value);
                    }
                }
            }
            return value;
        }

        /// <summary>
        /// 为字典设置Key-Value值；不存在添加，存在覆盖
        /// </summary>
        /// <typeparam name="TKey">Key类型</typeparam>
        /// <typeparam name="TValue">Value类型</typeparam>
        /// <param name="dict">字典对象</param>
        /// <param name="key">key值</param>
        /// <param name="value">value值</param>
        /// <returns>字典本身；方便实现链式调用</returns>
        public static IDictionary<TKey, TValue> Set<TKey, TValue>(this IDictionary<TKey, TValue> dict, in TKey key, in TValue value) where TKey : notnull
        {
            if (key != null)
            {
                dict[key] = value;
            }
            return dict;
        }
        /// <summary>
        /// 字典合并 <br />
        ///     1、将指定的多个字典对象合并给当前字典。 <br />
        ///     2、key存在则覆盖，不存在添加 <br />
        /// </summary>
        /// <typeparam name="TKey">Key类型</typeparam>
        /// <typeparam name="TValue">Value类型</typeparam>
        /// <param name="dict">字典对象</param>
        /// <param name="dicts">要合并给当前字典的对象数据</param>
        /// <returns>字典本身；方便实现链式调用</returns>
        public static IDictionary<TKey, TValue> Combine<TKey, TValue>(this IDictionary<TKey, TValue> dict, params IDictionary<TKey, TValue>?[] dicts) where TKey : notnull
        {
            //  嵌套有点多，后续考虑用.ForEach优化，
            if (dicts?.Any() == true)
            {
                foreach (var di in dicts)
                {
                    if (di?.Any() == true)
                    {
                        foreach (var kv in di)
                        {
                            dict[kv.Key] = kv.Value;
                        }
                    }
                }
            }
            return dict;
        }
        /// <summary>
        /// 尝试移除字段中的指定Key
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key">要移除的Key</param>
        /// <param name="value">移除成功时，移除的Key对应的Value值</param>
        /// <returns>是否移除成功</returns>
        public static bool TryRemove<TKey, TValue>(this IDictionary<TKey, TValue> dict, in TKey key, out TValue? value) where TKey : notnull
        {
            if (dict.TryGetValue(key, out value) == true)
            {
                dict.Remove(key);
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
        #endregion
    }
}

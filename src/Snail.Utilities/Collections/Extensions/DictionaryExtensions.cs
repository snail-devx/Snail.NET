namespace Snail.Utilities.Collections.Extensions;

/// <summary>
/// 字典对象扩展
/// </summary>
public static class DictionaryExtensions
{
    extension<TKey, TValue>(IDictionary<TKey, TValue> dictionary) where TKey : notnull
    {
        #region Any、ForEach
        /// <summary>
        /// 字典是否有值 <br />
        ///     1、不是null、不是空字典 <br />
        ///     2、直接使用自身类型属性判断；不用.Any <br />
        /// </summary>
        /// <returns></returns>
        public bool Any() => dictionary.Count != 0;
        /// <summary>
        /// 遍历字典
        /// </summary>
        /// <param name="each">遍历Action</param>
        public void ForEach(in Action<TKey, TValue> each)
        {
            /* each取消in，避免外部遍历时传入直接传入class中方法报错； CS1503 参数 2: 无法从“方法组”转换为“System.Action <System.Action<TKey,TValue>>” */
            ThrowIfNull(each);
            foreach (var kv in dictionary)
            {
                each(kv.Key, kv.Value);
            }
        }
        #endregion

        #region GetOrAdd、Set、Combine、Remove
        /// <summary>
        /// 从字典中获取值；取不到则添加 <br />
        ///     1、线程安全；唯一性；使用 字典实例 做lock <br />
        ///     2、确保addFunc只会被调用一次；解决ConcurrentDictionary中委托调用多次的问题<br />
        ///     3、确保写入的同步，不会对字典数据进行遍历读取，否则会报错<br />
        /// </summary>
        /// <param name="key">取值key</param>
        /// <param name="addFunc">key不存在时，用于取值添加的委托</param>
        /// <returns>key对象的value值</returns>
        public TValue GetOrAdd(in TKey key, in Func<TKey, TValue> addFunc)
        {
            ThrowIfNull(key);
            ThrowIfNull(addFunc);
            if (dictionary.TryGetValue(key, out TValue? value) != true)
            {
                lock (dictionary)
                {
                    if (dictionary.TryGetValue(key, out value) != true)
                    {
                        value = addFunc(key);
                        dictionary.Add(key, value);
                    }
                }
            }
            return value;
        }

        /// <summary>
        /// 为字典设置Key-Value值；不存在添加，存在覆盖
        /// </summary>
        /// <param name="key">key值</param>
        /// <param name="value">value值</param>
        /// <returns>字典本身；方便实现链式调用</returns>
        public IDictionary<TKey, TValue> Set(in TKey key, in TValue value)
        {
            if (key != null)
            {
                dictionary[key] = value;
            }
            return dictionary;
        }
        /// <summary>
        /// 字典合并 <br />
        ///     1、将指定的多个字典对象合并给当前字典。 <br />
        ///     2、key存在则覆盖，不存在添加 <br />
        /// </summary>
        /// <param name="dicts">要合并给当前字典的对象数据</param>
        /// <returns>字典本身；方便实现链式调用</returns>
        public IDictionary<TKey, TValue> Combine(params IDictionary<TKey, TValue>?[] dicts)
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
                            dictionary[kv.Key] = kv.Value;
                        }
                    }
                }
            }
            return dictionary;
        }
        /// <summary>
        /// 尝试移除字段中的指定Key
        /// </summary>
        /// <param name="key">要移除的Key</param>
        /// <param name="value">移除成功时，移除的Key对应的Value值</param>
        /// <returns>是否移除成功</returns>
        public bool TryRemove(in TKey key, out TValue? value)
        {
            if (dictionary.TryGetValue(key, out value) == true)
            {
                dictionary.Remove(key);
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
using System.Security.Cryptography.X509Certificates;

namespace Snail.Utilities.Collections.Extensions
{
    /// <summary>
    /// <see cref="IList{T}" />相关扩展方法；IEnumerable相关；避免进行IEnumerable转换
    /// </summary>
    public static class ListExtensions
    {
        #region Add、AddRange、Insert、、、
        /// <summary>
        /// 尝试添加元素，值为null则不添加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IList<T> TryAdd<T>(this IList<T> lst, T? value)
        {
            if (value != null)
            {
                lst.Add(value);
            }
            return lst;
        }
        /// <summary>
        /// 尝试将<paramref name="datas"/>集合数据，追加到<paramref name="lst"/>中
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <param name="datas"></param>
        /// <returns></returns>
        public static IList<T> TryAddRange<T>(this IList<T> lst, IEnumerable<T>? datas)
        {
            datas?.AppendTo(lst);
            return lst;
        }
        /// <summary>
        /// 尝试将<paramref name="datas"/>集合数据，追加到<paramref name="lst"/>中
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <param name="datas"></param>
        /// <returns></returns>
        public static IList<T> TryAddRange<T>(this IList<T> lst, IList<T>? datas)
        {
            datas?.ForEach(lst.Add);
            return lst;
        }

        /// <summary>
        /// 尝试将<paramref name="data"/>插入到<paramref name="lst"/>中<br />
        ///     1、插入到索引0位置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IList<T> TryInsert<T>(this IList<T> lst, T? data)
        {
            if (data != null)
            {
                lst.Insert(0, data);
            }
            return lst;
        }
        /// <summary>
        /// 尝试将<paramref name="datas"/>插入到<paramref name="lst"/>中 <br />
        ///     1、插入到索引0位置<br />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <param name="datas"></param>
        /// <returns></returns>
        public static IList<T> TryInsertRange<T>(this IList<T> lst, IList<T>? datas)
        {
            if (datas?.Count > 0)
            {
                foreach (var item in datas.Reverse())
                {
                    lst.Insert(0, item);
                }
            }
            return lst;
        }
        /// <summary>
        /// 将<paramref name="lst"/>插入到<paramref name="target"/>列表集合中<br />
        ///     1、插入到索引0位置<br />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static IList<T> InsertTo<T>(this IList<T> lst, List<T> target)
        {
            target?.InsertRange(0, lst);
            return lst;
        }
        /// <summary>
        /// 将<paramref name="lst"/>插入到<paramref name="target"/>列表集合中<br />
        ///     1、插入到索引0位置<br />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static IList<T> InsertTo<T>(this IList<T> lst, IList<T> target)
        {
            if (target != null)
            {
                for (int index = 0; index < lst.Count; index++)
                {
                    target.Insert(index, lst[index]);
                }
            }
            return lst;
        }
        #endregion

        #region First、FirstOrDefault、Last、LastOrDefault、IndexOf、LastIndexOf
        /// <summary>
        /// 获取集合第一个元素；无则报错
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <returns></returns>
        public static T First<T>(this IList<T> lst)
            => lst.Count > 0 ? lst[0] : throw new InvalidOperationException("lst is empty");
        /// <summary>
        /// 获取集合第一个元素，不存在则返回默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <returns></returns>
        public static T? FirstOrDefault<T>(this IList<T> lst)
            => lst.Count > 0 ? lst[0] : default;
        /// <summary>
        /// 获取集合的最后一个元素；无责报错
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <returns></returns>
        public static T Last<T>(this IList<T> lst)
            => lst.Count > 0 ? lst[lst.Count - 1] : throw new InvalidOperationException("lst is empty");
        /// <summary>
        /// 获取集合的最后一个元素，不存在则返回默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <returns></returns>
        public static T? LastOrDefault<T>(this IList<T> lst)
            => lst.Count > 0 ? lst[lst.Count - 1] : default;

        /// <summary>
        /// 从前往后 查找元素在列表中的第一个位置索引
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="predicate">断言委托</param>
        /// <returns>存在返回索引位置，否则返回-1</returns>
        public static int IndexOf<T>(this IList<T> list, in Predicate<T> predicate)
        {
            ThrowIfNull(predicate);
            for (var index = 0; index < list.Count; index++)
            {
                if (predicate(list[index]) == true)
                {
                    return index;
                }
            }
            return -1;
        }
        /// <summary>
        /// 从后往前 查找元素在列表中的第一个位置索引
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="predicate">断言委托</param>
        /// <returns>存在返回索引位置，否则返回-1</returns>
        public static int LastIndexOf<T>(this IList<T> list, in Predicate<T> predicate)
        {
            ThrowIfNull(predicate);
            for (var index = list.Count - 1; index >= 0; index--)
            {
                if (predicate(list[index]) == true)
                {
                    return index;
                }
            }
            return -1;
        }
        #endregion

        #region Any、ForEach
        /// <summary>
        /// 列表是否有值 <br />
        ///     1、不是null、不是空列表 <br />
        ///     2、直接使用自身类型属性判断；不用.Any <br />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool Any<T>(this IList<T> list)
            => list.Count != 0;

        /// <summary>
        /// 遍历列表 <br />
        ///     1、不能终止循环遍历 <br />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <param name="each"></param>
        public static void ForEach<T>(this IList<T> lst, Action<T> each)
        {
            ThrowIfNull(each);
            for (int index = 0; index < lst.Count; index++)
            {
                each(lst[index]);
            }
        }
        #endregion
    }
}

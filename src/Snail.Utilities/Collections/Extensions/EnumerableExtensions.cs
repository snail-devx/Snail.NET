using Snail.Utilities.Collections.Extensions;

namespace Snail.Utilities.Collections.Extensions
{
    /// <summary>
    /// <see cref="IEnumerable{T}"/>对象扩展方法
    /// </summary>
    public static class EnumerableExtensions
    {
        #region 和Ilist交互
        /// <summary>
        /// 将ts数据追加到指定的list集合中
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ts">当前可枚举数据</param>
        /// <param name="list">目标集合；追加数据时，若list为null，自动构建一个新的</param>
        public static void AppendTo<T>(this IEnumerable<T> ts, ref List<T>? list)
        {
            //  ts空不做处理；需要追加数据时，对list做为null初始化
            if (ts.Any() == true)
            {
                list ??= new List<T>();
                list.AddRange(ts);
            }
        }
        /// <summary>
        /// 将<paramref name="sources"/>数据追加到<paramref name="target"/>列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sources">源数据</param>
        /// <param name="target">追加到</param>
        /// <returns>源数据对象</returns>
        public static IEnumerable<T> AppendTo<T>(this IEnumerable<T> sources, List<T> target)
        {
            ThrowIfNull(target);
            target.AddRange(sources);
            return sources;
        }
        /// <summary>
        /// 将<paramref name="sources"/>数据追加到<paramref name="target"/>列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sources">源数据</param>
        /// <param name="target">追加到</param>
        /// <returns>源数据对象</returns>
        public static IEnumerable<T> AppendTo<T>(this IEnumerable<T> sources, in IList<T> target)
        {
            ThrowIfNull(target);
            foreach (var item in sources)
            {
                target.Add(item);
            }
            return sources;
        }
        #endregion

        #region 转换
        /// <summary>
        /// 遍历数据拼接成字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ts"></param>
        /// <param name="separator">拼接的字符</param>
        /// <returns></returns>
        public static string AsString<T>(this IEnumerable<T> ts, char separator)
            => string.Join(separator, ts);
        /// <summary>
        /// 遍历数据拼接成字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ts"></param>
        /// <param name="separator">拼接的字符</param>
        /// <returns></returns>
        public static string AsString<T>(this IEnumerable<T> ts, string separator)
            => string.Join(separator, ts);
        #endregion
    }
}

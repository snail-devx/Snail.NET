using Snail.Utilities.Collections.Extensions;

namespace Snail.Utilities.Collections.Extensions;
/// <summary>
/// <see cref="IEnumerable{T}"/>对象扩展方法
/// </summary>
public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T> ts)
    {
        #region ForEach
        /// <summary>
        /// 遍历数据
        ///     1、不能终止循环遍历
        /// </summary>
        /// <param name="each"></param>
        public void ForEach(Action<T> each)
        {
            ThrowIfNull(each);
            foreach (T item in ts)
            {
                each(item);
            }
        }
        /// <summary>
        /// 遍历数据，给出当前数据索引位置
        ///     1、不能终止循环遍历
        ///     2、遍历动作可得到当前元素索引位置
        /// </summary>
        /// <param name="each">遍历处理动作；参数：索引位置、当前元素</param>
        public void ForEach(Action<int, T> each)
        {
            ThrowIfNull(each);
            int index = 0;
            foreach (T item in ts)
            {
                each(index, item);
                index += 1;
            }
        }
        #endregion

        #region 和Ilist交互
        /// <summary>
        /// 将ts数据追加到指定的list集合中
        /// </summary>
        /// <param name="list">目标集合；追加数据时，若list为null，自动构建一个新的</param>
        public void AppendTo(ref List<T>? list)
        {
            //  ts空不做处理；需要追加数据时，对list做为null初始化
            if (ts.Any() == true)
            {
                list ??= new List<T>();
                list.AddRange(ts);
            }
        }
        /// <summary>
        /// 将<paramref name="ts"/>数据追加到<paramref name="target"/>列表
        /// </summary>
        /// <param name="target">追加到的目标列表对象</param>
        /// <returns>源数据对象</returns>
        public IEnumerable<T> AppendTo(List<T> target)
        {
            ThrowIfNull(target);
            target.AddRange(ts);
            return ts;
        }
        /// <summary>
        /// 将<paramref name="ts"/>数据追加到<paramref name="target"/>列表
        /// </summary>
        /// <param name="target">追加到的目标列表对象</param>
        /// <returns>源数据对象</returns>
        public IEnumerable<T> AppendTo(IList<T> target)
        {
            ThrowIfNull(target);
            foreach (var item in ts)
            {
                target.Add(item);
            }
            return ts;
        }
        #endregion

        #region 转换
        /// <summary>
        /// 遍历数据拼接成字符串
        /// </summary>
        /// <param name="separator">拼接的字符</param>
        /// <returns></returns>
        public string AsString(char separator) => Join(separator, ts);
        /// <summary>
        /// 遍历数据拼接成字符串
        /// </summary>
        /// <param name="separator">拼接的字符</param>
        /// <returns></returns>
        public string AsString(string separator) => Join(separator, ts);
        #endregion
    }
}

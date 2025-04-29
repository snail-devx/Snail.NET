namespace Snail.Utilities.Common.Extensions
{
    /// <summary>
    /// <see cref="Array"/> 扩展；IEnumerable相关；避免进行IEnumerable转换
    /// </summary>
    public static class ArrayExtensions
    {
        #region First、FirstOrDefault、Last、LastOrDefault、IndexOf、LastIndexOf
        /// <summary>
        /// 获取数组第一个元素；无则报错
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T First<T>(this T[] array)
            => array.Length > 0 ? array[0] : throw new InvalidOperationException("array is empty");
        /// <summary>
        /// 获取数组第一个元素，不存在则返回默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T? FirstOrDefault<T>(this T[] array)
            => array.Length > 0 ? array[0] : default;
        /// <summary>
        /// 获取数组的最后一个元素；无责报错
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T Last<T>(this T[] array)
            => array.Length > 0 ? array[^1] : throw new InvalidOperationException("array is empty");
        /// <summary>
        /// 获取数组的最后一个元素，不存在则返回默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T? LastOrDefault<T>(this T[] array)
            => array.Length > 0 ? array[^1] : default;

        /// <summary>
        /// 从前往后 搜索元素在数组中的第一个位置索引
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr"></param>
        /// <param name="obj"></param>
        /// <returns>找到则为第一个匹配项的索引；否则-1。</returns>
        public static int IndexOf<T>(this T[] arr, in T obj)
            => Array.IndexOf(arr, obj);
        /// <summary>
        /// 从后往前 搜索元素在数组中的最后一个位置索引
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr"></param>
        /// <param name="obj"></param>
        /// <returns>找到则为最后一个匹配项的索引；否则-1。</returns>
        public static int LastIndexOf<T>(this T[] arr, in T obj)
            => Array.LastIndexOf(arr, obj);
        #endregion

        #region Any、ForEach
        /// <summary>
        /// 数组是否有值 <br />
        ///     1、不是null、不是空数组 <br />
        ///     2、直接使用自身类型属性判断；不用.Any <br />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static bool Any<T>(this T[] array)
            => array.Length != 0;
        /// <summary>
        /// 数据是否存在符合条件数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static bool Any<T>(this T[] array, in Predicate<T> predicate)
        {
            ThrowIfNull(predicate);
            for (int index = 0; index < array.Length; index++)
            {
                if (predicate(array[index]) == true)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 遍历数据 <br />
        ///     1、不能终止循环遍历
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="each"></param>
        public static void ForEach<T>(this T[] array, in Action<T> each)
        {
            ThrowIfNull(each);
            for (int index = 0; index < array.Length; index++)
            {
                each(array[index]);
            }
        }
        #endregion
    }
}

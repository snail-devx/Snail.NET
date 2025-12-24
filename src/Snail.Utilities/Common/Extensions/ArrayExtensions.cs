namespace Snail.Utilities.Common.Extensions;
/// <summary>
/// <see cref="Array"/> 扩展；IEnumerable相关；避免进行IEnumerable转换
/// </summary>
public static class ArrayExtensions
{
    extension<T>(T[] array)
    {
        #region First、FirstOrDefault、Last、LastOrDefault、IndexOf、LastIndexOf
        /// <summary>
        /// 获取数组第一个元素；无则报错
        /// </summary>
        /// <returns></returns>
        public T First() => array.Length > 0 ? array[0] : throw new InvalidOperationException("array is empty");
        /// <summary>
        /// 获取数组第一个元素，不存在则返回默认值
        /// </summary>
        /// <returns></returns>
        public T? FirstOrDefault() => array.Length > 0 ? array[0] : default;
        /// <summary>
        /// 获取数组的最后一个元素；无责报错
        /// </summary>
        /// <returns></returns>
        public T Last() => array.Length > 0 ? array[^1] : throw new InvalidOperationException("array is empty");
        /// <summary>
        /// 获取数组的最后一个元素，不存在则返回默认值
        /// </summary>
        /// <returns></returns>
        public T? LastOrDefault() => array.Length > 0 ? array[^1] : default;

        /// <summary>
        /// 从前往后 搜索元素在数组中的第一个位置索引
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>找到则为第一个匹配项的索引；否则-1。</returns>
        public int IndexOf(in T obj) => Array.IndexOf(array, obj);
        /// <summary>
        /// 从后往前 搜索元素在数组中的最后一个位置索引
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>找到则为最后一个匹配项的索引；否则-1。</returns>
        public int LastIndexOf(in T obj) => Array.LastIndexOf(array, obj);
        #endregion

        #region Any、ForEach
        /// <summary>
        /// 数组是否有值
        /// <para>1、不是null、不是空数组 </para>
        /// <para>2、直接使用自身类型属性判断；不用.Any </para>
        /// </summary>
        /// <returns></returns>
        public bool Any() => array.Length != 0;
        /// <summary>
        /// 数据是否存在符合条件数据
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public bool Any(in Predicate<T> predicate)
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
        /// 遍历数据
        /// <para>1、不能终止循环遍历 </para>
        /// </summary>
        /// <param name="each"></param>
        public void ForEach(in Action<T> each)
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
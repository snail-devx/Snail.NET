using Snail.Utilities.Common.Extensions;
using StackExchange.Redis;

namespace Snail.Redis.Extensions
{
    /// <summary>
    /// Redis实体扩展方法
    /// </summary>
    internal static class RedisExtensions
    {
        #region 公共方法
        /// <summary>
        /// 将数据转成Redis值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static RedisValue AsValue<T>(this T data)
        {
            //  对数据始终做序列化，即使是值类型
            if (data != null)
            {
                string tmpValue = data.AsJson();
                return tmpValue.AsBytes();
            }
            return RedisValue.Null;
        }

        /// <summary>
        /// 将redis值转换成具体类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T As<T>(this RedisValue value)
        {
            //  对数据始终做反序列化，即使是值类型
            if (value.HasValue == true && value.IsNull == false)
            {
                byte[] bytes = value!;
                string str = bytes.AsString();
                return str.As<T>();
            }
            return default!;
        }
        /// <summary>
        /// 将redis值转换成具体类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static IList<T> ToList<T>(this RedisValue[] values)
        {
            IList<T> lst = new List<T>();
            for (int index = 0; index < values.Length; index++)
            {
                RedisValue value = values[index];
                if (value.HasValue == true)
                {
                    lst.Add(value.As<T>());
                }
            }
            return lst;
        }

        /// <summary>
        /// 将hash实体转成字典数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entries"></param>
        /// <returns></returns>
        public static IDictionary<string, T> ToDictionary<T>(this HashEntry[] entries)
        {
            IDictionary<string, T> map = new Dictionary<string, T>();
            for (var index = 0; index < entries.Length; index++)
            {
                var item = entries[index];
                if (item.Value.HasValue == true)
                {
                    map[item.Name!] = item.Value.As<T>();
                }
            }
            return map;
        }
        /// <summary>
        /// 将转换成字典数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entries"></param>
        /// <returns></returns>
        public static IDictionary<T, double> ToDictionary<T>(this SortedSetEntry[] entries) where T : notnull
        {
            IDictionary<T, double> map = new Dictionary<T, double>();
            for (int index = 0; index < entries.Length; index++)
            {
                SortedSetEntry set = entries[index];
                if (set.Element.HasValue == true)
                {
                    T value = set.Element.As<T>();
                    map[value] = set.Score;
                }
            }
            return map;
        }
        #endregion
    }
}

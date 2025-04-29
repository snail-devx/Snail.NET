namespace Snail.Utilities.Common.Extensions
{
    /// <summary>
    /// <see cref="Span{T}"/>、<see cref="ReadOnlySpan{T}"/>类型扩展方法
    /// </summary>
    public static class SpanExtensions
    {
        #region Span
        /// <summary>
        /// 是否有符合条件的任意值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span"></param>
        /// <param name="predicate">断言，传null则第一个；否则匹配符合条件的第一个</param>
        /// <returns>存在符合条件数据，返回true；否则false</returns>
        public static bool Any<T>(this Span<T> span, Predicate<T>? predicate = null)
        {
            //  无数据、或者无断言，基于长度判断
            if (span.Length == 0 || predicate == null)
            {
                return span.Length > 0;
            }
            //  根据断言条件，匹配补上返回false
            for (int index = 0; index < span.Length; index++)
            {
                if (predicate.Invoke(span[index]) == true)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 取符合条件的第一个值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static T? FirstOrDefault<T>(this Span<T> span, Predicate<T>? predicate = null)
        {
            //  无断言，取第一个，无数据返回默认值
            if (span.Length == 0) return default;
            if (predicate == null) return span[0];
            //  遍历查数据，无则返回default
            for (int index = 0; index < span.Length; index++)
            {
                if (predicate.Invoke(span[index]) == true)
                {
                    return span[index];
                }
            }
            return default;
        }
        /// <summary>
        /// 取符合条件的最后一个值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static T? LastOrDefault<T>(this Span<T> span, Predicate<T>? predicate = null)
        {
            if (span.Length == 0) return default;
            if (predicate == null) return span[span.Length - 1];
            //  遍历查数据，无则返回default
            for (int index = span.Length - 1; index >= 0; index--)
            {
                if (predicate.Invoke(span[index]) == true)
                {
                    return span[index];
                }
            }
            return default;
        }

        /// <summary>
        /// 遍历；不可中断循环
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span"></param>
        /// <param name="each"></param>
        /// <returns></returns>
        public static Span<T> ForEach<T>(this Span<T> span, Action<T> each)
        {
            ThrowIfNull(each);
            for (int index = 0; index < span.Length; index++)
            {
                each.Invoke(span[index]);
            }
            return span;
        }
        /// <summary>
        /// 遍历；不可中断循环
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span"></param>
        /// <param name="each"></param>
        /// <returns></returns>
        public static Span<T> ForEach<T>(this Span<T> span, Action<int, T> each)
        {
            ThrowIfNull(each);
            for (int index = 0; index < span.Length; index++)
            {
                each.Invoke(index, span[index]);
            }
            return span;
        }
        #endregion

        #region ReadOnlySpan
        /// <summary>
        /// 是否有符合条件的任意值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span"></param>
        /// <param name="predicate">断言，传null则第一个；否则匹配符合条件的第一个</param>
        /// <returns>存在符合条件数据，返回true；否则false</returns>
        public static bool Any<T>(this ReadOnlySpan<T> span, Predicate<T>? predicate = null)
        {
            //  无数据、或者无断言，基于长度判断
            if (span.Length == 0 || predicate == null)
            {
                return span.Length > 0;
            }
            //  根据断言条件，匹配补上返回false
            for (int index = 0; index < span.Length; index++)
            {
                if (predicate.Invoke(span[index]) == true)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 取符合条件的第一个值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static T? FirstOrDefault<T>(this ReadOnlySpan<T> span, Predicate<T>? predicate = null)
        {
            //  无断言，取第一个，无数据返回默认值
            if (span.Length == 0) return default;
            if (predicate == null) return span[0];
            //  遍历查数据，无则返回default
            for (int index = 0; index < span.Length; index++)
            {
                if (predicate.Invoke(span[index]) == true)
                {
                    return span[index];
                }
            }
            return default;
        }
        /// <summary>
        /// 取符合条件的最后一个值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static T? LastOrDefault<T>(this ReadOnlySpan<T> span, Predicate<T>? predicate = null)
        {
            if (span.Length == 0) return default;
            if (predicate == null) return span[span.Length - 1];
            //  遍历查数据，无则返回default
            for (int index = span.Length - 1; index >= 0; index--)
            {
                if (predicate.Invoke(span[index]) == true)
                {
                    return span[index];
                }
            }
            return default;
        }

        /// <summary>
        /// 遍历；不可中断循环
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span"></param>
        /// <param name="each"></param>
        /// <returns></returns>
        public static ReadOnlySpan<T> ForEach<T>(this ReadOnlySpan<T> span, Action<T> each)
        {
            ThrowIfNull(each);
            for (int index = 0; index < span.Length; index++)
            {
                each.Invoke(span[index]);
            }
            return span;
        }
        /// <summary>
        /// 遍历；不可中断循环
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span"></param>
        /// <param name="each"></param>
        /// <returns></returns>
        public static ReadOnlySpan<T> ForEach<T>(this ReadOnlySpan<T> span, Action<int, T> each)
        {
            ThrowIfNull(each);
            for (int index = 0; index < span.Length; index++)
            {
                each.Invoke(index, span[index]);
            }
            return span;
        }
        #endregion
    }
}

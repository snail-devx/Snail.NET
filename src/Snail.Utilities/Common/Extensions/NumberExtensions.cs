namespace Snail.Utilities.Common.Extensions
{
    /// <summary>
    /// 数值相关扩展方法
    /// </summary>
    public static class NumberExtensions
    {
        #region Int32 相关扩展
        extension(int value)
        {
            /// <summary>
            /// 将Int值转换成指定枚举值
            /// <para>1、若转换失败，则报错</para>
            /// </summary>
            /// <typeparam name="TEnum"></typeparam>
            /// <returns></returns>
            public TEnum AsEnum<TEnum>() where TEnum : struct, Enum
            {
                object obj = Enum.ToObject(typeof(TEnum), value);
                return (TEnum)obj;
            }
            /// <summary>
            /// Int值是否是指定类型枚举
            /// </summary>
            /// <typeparam name="TEnum"></typeparam>
            /// <returns></returns>
            public bool IsEnum<TEnum>() where TEnum : struct, Enum
                => Enum.IsDefined(typeof(TEnum), value);
            /// <summary>
            /// Int值是否是指定类型枚举
            /// </summary>
            /// <typeparam name="TEnum"></typeparam>
            /// <param name="enum">out参数；若不是有效枚举，则返回默认值0</param>
            /// <returns></returns>
            public bool IsEnum<TEnum>(out TEnum @enum) where TEnum : struct, Enum
            {
                if (Enum.IsDefined(typeof(TEnum), value) == true)
                {
                    @enum = AsEnum<TEnum>(value);
                    return true;
                }
                else
                {
                    @enum = default;
                    return false;
                }
            }
        }
        #endregion

        #region Double 相关扩展
        extension(double value)
        {
            /// <summary>
            /// 转换成字符串
            /// <para>1、保留小数位数</para>
            /// <para>2、四舍五入,小数位不足时补0</para>
            /// </summary>
            /// <param name="precision"></param>
            /// <returns></returns>
            public string AsString(int precision)
            {
                double result = precision >= 0
                    ? Math.Round(value, precision, MidpointRounding.AwayFromZero)
                    : precision;
                return precision >= 0
                    ? string.Format("{0:F" + precision + "}", result)
                    : result.ToString();
            }
        }
        #endregion
    }
}
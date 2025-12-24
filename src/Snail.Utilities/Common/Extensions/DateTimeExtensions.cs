namespace Snail.Utilities.Common.Extensions;

/// <summary>
/// 日期时间扩展
/// </summary>
/// <remarks>只扩展用到的，不做全量扩展，日期时间格式字符串太多，没有意义</remarks>
public static class DateTimeExtensions
{
    #region 属性变量
    /// <summary>
    /// 格式化：日期时间
    /// </summary>
    public const string FMT_DATETIME = "yyyy-MM-dd HH:mm:ss";
    /// <summary>
    /// 格式化：日期时间ISO格式
    /// </summary>
    /// <remarks>从Newtonsoft.Json程序集IsoDateTimeConverter类中DefaultDateTimeFormat逼常量copy过来的</remarks>
    public const string FMT_DATETIME_ISO = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";
    /// <summary>
    /// 格式化：日期
    /// </summary>
    public const string FMT_DATE = "yyyy-MM-dd";
    /// <summary>
    /// 格式化：时间
    /// </summary>
    public const string FMT_TIME = "HH:mm:ss";
    #endregion

    #region 公共方法

    #region DateTime
    extension(DateTime dt)
    {
        /// <summary>
        /// 转换成日期时间字符串
        /// <para>1、强制格式：<see cref="FMT_DATETIME"/></para>
        /// </summary>
        /// <param name="toUniversalTime">是否先转成UTC时间</param>
        /// <returns></returns>
        public string AsDateTimeString(bool toUniversalTime = false)
            => AsString(dt, FMT_DATETIME, toUniversalTime);
        /// <summary>
        /// 转换成日期字符串
        /// <para>1、强制格式：<see cref="FMT_DATE"/></para>
        /// </summary>
        /// <param name="toUniversalTime">是否先转成UTC时间</param>
        /// <returns></returns>
        public string AsDateString(bool toUniversalTime = false)
            => AsString(dt, FMT_DATE, toUniversalTime);
        /// <summary>
        /// 转换成时间字符串
        /// <para>1、强制格式：<see cref="FMT_TIME"/></para>
        /// </summary>
        /// <param name="toUniversalTime">是否先转成UTC时间</param>
        /// <returns></returns>
        public string AsTimeString(bool toUniversalTime = false)
            => AsString(dt, FMT_TIME, toUniversalTime);

        /// <summary>
        /// 转换成ISO8601日期字符串
        /// <para>1、强制格式：<see cref="FMT_DATETIME_ISO"/></para>
        /// </summary>
        /// <param name="toUniversalTime">是否先转成UTC时间</param>
        /// <returns></returns>
        public string AsIsoDateTimeString(bool toUniversalTime = false)
            => AsString(dt, FMT_DATETIME_ISO, toUniversalTime);
    }
    #endregion

    #region DateTime?
    extension(DateTime? dt)
    {
        /// <summary>
        /// 转换成日期时间字符串
        /// <para>1、强制格式：<see cref="FMT_DATETIME"/></para>
        /// <para>2、若为null则返回String.Empty</para>
        /// </summary>
        /// <param name="toUniversalTime">是否先转成UTC时间</param>
        /// <returns></returns>
        public string AsDateTimeString(bool toUniversalTime = false)
            => dt.HasValue ? AsString(dt.Value, FMT_DATETIME, toUniversalTime) : Empty;
        /// <summary>
        /// 转换成日期字符串
        /// <para>1、强制格式：<see cref="FMT_DATE"/></para>
        /// <para>2、若为null则返回String.Empty</para>
        /// </summary>
        /// <param name="toUniversalTime">是否先转成UTC时间</param>
        /// <returns></returns>
        public string AsDateString(bool toUniversalTime = false)
            => dt.HasValue ? AsString(dt.Value, FMT_DATE, toUniversalTime) : Empty;
        /// <summary>
        /// 转换成时间字符串
        /// <para>1、强制格式：<see cref="FMT_TIME"/></para>
        /// <para>2、若为null则返回String.Empty</para>
        /// </summary>
        /// <param name="toUniversalTime">是否先转成UTC时间</param>
        /// <returns></returns>
        public string AsTimeString(bool toUniversalTime = false)
            => dt.HasValue ? AsString(dt.Value, FMT_TIME, toUniversalTime) : Empty;

        /// <summary>
        /// 转换成ISO8601日期字符串
        /// <para>1、强制格式：<see cref="FMT_DATETIME_ISO"/></para>
        /// <para>2、若为null则返回String.Empty</para>
        /// </summary>
        /// <param name="toUniversalTime">是否先转成UTC时间</param>
        /// <returns></returns>
        public string AsIsoDateTimeString(bool toUniversalTime = false)
            => dt.HasValue ? AsString(dt.Value, FMT_DATETIME_ISO, toUniversalTime) : Empty;
    }
    #endregion

    #endregion

    #region 私有方法
    /// <summary>
    /// 将日期转成字符串；内部专用
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="dtfmt">日期转字符串的格式；不能为空</param>
    /// <param name="toUniversalTime">是否先将<paramref name="dt"/>转成UTC时间</param>
    /// <returns></returns>
    private static string AsString(DateTime dt, string dtfmt, bool toUniversalTime)
        => (toUniversalTime ? dt.ToUniversalTime() : dt).ToString(dtfmt);
    #endregion
}

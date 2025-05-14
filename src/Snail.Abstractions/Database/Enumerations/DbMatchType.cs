namespace Snail.Abstractions.Database.Enumerations
{
    /// <summary>
    /// 匹配类型枚举
    /// </summary>
    public enum DbMatchType
    {
        /// <summary>
        /// 全匹配：满足所有条件
        /// </summary>
        AndAll = 0,

        /// <summary>
        /// 满足任意条件即可
        /// </summary>
        OrAny = 10,
    }
}

namespace Snail.SqlCore.Enumerations;

/// <summary>
/// Sql的Select的用途类型
/// </summary>
public enum SelectUsageType
{
    /// <summary>
    /// 查询数据：select 字段名1,字段名2
    /// </summary>
    Data = 0,

    /// <summary>
    /// 进行Any数据有值判断：select id字段名
    /// </summary>
    Any = 1,

    /// <summary>
    /// 计算数量使用：select count(1)
    /// </summary>
    Count = 10,
}

namespace Snail.Abstractions.Database.Enumerations
{
    /// <summary>
    /// 数据库类型枚举
    /// </summary>
    /// <remarks>和系统的<see cref="System.Data.DbType"/>有冲突，区别一下</remarks>
    public enum DbType
    {
        /// <summary>
        /// MySql数据库
        /// </summary>
        MySql = 100,
        /// <summary>
        /// 微软SQLServer数据库
        /// </summary>
        SqlServer = 101,
        /// <summary>
        /// 甲骨文Oracle数据库
        /// </summary>
        Oracle = 102,
        /// <summary>
        /// Postgres数据库
        /// </summary>
        Postgres = 103,

        /// <summary>
        /// mongo数据库：nosql代表
        /// </summary>
        MongoDB = 200,
        /// <summary>
        /// ElasticSearch数据库
        /// </summary>
        ElasticSearch = 201,
    }
}

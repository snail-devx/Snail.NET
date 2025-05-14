namespace Snail.Abstractions.Database.DataModels
{
    /// <summary>
    /// 数据库实体映射到数据的字段信息
    /// </summary>
    public sealed class DbModelField
    {
        /// <summary>
        /// 数据库字段名
        /// </summary>
        public required string Name { init; get; }
        /// <summary>
        /// 字段对应数据库实体中的属性名
        /// </summary>
        public required PropertyInfo Property { init; get; }
        /// <summary>
        /// 数据库字段类型
        /// </summary>
        public required Type Type { init; get; }

        /// <summary>
        /// 是否是主键字段
        /// </summary>
        public required bool PK { init; get; }
    }
}

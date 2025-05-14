namespace Snail.Abstractions.Database.Attributes
{
    /// <summary>
    /// 特性标签：数据库表字段属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class DbFieldAttribute : Attribute
    {
        /// <summary>
        /// 数据库字段名；为空使用属性名
        /// </summary>
        public string? Name { get; init; }
        /// <summary>
        /// 字段数据类型；为空使用属性类型 <br />
        ///     此值在不同类型数据库下支持情况不一样，慎用
        /// </summary>
        public Type? Type { get; init; }
        /// <summary>
        /// 数据库映射时，是否忽略此字段
        /// </summary>
        public bool Ignored { get; init; }

        /// <summary>
        /// 是否是主键字段
        /// </summary>
        public bool PK { get; init; }

        #region 暂不对外开放；后续有需求再说
        ///// <summary>
        ///// 是否是业务主键字段
        ///// </summary>
        //public bool BK { get; init; }
        ///// <summary>
        ///// 是否为外键字段
        ///// </summary>
        //public bool FK { get; init; }
        ///// <summary>
        ///// 是否是子表对象
        ///// </summary>
        //public bool Children { get; init; }
        #endregion
    }
}

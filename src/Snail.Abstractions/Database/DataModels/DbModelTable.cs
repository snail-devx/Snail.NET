using Snail.Abstractions.Database.Interfaces;

namespace Snail.Abstractions.Database.DataModels
{
    /// <summary>
    /// 数据库实体映射到数据库的表信息 <br />
    ///     1、用于存储数据库实体的表信息、字段信息等
    /// </summary>
    public sealed class DbModelTable
    {
        #region 属性变量
        /// <summary>
        /// 数据库实体类型
        /// </summary>
        public required Type Type { init; get; }

        /// <summary>
        /// 数据表名称
        /// </summary>
        public required string Name { init; get; }
        /// <summary>
        /// 是否启用数据路由分片存储 <br />
        ///     1、为true时，实体必须实现<see cref="IDbRouting.GetRouting"/>接口方法，且值不能为空 <br />
        ///     2、具体能否实现分片存储，还得看数据库和具体<see cref="IDbModelProvider{DbModel}"/>实现类是否是否支持 <br />
        /// </summary>
        public bool Routing { init; get; }

        /// <summary>
        /// 主键字段；从<see cref="Fields"/>中读取出来，快捷使用
        /// </summary>
        public required DbModelField PKField { init; get; }
        /// <summary>
        /// 数据库字段信息
        /// </summary>
        public required IReadOnlyCollection<DbModelField> Fields { init; get; }
        #endregion
    }
}

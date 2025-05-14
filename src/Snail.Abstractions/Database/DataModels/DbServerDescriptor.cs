using Snail.Abstractions.Database.Interfaces;

namespace Snail.Abstractions.Database.DataModels
{
    /// <summary>
    /// 数据库服务器描述器
    /// </summary>
    public sealed class DbServerDescriptor : DbServerOptions, IDbServerOptions
    {
        /** 从父级<see cref="DbServerOptions"/>继承属性
         *      <see cref="DbServerOptions.Workspace"/>                     服务器地址所属的工作空间
         *      <see cref="DbServerOptions.DbType"/>                        数据库类型
         *      <see cref="DbServerOptions.DbCode"/>                        数据库编码
         */

        /// <summary>
        /// 数据库名称；真实物理数据库名称，注意某些数据库大小写敏感
        /// </summary>
        public required string DbName { init; get; }

        /// <summary>
        /// 数据库连接串
        /// </summary>
        public required string Connection { init; get; }
    }
}

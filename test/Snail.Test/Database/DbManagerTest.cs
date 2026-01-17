using Snail.Abstractions.Database;
using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Database.Enumerations;
using Snail.Abstractions.Database.Extensions;

namespace Snail.Test.Database
{
    /// <summary>
    /// 数据库管理器测试
    /// </summary>
    public sealed class DbManagerTest : UnitTestApp
    {
        #region 属性变量
        #endregion

        #region 公共方法
        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void TestManager()
        {
            IDbManager manager = App.ResolveRequired<IDbManager>();
            //  注册测试
            DbServerDescriptor? server = manager.GetServer(dbCode: "Test", dbType: DbType.ElasticSearch);
            Assert.That(server == null, "Test数据库不存在");
            manager.RegisterServer(new DbServerDescriptor()
            {
                DbCode = "Test",
                Workspace = null,
                DbName = "Test1",
                DbType = DbType.ElasticSearch,
                Connection = "ddddd"
            });
            server = manager.GetServer(dbCode: "Test", dbType: DbType.ElasticSearch);
            Assert.That(server != null && server.DbName == "Test1", "Test数据库存在");
            //  读取配置文件
            server = manager.GetServer(workspace: "Test", dbCode: "Test", DbType.MySql);
            Assert.That(server != null && server.DbName == "Test", "Test工作空间下，MySql数据库存在");
            server = manager.GetServer(workspace: "Test", dbCode: "Test", DbType.MongoDB);
            Assert.That(server != null && server.DbName == "Test", "Test工作空间下，MongoDB数据库存在");
            server = manager.GetServer(workspace: "Test", dbCode: "Test", DbType.ElasticSearch);
            Assert.That(server != null && server.DbName == "Test", "Test工作空间下，ElasticSearch数据库存在");
        }
        #endregion

        #region 私有方法
        #endregion
    }
}

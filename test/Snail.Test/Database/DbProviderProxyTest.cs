using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.Enumerations;
using Snail.Abstractions.Database.Extensions;
using Snail.Abstractions.Database.Interfaces;
using Snail.Abstractions.Distribution;
using Snail.Abstractions.Distribution.Attributes;
using Snail.Abstractions.Identity;
using Snail.Database.Components;
using Snail.Test.Database.DataModels;
using System.Linq.Expressions;

namespace Snail.Test.Database;
/// <summary>
/// 
/// </summary>
public sealed class DbProviderProxyTest : UnitTestApp
{
    #region 公共方法
    [Test]
    public async Task TestProxy()
    {
        DbProviderProxyFacctory proxy = App.ResolveRequired<DbProviderProxyFacctory>();
        //  测试缓存代理
        await TestCacheProxy("MySql", new DbProviderCacheProxy(App, proxy.MySql, proxy.Cacher));
        await TestCacheProxy("Postgres", new DbProviderCacheProxy(App, proxy.Postgres, proxy.Cacher));
        await TestCacheProxy("MongoDB", new DbProviderCacheProxy(App, proxy.MongoDB, proxy.Cacher));
        await TestCacheProxy("ElasticSearch", new DbProviderCacheProxy(App, proxy.ElasticSearch, proxy.Cacher));
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 构建实体集合做测试
    /// </summary>
    /// <returns></returns>
    private IEnumerable<TestDbModel> BuildModels2Test()
    {
        //IDbModelProvider xx = null;
        //List<TestDbModel> models; string id = null;
        //xx.Delete<TestDbModel>(id);

        string id = App.ResolveRequired<IIdGenerator>().NewId("TestModelProvider");
        for (var index = 0; index < 20; index++)
        {
            //int 0值，强转成char时，Postgres数据库报错：Npgsql.PostgresException : 22021: 无效的 "UTF8" 编码字节顺序: 0x00。MySql数据库的值为空字符。强转存的值也不是对应数值。所以0时此处先赋值为0
            char? cn = index % 11 == 0 ? null : index % 3 == 0 ? '0' : (char)(index % 3);
            yield return new TestDbModel()
            {
                IdValue = $"{id}_{index.ToString().PadLeft(2)}",
                String = index % 3 == 0 ? null : index.ToString(),
                Int = index,
                IntNull = index % 4 == 0 ? null : index % 4,
                Bool = index % 6 == 0,
                BoolNull = index % 7 == 0 ? null : index % 3 == 0,
                Char = (char)(60 + index % 10),
                CharNull = cn,//index % 11 == 0 ? null : (char)(index % 3),
                DateTime = DateTime.Now,
                DateTimeNull = index % 12 == 0 ? null : DateTime.Now,
                NodeType = (ExpressionType)(index % 9),
                NodeTypeNull = index % 9 == 0 ? null : (ExpressionType)(index % 11),

                ParentName = Guid.NewGuid().ToString(),
                ParentIgnore = "这个字段是忽略的，数据库中不能有此字段",
                Override = "Override" + 100 % 6
            };
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private async Task TestCacheProxy(string title, IDbProviderProxy proxy)
    {
        /* 先验证不报错，后期直接从缓存服务器判断存在性和正确性 */

        string tmpId = Guid.NewGuid().ToString();
        //  先取缓存数据
        TestDbModel? model = await proxy.Load<TestDbModel>(tmpId);
        Assert.That(model == null);
        //  插入数据
        IList<TestDbModel> models = BuildModels2Test().ToList();
        await proxy.Insert(models);
        await proxy.Load<TestDbModel, string>(models.Select(m => m.IdValue!).ToList());
        //  更新数据
        foreach (var m in models)
        {
            m.String = Guid.NewGuid().ToString();
        }
        await proxy.Save(models);
        await proxy.Update<TestDbModel, string>
        (
            models.Select(m => m.IdValue!).ToList(),
            new Dictionary<string, object?>() { { "String", Guid.NewGuid().ToString() } }
        );
        //  删除数据
        await proxy.Delete<TestDbModel, string>(models.Select(m => m.IdValue!).ToList());
    }
    #endregion

    #region 私有类型
    /// <summary>
    /// 提供程序工厂类；用于进行di注入动态构建
    /// </summary>
    [Component]
    private class DbProviderProxyFacctory
    {
#pragma warning disable CS0649
        [Cacher, Server(Workspace = "Test", Code = "Default")]
        public required ICacher Cacher { init; get; }

        /// <summary>
        /// 默认提供程序
        /// </summary>
        [DbProvider(Workspace = "Test", DbCode = "Test")]
        public required IDbProvider Default { init; get; }

        /// <summary>
        /// MySql提供程序
        /// </summary>
        [DbProvider(DbType.MySql, Workspace = "Test", DbCode = "Test")]
        public required IDbProvider MySql { init; get; }
        /// <summary>
        /// MySql提供程序
        /// </summary>
        [DbProvider(DbType.Postgres, Workspace = "Test", DbCode = "Test")]
        public required IDbProvider Postgres { init; get; }

        /// <summary>
        /// MongoDB提供程序
        /// </summary>
        [DbProvider(DbType.MongoDB, Workspace = "Test", DbCode = "Test")]
        public required IDbProvider MongoDB { init; get; }

        /// <summary>
        /// ElasticSearch提供程序
        /// </summary>
        [DbProvider(DbType.ElasticSearch, Workspace = "Test", DbCode = "Test")]
        public required IDbProvider ElasticSearch { init; get; }
#pragma warning restore CS0649
    }
    #endregion
}
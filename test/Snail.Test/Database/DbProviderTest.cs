using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.Enumerations;
using Snail.Abstractions.Database.Extensions;
using Snail.Abstractions.Database.Interfaces;
using Snail.Abstractions.Identity;
using Snail.Elastic;
using Snail.Mongo;
using Snail.MySql;
using Snail.PostgreSql;
using Snail.Test.Database.Components;
using Snail.Test.Database.DataModels;
using Snail.Test.Database.Interfaces;
using Snail.Utilities.Collections.Extensions;
using System.Linq.Expressions;

namespace Snail.Test.Database
{
    /// <summary>
    /// 数据库提供程序测试
    /// </summary>
    public sealed class DbProviderTest : UnitTestApp
    {
        #region 公共方法

        #region IModelProvider测试
        /// <summary>
        /// 测试提供程序
        /// </summary>
        [Test]
        public async Task TestProvider()
        {
            var proxy = App.ResolveRequired<DbProviderFactory>();
            Assert.That(proxy.Default != null && (proxy.Default is MySqlProvider), "proxy.Default不能为null");
            Assert.That(proxy.MySql != null && proxy.MySql is MySqlProvider, "proxy.MySql不能为null");
            Assert.That(proxy.MongoDB != null && proxy.MongoDB is MongoProvider, "proxy.MongoDB不能为null");
            Assert.That(proxy.ElasticSearch != null && proxy.ElasticSearch is ElasticProvider, "proxy.ElasticSearch不能为null");
            Assert.That(proxy.Postgres != null && proxy.Postgres is PostgresProvider, "proxy.ElasticSearch不能为null");
            //  测试基于id的insert、save、update、delete、load等
            Task[] tasks = [
                TestModelProvider(proxy.Default!, "proxy.Default"),
                TestModelProvider(proxy.MySql!, "proxy.MySql"),
                TestModelProvider(proxy.MongoDB!, "proxy.MongoDB"),
                TestModelProvider(proxy.ElasticSearch!, "proxy.ElasticSearch"),
                TestModelProvider(proxy.Postgres!, "proxy.Postgres"),
            ];
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 测试自定义数据库提供程序
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestCustomProvider()
        {
            var proxy = App.ResolveRequired<DbCustomProviderFactory>();
            Assert.That(proxy.Default != null && proxy.Default is MySqlCustomProvider, "proxy.Default不能为null");
            Assert.That(proxy.MySql != null && proxy.MySql is MySqlCustomProvider, "proxy.MySql不能为null");
            Assert.That(proxy.Postgres != null && proxy.Postgres is PostgresCustomProvider, "proxy.Postgres不能为null");
            Assert.That(proxy.MongoDB != null && proxy.MongoDB is MongoCustomProvider, "proxy.MongoDB不能为null");
            Assert.That(proxy.ElasticSearch != null && proxy.ElasticSearch is ElasticCustomProvider, "proxy.ElasticSearch不能为null");
            //  测试基于id的insert、save、update、delete、load等
            Task[] tasks = [
                TestModelProvider(proxy.Default!, "proxy.Default"),
                TestModelProvider(proxy.MySql!, "proxy.MySql"),
                TestModelProvider(proxy.MongoDB!, "proxy.MongoDB"),
                TestModelProvider(proxy.ElasticSearch!, "proxy.ElasticSearch"),
                TestModelProvider(proxy.Postgres!, "proxy.Postgres"),
            ];
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 路由测试
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestRouting()
        {
            var proxy = App.ResolveRequired<DbProviderFactory>();
            await TestModelRouting("MySql", proxy.MySql, false);
            await TestModelRouting("MongoDB", proxy.MongoDB, false);
            await TestModelRouting("ElasticSearch", proxy.ElasticSearch, true);
            await TestModelRouting("Postgres", proxy.Postgres, false);
        }
        #endregion

        #region XXXAble相关测试
        /// <summary>
        /// 测试QueryAble、Updatable、Deletable
        /// </summary>
        [Test]
        public async Task TestAble()
        {
            var proxy = App.ResolveRequired<DbProviderFactory>();

            //  调试lambda表达式
            //await proxy.MongoDB.AsQueryable(routing: null)
            //      .Where(model => model.IdValue == null && (model.Int > 0 || model.BoolNull == null))
            //      .ToListAsync();

            //  此时不用测试Default了；每个数据库单独测试即可
            IDictionary<string, IDbProvider> providers = new Dictionary<string, IDbProvider>()
                .Set("MySql", proxy.MySql)
                .Set("Mongo", proxy.MongoDB)
                .Set("ElasticSearch", proxy.ElasticSearch)
                .Set("Postgres", proxy.Postgres)
                ;
            await TestModelProviderAble(providers);
        }
        #endregion

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
            for (var index = 0; index < 100; index++)
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
        /// 测试数据库实体提供程序能力，基于主键Id的增删改查
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="title"></param>
        private async Task TestModelProvider(IDbProvider provider, string title)
        {
            var next = new Random(DateTime.Now.Second);
            string guidString = Guid.NewGuid().ToString();
            //  空数据操作测试：查询，删除、更新；单个批量、同步异步
            TestDbModel? dM = await provider.Load<TestDbModel, string>("819122251341631488_ 0");
            Assert.That(await provider.Load<TestDbModel>(guidString) == null, $"{title}单个load空数据");
            Assert.That(0 == (await provider.Load<TestDbModel>([guidString])).Count, $"{title}批量load空数据");
            //      测试一个主键id转换值失败的情况
            try { await provider.Load<TestDbModel>(new List<string>() { }); }
            catch (ArgumentNullException) { }
            //  批量插入、保存、查询、更新、删除
            {
                IList<TestDbModel> models = BuildModels2Test().ToList();
                List<string> ids = models.Select(model => model.IdValue!).ToList();
                //  insert
                bool bValue = await provider.Insert(models);
                Assert.That(bValue == true, $"{title}批量异步插入数据");
                Assert.That(ids.Count == (await provider.Load<TestDbModel>(ids)).Count, $"{title}批量load数据");
                //  save
                for (int index = 0; index < models.Count; index++)
                {
                    models[index].String = "测试Save操作" + index;
                    models[index].Int = index;
                    models[index].BoolNull = index % 3 == 0 ? null : true;
                }
                await provider.Save(models);
                models = await provider.Load<TestDbModel>(ids);
                Assert.That(ids.Count == models.Count, $"{title}批量save后数据");
                foreach (var model in models)
                {
                    Assert.That(model.String == $"测试Save操作{model.Int}", $"{title}测试save后的String属性值");
                    Assert.That(model.BoolNull == (model.Int % 3 == 0 ? null : true), $"{title}测试save后的Int属性值");
                }
                //  update
                var updates = new Dictionary<string, object?>()
                {
                    { "String",guidString },
                    { "Int",next.Next(1,1000)}
                };
                Assert.That(ids.Count == await provider.Update<TestDbModel>(ids, updates), $"{title}测试update数据");
                models = await provider.Load<TestDbModel>(ids);
                Assert.That(ids.Count == models.Count, $"{title}测试update后加载数据");
                Assert.That(1 == models.Select(item => item.String).Distinct().Count(), $"{title}测试update数据后的String值");

                //  delete
                await provider.Delete<TestDbModel>(ids);
                Assert.That(0 == (await provider.Load<TestDbModel>(ids)).Count, $"{title}批量load数据");
            }
            //  单个插入、保存、查询、更新、删除：作为批量的扩展方法
            {
                IList<TestDbModel> models = BuildModels2Test().ToList();
                IList<string> ids = models.Select(model => model.IdValue!).ToList();
                //  单个insert+save
                foreach (var model in models)
                {
                    Assert.That(true == await provider.Insert(model), $"{title}测试insert单个插入");
                    Assert.That(model.IdValue == (await provider.Load<TestDbModel>(model.IdValue!))?.IdValue, $"{title}测试单个Load");
                }
                for (int index = 0; index < models.Count; index++)
                {
                    models[index].String = "测试Save操作" + index;
                    models[index].Int = index;
                    models[index].BoolNull = index % 3 == 0 ? null : true;
                    Assert.That(true == await provider.Save(models[index]), $"{title}测试单个Save");
                }
                //  save验证+单个update
                var updates = new Dictionary<string, object?>()
                {
                    { "String",guidString },
                    { "Int",next.Next(1,1000)}
                };
                foreach (string id in ids)
                {
                    TestDbModel? model = await provider.Load<TestDbModel>(id);
                    Assert.That(id == model?.IdValue, $"{title}验证单个Save");
                    Assert.That(model!.String == $"测试Save操作{model.Int}", $"{title}测试单个save后的String属性值");
                    Assert.That(model.BoolNull == (model.Int % 3 == 0 ? null : true), $"{title}测试单个save后的Int属性值");

                    Assert.That(1 == await provider.Update<TestDbModel>(id, updates), $"{title}测试单个update");
                }
                //  update验证+单个删除
                foreach (string id in ids)
                {
                    TestDbModel? model = await provider.Load<TestDbModel>(id);
                    Assert.That(model?.String == guidString, $"{title}验证单个update数据");
                    Assert.That(1 == await provider.Delete<TestDbModel>(id!), $"{title}测试单个Delete");
                    Assert.That(null == await provider.Load<TestDbModel>(id), $"{title}验证单个delete");
                }
            }
        }

        /// <summary>
        /// 测试数据库提供程序的XXXAble相关接口
        /// </summary>
        /// <param name="providers"></param>
        /// <returns></returns>
        private async Task TestModelProviderAble(IDictionary<string, IDbProvider> providers)
        {
            //  插入100数据，先清空了
            TestDbModel[] models = BuildModels2Test().ToArray();
            foreach (var (key, provider) in providers)
            {
                await provider.AsDeletable<TestDbModel>().Where(item => item.IdValue != null).Delete();
                bool bValue = await provider.Insert(models);
                Assert.That(bValue, $"{key}：测试前插入100条数据");
            }
            //  执行queryable查询
            foreach (var descriptor in new DbFilterExpressProxy().BuildFilterExpress())
            {
                IDictionary<string, IList<TestDbModel>> tmpDict = new Dictionary<string, IList<TestDbModel>>();
                //  内存中查询：若需要比较，则放入字典中
                try { tmpDict["内存"] = models.Where(descriptor.Lambda.Compile()).ToList(); }
                catch (Exception ex)
                {
                    //  文本搜索比较时，item.String.Contains("_") item.String为null时会报错，这里做一下排除
                    //  intArray[item.Int] 可能出现，实际上也不应该支持
                    bool ignoreEx = descriptor.IsSupport != true
                         //  Contains(item.String)为null
                         || (descriptor.IsTextFilter == true && ex is ArgumentNullException)
                         //  item.String.Contains("")  String属性值为null
                         || (descriptor.IsTextFilter == true && ex is NullReferenceException)
                         //  item.String[index]  数组索引越界
                         || (descriptor.IsSupport != true && ex is IndexOutOfRangeException);
                    Assert.That(ignoreEx, $"内存查询报错。表达式：{descriptor.Lambda}；异常信息：{ex}");
                }
                int[] x = [1, 2];
                x.Contains(2);
                //  数据库中查询
                foreach (var (key, provider) in providers)
                {
                    Exception? tmpEx = null;
                    try
                    {
                        tmpDict[key] = await provider.AsQueryable<TestDbModel>()
                            .Where(descriptor.Lambda)
                            .Take(models.Length + 100)
                            .ToList();
                    }
                    catch (AggregateException ax)
                    {
                        tmpEx = ax.InnerExceptions.FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        tmpEx = ex;
                    }
                    //  进行异常处理；特殊异常支持
                    if (tmpEx != null)
                    {
                        //  不支持的情况下，keynotfound在忽略字段时会报出此异常
                        bool ignoreEx = (descriptor.IsSupport != true && tmpEx is NotSupportedException)
                             || (descriptor.IsSupport != true && tmpEx is KeyNotFoundException);
                        Assert.That(ignoreEx == true, $"{key}查询报错。表达式：{descriptor.Lambda}；异常信息：{tmpEx}");
                    }
                }
                //  对结果数据做比较，确保都是相同的：内存和各个数据库比较
                IList<TestDbModel>? flagModels = null;
                string? flagKey = null;
                foreach (var (key, value) in tmpDict)
                {
                    //  找参照实体
                    if (flagModels == null)
                    {
                        flagKey = key;
                        flagModels = value ?? new List<TestDbModel>();
                        break;
                    }
                    var rets = value ?? new List<TestDbModel>();
                    //  进行比较：先比较数量
                    Assert.That(flagModels.Count == rets.Count, $"查询结果数量不一致；{flagKey}:{flagModels.Count}；{key}:{rets.Count}");
                    //      数量一致了，比较id结果值
                    List<string> notIds = flagModels
                        .Select(item => item.IdValue!)
                        .Except(rets.Select(item => item.IdValue!))
                        .ToList();
                    Assert.That(notIds.Any() == false, $"查询结果Id不一致；{key}中不存在{flagKey}中对应Id数据{notIds}");
                }
            }
            //  更新测试、删除测试；后期再完善一下，在更新、删除的时候，做一些干扰项
            foreach (var (key, provider) in providers)
            {
                string tmpString = Guid.NewGuid().ToString();
                IDictionary<string, object?> updates = new Dictionary<string, object?>();
                updates.Set("String", tmpString);
                long count = await provider.AsUpdatable<TestDbModel>()
                    .Where(item => item.IdValue != null)
                    .Set(updates)
                    .Update();
                IList<TestDbModel> tmpModels = await
                    (
                        from p in provider.AsQueryable<TestDbModel>()
                        where p.String == tmpString
                        select p
                    )
                    .Take(models.Length + 100)
                    .ToList();
                count = models.Select(item => item.IdValue).Except(tmpModels.Select(item => item.IdValue)).Count();
                Assert.That(count == 0, $"{key}更新数据失败，更新完后，查询数据和models不一致");
                //  这里测一下select功能
                tmpModels = await provider.AsQueryable<TestDbModel>()
                    .Select(item => item.IdValue)
                    .Where(item => item.String == tmpString)
                    .Take(models.Length + 100)
                    .ToList();
                Assert.That(tmpModels.Any(item => item.String != null) == false, $"{key}测试select失败，String中返回了为null的数据");
                //  测试异步更新
                tmpString = Guid.NewGuid().ToString();
                await provider.AsUpdatable<TestDbModel>().Set(item => item.String, tmpString).Where(item => item.IdValue != null).Update();
                tmpModels = await provider.AsQueryable<TestDbModel>()
                    .Where(item => item.IdValue != null)
                    .Take(models.Length + 100)
                    .ToList();
                count = models.Select(item => item.IdValue).Except(tmpModels.Select(item => item.IdValue)).Count();
                Assert.That(count == 0, $"{key}更新数据失败，更新完后，查询数据和models不一致");
                //  测试删除
                count = await provider.AsDeletable<TestDbModel>()
                    .Where(item => item.IntNull != null)
                    .Delete();
                count += await provider.AsDeletable<TestDbModel>().Where(item => item.IntNull == null).Delete();
                Assert.That(count == models.Length, $"{key}删除数据失败，删除数据条数不为{models.Length}");
                //      再查一下
                bool bValue = await provider.AsQueryable<TestDbModel>().Any(item => item.IdValue != null);
                Assert.That(bValue == false, $"{key}删除数据失败，还能查询到数据");
            }
        }

        /// <summary>
        /// 测试实体的路由信息
        /// </summary>
        /// <param name="title"></param>
        /// <param name="provider"></param>
        /// <param name="supportRoutting">是否支持路由</param>
        /// <returns></returns>
        private async Task TestModelRouting(string title, IDbProvider provider, bool supportRoutting)
        {
            //  构建数据
            TestRoutingModel[] models = new TestRoutingModel[100];
            string id = App.ResolveRequired<IIdGenerator>().NewId("TestModelProvider");
            for (int index = 0; index < models.Length; index++)
            {
                models[index] = new TestRoutingModel()
                {
                    Id = $"{id}_{index.ToString().PadLeft(2)}",
                    Routing = $"routing_{index % 4}",
                    Name = index % 3 == 0 ? null : $"{index % 3}"
                };
            }
            await provider.Insert(models);
            //      测试一下routing为null的情况，会强制报错
            if (supportRoutting == true)
            {
                try
                {

                    await provider.Insert(new TestRoutingModel()
                    {
                        Id = id,
                        Routing = (null)!
                    });
                }
                catch (ArgumentException ex)
                {
                    if (ex.Message.Contains("已启用Routing；但实例GetRouting()值为空") == false)
                    {
                        throw;
                    }
                }
            }
            //  基于路由取数据：若是不支持路由的，则和内存中数据做比对
            int random = new Random().Next(0, 4);
            string routing = $"routing_{random}";
            Expression<Func<TestRoutingModel, Boolean>> filter = item =>
                item.Name == (random == 0 ? null : random.ToString()) && item.Id!.StartsWith(id);
            //      query
            IList<TestRoutingModel> dbModels = await provider.AsQueryable<TestRoutingModel>(routing)
                .Where(filter)
                .Take(models.Length * 2)
                .ToList();
            List<TestRoutingModel> mModels = supportRoutting
                ? models.Where(filter.Compile()).Where(item => item.Routing == routing).ToList()
                : models.Where(filter.Compile()).ToList();
            //  干完之后，把数据干掉
            await provider.AsDeletable<TestRoutingModel>()
                .Where(item => models.Select(item => item.Id).ToList().Contains(item.Id))
                .Delete();
        }
        #endregion

        #region 私有类型
        /// <summary>
        /// 提供程序工厂类；用于进行di注入动态构建
        /// </summary>
        [Component]
        private class DbProviderFactory
        {
#pragma warning disable CS0649
            /// <summary>
            /// 默认提供程序
            /// </summary>
            [DbProvider(Workspace = "Test", DbCode = "Test")]
            public required IDbProvider Default;

            /// <summary>
            /// MySql提供程序
            /// </summary>
            [DbProvider(DbType.MySql, Workspace = "Test", DbCode = "Test")]
            public required IDbProvider MySql;
            /// <summary>
            /// MySql提供程序
            /// </summary>
            [DbProvider(DbType.Postgres, Workspace = "Test", DbCode = "Test")]
            public required IDbProvider Postgres;

            /// <summary>
            /// MongoDB提供程序
            /// </summary>
            [DbProvider(DbType.MongoDB, Workspace = "Test", DbCode = "Test")]
            public required IDbProvider MongoDB;

            /// <summary>
            /// ElasticSearch提供程序
            /// </summary>
            [DbProvider(DbType.ElasticSearch, Workspace = "Test", DbCode = "Test")]
            public required IDbProvider ElasticSearch;
#pragma warning restore CS0649
        }

        /// <summary>
        /// 提供程序代理器；用于进行di注入动态构建
        /// </summary>
        [Component]
        private class DbCustomProviderFactory
        {
#pragma warning disable CS0649
            /// <summary>
            /// 默认提供程序
            /// </summary>
            [DbProvider(Workspace = "Test", DbCode = "Test")]
            public required ICustomProvider Default;

            /// <summary>
            /// MySql提供程序
            /// </summary>
            [DbProvider(DbType.MySql, Workspace = "Test", DbCode = "Test")]
            public required ICustomProvider MySql;
            /// <summary>
            /// Postgres提供程序
            /// </summary>
            [DbProvider(DbType.Postgres, Workspace = "Test", DbCode = "Test")]
            public required ICustomProvider Postgres;


            /// <summary>
            /// MongoDB提供程序
            /// </summary>
            [DbProvider(DbType.MongoDB, Workspace = "Test", DbCode = "Test")]
            public required ICustomProvider MongoDB;

            /// <summary>
            /// ElasticSearch提供程序
            /// </summary>
            [DbProvider(DbType.ElasticSearch, Workspace = "Test", DbCode = "Test")]
            public required ICustomProvider ElasticSearch;
#pragma warning restore CS0649
        }
        #endregion
    }
}

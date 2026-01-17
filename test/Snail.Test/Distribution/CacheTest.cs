using Snail.Abstractions.Distribution;
using Snail.Abstractions.Distribution.Attributes;
using Snail.Abstractions.Distribution.Extensions;
using Snail.Abstractions.Web.Extensions;
using Snail.Redis;
using Snail.Utilities.Collections;
using Snail.Utilities.Collections.Extensions;
using StackExchange.Redis;
using System.Diagnostics;

namespace Snail.Test.Distribution
{
    /// <summary>
    /// 缓存测试
    /// </summary>
    public sealed class CacheTest : UnitTestApp
    {
        #region 属性变量
        #endregion

        #region 构造方法
        /// <summary>
        /// 默认无参构造方法
        /// </summary>
        public CacheTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        #endregion

        #region 公共方法

        /// <summary>
        /// 测试批量和单个的性能差距；用于决策Provider中用哪个
        /// </summary>
        [Test]
        public void TestStringSetPerformance()
        {
            //throw new NotImplementedException();
            byte[] buffer = Guid.NewGuid().ToByteArray();
            _ = BitConverter.ToInt64(buffer, 0).ToString()[5..];
            IDatabase db;
            {
                RedisManager manager = App.ResolveRequired<RedisManager>();
                string server = manager.GetServer(workspace: "Test", code: "Default")!.Server;
                ConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(server);
                db = multiplexer.GetDatabase(0);
            }
            List<string> strs = new();
            //  循环单个
            Stopwatch sw = Stopwatch.StartNew();
            for (var index = 0; index < 1000; index++)
            {
                db.StringSet("S:Loop:" + index, index);
            }
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds);
            strs.Add($"单个循环：{sw.ElapsedMilliseconds}");
            //  单个构成批量
            sw.Restart();
            KeyValuePair<RedisKey, RedisValue>[] keyValuePairs = new KeyValuePair<RedisKey, RedisValue>[1000];
            for (var index = 0; (index < 1000); index++)
            {
                keyValuePairs[index] = new KeyValuePair<RedisKey, RedisValue>("BS:Loop:" + index, index);
            }
            db.StringSet(keyValuePairs);
            sw.Stop();
            strs.Add($"单个批量循环：{sw.ElapsedMilliseconds}");
            //  批量循环：得记录批量的任务数，等待批量执行完成；否则会出现部分加不进去的情况
            sw.Restart();
            IBatch batch = db.CreateBatch();
            List<Task<bool>> tasks = new();
            for (var index = 0; index < 1000; index++)
            {
                string tmpKey = "batch1:Loop:" + index;
                tasks.Add(batch.StringSetAsync(tmpKey, index));
            }
            batch.Execute();

            sw.Stop();
            strs.Add($"批量循环：{sw.ElapsedMilliseconds}");
            TestContext.Out.WriteLine(string.Join("\r\n", strs));
        }

        /// <summary>
        /// 缓存测试；测试存在和不存在等情况
        /// </summary>
        [Test]
        public async Task TestCacher()
        {
            var proxy = App.ResolveRequired<CacherProxy>();
            Assert.That(proxy != null, "proxy不能为null");
            await TestCacher(proxy!.Cacher, "默认实现");
            await TestCacher(proxy.Redis, "Redis实现");
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 测试缓存管理器
        /// </summary>
        /// <param name="cacher"></param>
        /// <param name="title"></param>
        private static async Task TestCacher(ICacher cacher, string title)
        {
            Assert.That(cacher != null, $"{title}:cacher不能为null");
            List<Task> tasks = [
                TestObjectCache(cacher!, title),
                TestHashCache(cacher!, title),
                TestSortedSetCache(cacher!, title),
                TestExpireTime(cacher!, title)
            ];
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 测试对象缓存
        /// </summary>
        /// <param name="cacher"></param>
        /// <param name="title"></param>
        private static async Task TestObjectCache(ICacher cacher, string title)
        {
            TestContext.Out.WriteLine($"{title}：测试对象缓存");
            CacheTestModel
               obj1 = new()
               {
                   Id = Guid.NewGuid().ToString(),
                   Name = "Obj1",
               },
               obj2 = new()
               {
                   Id = Guid.NewGuid().ToString(),
                   Name = "Obj2"
               };

            //  1、对象缓存
            Assert.That(false == await cacher.HasObject<CacheTestModel>(obj1.Id));
            CacheTestModel? tm = await cacher.GetObject<CacheTestModel>(obj1.Id);
            Assert.That(null == tm);
            Assert.That(0 == (await cacher.GetObject<CacheTestModel>([obj1.Id, obj2.Id])).Count);
            //      添加缓存
            await cacher.AddObject(obj1.Id, obj1);
            Assert.That(true == await cacher.HasObject<CacheTestModel>(obj1.Id));
            Assert.That(false == await cacher.HasObject<CacheTestModel>(obj2.Id));
            tm = await cacher.GetObject<CacheTestModel>(obj1.Id)!;
            Assert.That(obj1.Id == tm?.Id);
            Assert.That(null == await cacher.GetObject<CacheTestModel>(obj2.Id));
            //          添加obj2
            await cacher.AddObject(new Dictionary<string, CacheTestModel>() { { obj2.Id, obj2 } });
            Assert.That(true == await cacher.HasObject<CacheTestModel>(obj1.Id));
            Assert.That(true == await cacher.HasObject<CacheTestModel>(obj2.Id));
            Assert.That(obj1.Id == (await cacher.GetObject<CacheTestModel>(obj1.Id))?.Id);
            Assert.That(obj2.Id == (await cacher.GetObject<CacheTestModel>(obj2.Id))?.Id);
            Assert.That(obj2.Id != (await cacher.GetObject<CacheTestModel>(obj1.Id))?.Id);
            //          更新数据
            obj1.Name = "更新后的Obj";
            await cacher.AddObject<CacheTestModel>(obj1.Id, obj1);
            Assert.That(obj1.Name == (await cacher.GetObject<CacheTestModel>(obj1.Id))?.Name);
            Assert.That(obj2.Name == (await cacher.GetObject<CacheTestModel>(obj2.Id))?.Name);
            //      移除缓存
            await cacher.RemoveObject<CacheTestModel>(obj1.Id);
            Assert.That(false == await cacher.HasObject<CacheTestModel>(obj1.Id));
            Assert.That(true == await cacher.HasObject<CacheTestModel>(obj2.Id));
            tm = await cacher.GetObject<CacheTestModel>(obj1.Id);
            Assert.That(null == tm);
            Assert.That(null != await cacher.GetObject<CacheTestModel>(obj2.Id));
            //          obj2干掉
            await cacher.RemoveObject<CacheTestModel>(obj2.Id);
            Assert.That(false == await cacher.HasObject<CacheTestModel>(obj1.Id));
            Assert.That(false == await cacher.HasObject<CacheTestModel>(obj2.Id));
            tm = await cacher.GetObject<CacheTestModel>(obj1.Id);
            Assert.That(tm == null);
            Assert.That(null == await cacher.GetObject<CacheTestModel>(obj2.Id));
        }
        /// <summary>
        /// 测试hash缓存
        /// </summary>
        /// <param name="cacher"></param>
        /// <param name="title"></param>
        public static async Task TestHashCache(ICacher cacher, string title)
        {
            TestContext.Out.WriteLine($"{title}：测试Hash缓存");
            CacheTestModel
                obj1 = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Obj1",
                },
                obj2 = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Obj2"
                };
            //  2、Hash缓存 ExistsHash GetHashLength GetHashKeys GetHash  AddHash RemoveHash
            string hashKey = "Snail_TestHash";
            //      无数据
            Assert.That(false == await cacher.HasHash<CacheTestModel>(hashKey));
            Assert.That(0 == await cacher.GetHashLen<CacheTestModel>(hashKey));
            Assert.That(false == await cacher.HasHash<CacheTestModel>(hashKey, obj1.Id));
            Assert.That(false == await cacher.HasHash<CacheTestModel>(hashKey, obj2.Id));
            Assert.That(false == (await cacher.GetHashKeys<CacheTestModel>(hashKey)).Any() == true);
            Assert.That(false == (await cacher.GetHash<CacheTestModel>(hashKey)).Any() == true);
            Assert.That(false == (await cacher.GetHash<CacheTestModel>(hashKey, [obj1.Id, obj2.Id])).Any() == true);
            Assert.That(null == await cacher.GetHash<CacheTestModel>(hashKey, obj1.Id));
            Assert.That(null == await cacher.GetHash<CacheTestModel>(hashKey, obj2.Id));
            //      加数据：重复添加的情况做一下验证
            await cacher.AddHash<CacheTestModel>(hashKey, obj1.Id, obj1);
            Assert.That(true == await cacher.HasHash<CacheTestModel>(hashKey));
            Assert.That(1 == (await cacher.GetHash<CacheTestModel>(hashKey)).Count);
            Assert.That(1 == (await cacher.GetHashKeys<CacheTestModel>(hashKey)).Count);
            Assert.That(obj1.Id == (await cacher.GetHashKeys<CacheTestModel>(hashKey)).FirstOrDefault());
            Assert.That(1 == await cacher.GetHashLen<CacheTestModel>(hashKey));

            Assert.That(true == await cacher.HasHash<CacheTestModel>(hashKey, obj1.Id));
            Assert.That(null != await cacher.GetHash<CacheTestModel>(hashKey, obj1.Id));
            Assert.That(obj1.Id == (await cacher.GetHash<CacheTestModel>(hashKey, obj1.Id))?.Id);
            Assert.That(obj1.Name == (await cacher.GetHash<CacheTestModel>(hashKey, obj1.Id))?.Name);

            Assert.That(false == await cacher.HasHash<CacheTestModel>(hashKey, obj2.Id));
            Assert.That(null == await cacher.GetHash<CacheTestModel>(hashKey, obj2.Id));
            //         添加obj2
            await cacher.AddHash<CacheTestModel>(hashKey, obj2.Id, obj2);
            Assert.That(true == await cacher.HasHash<CacheTestModel>(hashKey));
            Assert.That(2 == (await cacher.GetHash<CacheTestModel>(hashKey)).Count);
            Assert.That(2 == (await cacher.GetHashKeys<CacheTestModel>(hashKey)).Count);
            Assert.That(true != (await cacher.GetHashKeys<CacheTestModel>(hashKey)).Except([obj1.Id, obj2.Id]).Any());
            Assert.That(true != (await cacher.GetHash<CacheTestModel>(hashKey)).Select(item => item.Value.Id).Except([obj1.Id, obj2.Id]).Any());
            Assert.That(2 == await cacher.GetHashLen<CacheTestModel>(hashKey));

            Assert.That(true == await cacher.HasHash<CacheTestModel>(hashKey, obj1.Id));
            Assert.That(null != await cacher.GetHash<CacheTestModel>(hashKey, obj1.Id));
            Assert.That(obj1.Id == (await cacher.GetHash<CacheTestModel>(hashKey, obj1.Id))?.Id);
            Assert.That(obj1.Name == (await cacher.GetHash<CacheTestModel>(hashKey, obj1.Id))?.Name);

            Assert.That(true == await cacher.HasHash<CacheTestModel>(hashKey, obj2.Id));
            Assert.That(null != await cacher.GetHash<CacheTestModel>(hashKey, obj2.Id));
            Assert.That(obj2.Id == (await cacher.GetHash<CacheTestModel>(hashKey, obj2.Id))?.Id);
            Assert.That(obj2.Name == (await cacher.GetHash<CacheTestModel>(hashKey, obj2.Id))?.Name);
            //      更新数据：确保不会对已有数据造成影响
            obj1.Name = "hash中更新值";
            await cacher.AddHash<CacheTestModel>(hashKey, obj1.Id, obj1);
            Assert.That(true == await cacher.HasHash<CacheTestModel>(hashKey));
            Assert.That(2 == (await cacher.GetHash<CacheTestModel>(hashKey)).Count);
            Assert.That(2 == (await cacher.GetHashKeys<CacheTestModel>(hashKey)).Count);
            Assert.That(true != (await cacher.GetHashKeys<CacheTestModel>(hashKey)).Except([obj1.Id, obj2.Id]).Any());
            Assert.That(2 == await cacher.GetHashLen<CacheTestModel>(hashKey));

            Assert.That(true == await cacher.HasHash<CacheTestModel>(hashKey, obj1.Id));
            Assert.That(true == await cacher.HasHash<CacheTestModel>(hashKey, obj2.Id));
            Assert.That(obj1.Name == (await cacher.GetHash<CacheTestModel>(hashKey, obj1.Id))?.Name);
            Assert.That(obj2.Name == (await cacher.GetHash<CacheTestModel>(hashKey, obj2.Id))?.Name);
            //      移除数据
            await cacher.RemoveHash<CacheTestModel>(hashKey, obj1.Id);
            Assert.That(true == await cacher.HasHash<CacheTestModel>(hashKey));
            Assert.That(1 == (await cacher.GetHash<CacheTestModel>(hashKey)).Count);
            Assert.That(1 == (await cacher.GetHashKeys<CacheTestModel>(hashKey)).Count);
            Assert.That(true != (await cacher.GetHashKeys<CacheTestModel>(hashKey)).Except([obj2.Id]).Any());
            Assert.That(1 == await cacher.GetHashLen<CacheTestModel>(hashKey));

            Assert.That(null == await cacher.GetHash<CacheTestModel>(hashKey, obj1.Id));
            Assert.That(false == await cacher.HasHash<CacheTestModel>(hashKey, obj1.Id));
            Assert.That(null != await cacher.GetHash<CacheTestModel>(hashKey, obj2.Id));
            Assert.That(true == await cacher.HasHash<CacheTestModel>(hashKey, obj2.Id));
            //          obj2干掉
            await cacher.RemoveHash<CacheTestModel>(hashKey, obj2.Id);
            Assert.That(false == await cacher.HasHash<CacheTestModel>(hashKey));
            Assert.That(false == (await cacher.GetHash<CacheTestModel>(hashKey)).Any());
            Assert.That(false == (await cacher.GetHashKeys<CacheTestModel>(hashKey)).Any());
            Assert.That(false == (await cacher.GetHashKeys<CacheTestModel>(hashKey)).Any());
            Assert.That(0 == await cacher.GetHashLen<CacheTestModel>(hashKey));
        }
        /// <summary>
        /// 测试SortedSet缓存
        /// </summary>
        /// <param name="cacher"></param>
        /// <param name="title"></param>
        public static async Task TestSortedSetCache(ICacher cacher, string title)
        {
            TestContext.Out.WriteLine($"{title}：测试SortedSet缓存");
            CacheTestModel
                obj1 = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Obj1",
                },
                obj2 = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Obj2"
                };
            string setKey = "Snail_SortedSet";
            await cacher.RemoveSortedSet<CacheTestModel>(setKey);
            // AddSortedSet  ExistsSortedSet GetSortedSetLength  GetSortedSetRank  GetSortedSet  GetSortedSetWithScore  RemoveSortedSet
            Assert.That(false == await cacher.HasSortedSet<CacheTestModel>(setKey));
            Assert.That(0 == await cacher.GetSortedSetLen<CacheTestModel>(setKey));

            //  添加
            await cacher.AddSortedSet<CacheTestModel>(setKey, obj1, 100);
            Assert.That(true == await cacher.HasSortedSet<CacheTestModel>(setKey));
            Assert.That(1 == await cacher.GetSortedSetLen<CacheTestModel>(setKey));
            Assert.That(0 == await cacher.GetSortedSetRank<CacheTestModel>(setKey, obj1));

            await cacher.AddSortedSet<CacheTestModel>(setKey, obj2, 80);
            Assert.That(0 == await cacher.GetSortedSetRank<CacheTestModel>(setKey, obj2));
            Assert.That(1 == await cacher.GetSortedSetRank<CacheTestModel>(setKey, obj1));
            Assert.That(true == await cacher.HasSortedSet<CacheTestModel>(setKey));
            Assert.That(2 == await cacher.GetSortedSetLen<CacheTestModel>(setKey));
            //      更新；若是更新Obj对象则实际上是添加了数据
            await cacher.AddSortedSet<CacheTestModel>(setKey, obj1, 200);
            Assert.That(1 == await cacher.GetSortedSetRank<CacheTestModel>(setKey, obj1));
            Assert.That(2 == await cacher.GetSortedSetLen<CacheTestModel>(setKey));
            obj2.Name = "更新sort";
            await cacher.AddSortedSet<CacheTestModel>(setKey, obj2, 70);
            Assert.That(2 == await cacher.GetSortedSetRank<CacheTestModel>(setKey, obj1));
            Assert.That(3 == await cacher.GetSortedSetLen<CacheTestModel>(setKey));

            //  移除
            await cacher.RemoveSortedSet<CacheTestModel>(setKey, 150.0f, 210);

        }
        /// <summary>
        /// 测试缓存失效时间
        /// </summary>
        private static async Task TestExpireTime(ICacher cacher, string title)
        {
            TestContext.Out.WriteLine($"{title}：测试缓存失效时间");
            CacheTestModel obj1 = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Obj1",
            };

            await cacher.AddObject<CacheTestModel>(obj1.Id, obj1, 5);
            Assert.That(null != await cacher.GetObject<CacheTestModel>(obj1.Id));
            Thread.Sleep(TimeSpan.FromSeconds(7));
            Assert.That(null == await cacher.GetObject<CacheTestModel>(obj1.Id));
            Assert.That(false == await cacher.HasObject<CacheTestModel>(obj1.Id));

            string hashKey = "Snail_HashKey";
            await cacher.AddHash<CacheTestModel>(hashKey, obj1.Id, obj1, 4);
            Assert.That(null != await cacher.GetHash<CacheTestModel>(hashKey, obj1.Id));
            Thread.Sleep(TimeSpan.FromSeconds(6));
            Assert.That(null == await cacher.GetHash<CacheTestModel>(hashKey, obj1.Id));
            Assert.That(false == await cacher.HasHash<CacheTestModel>(hashKey, obj1.Id));

            string setKey = "Snail_SortedSet_Expire";
            await cacher.AddSortedSet<CacheTestModel>(setKey, obj1, 100, 3);
            Assert.That(null != (await cacher.GetSortedSet<CacheTestModel>(setKey, 0, 0))?.FirstOrDefault());
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Assert.That(false == await cacher.HasSortedSet<CacheTestModel>(setKey));
            Assert.That(0 == await cacher.GetSortedSetLen<CacheTestModel>(setKey));
        }
        #endregion

        #region 私有类型
        /// <summary>
        /// 缓存记录器代理
        /// </summary>
        [Component]
        private class CacherProxy
        {
            /// <summary>
            /// 默认缓存器
            /// </summary>
            [Cacher, Server(Workspace = "Test", Code = "Default")]
            public required ICacher Cacher { init; get; }

            /// <summary>
            /// Redis缓存器
            /// </summary>
            [Cacher(ProviderKey = DIKEY_Redis), Server(Workspace = "Test", Code = "Redis")]
            public required ICacher Redis { init; get; }
        }

        /// <summary>
        /// 缓存测试实体
        /// </summary>
        [Cache(Type = typeof(LockMap<,>))]
        private class CacheTestModel
        {
            public string? Id { set; get; }

            public string? Name { set; get; }
        }
        #endregion
    }
}

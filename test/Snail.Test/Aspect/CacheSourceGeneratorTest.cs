using Snail.Abstractions.Common.Interfaces;
using Snail.Test.Aspect.Components;
using Snail.Test.Aspect.DataModels;

namespace Snail.Test.Aspect
{
    /// <summary>
    /// 缓存源码生成器测试
    /// </summary>
    public sealed class CacheSourceGeneratorTest
    {
        #region 公共方法
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task Test()
        {
            IApplication app = new Application();
            app.Run();


            CacheAspectTest aspect = app.ResolveRequired<CacheAspectTest>();
            //  List测试
            //      Object对象缓存
            {
                List<TestCache> list = await aspect.LoadListAbstract("1", "2");
                Assert.That(list?.Any() != true);
                list = await aspect.LoadList("1", "2");
                Assert.That(list?.Count == 2);
                list = await aspect.LoadListAbstract("1", "2");
                Assert.That(list?.Count == 1);
                await aspect.DeleteListAbstract("1", "2");
                list = await aspect.LoadListAbstract("1", "2");
                Assert.That(list?.Any() != true);

                list = await aspect.SaveList("1", "2");
                Assert.That(list?.Count == 2 && list.Where(item => item.Name == "SaveList").Count() == 2);
                list = await aspect.LoadListAbstract("1", "2");
                Assert.That(list?.Count == 1 && list.Where(item => item.Name == "SaveList").Count() == 1);
                list = await aspect.LoadList("1", "2");
                Assert.That(list?.Count == 2 && list.Where(item => item.Name == "SaveList").Count() == 2);

                await aspect.DeleteListAbstract("1", "2");
            }
            //      Hash测试
            {
                List<TestCache> list = await aspect.LoadHashListAbstract("1", "2");
                Assert.That(list?.Any() != true);
                list = await aspect.LoadHashList("1", "2");
                Assert.That(list?.Count == 2);
                list = await aspect.LoadHashListAbstract("1", "2");
                Assert.That(list?.Count == 2);

                await aspect.DeleteHashListAbstract("1", "2");
                list = await aspect.LoadHashListAbstract("1", "2");
                Assert.That(list?.Any() != true);

                list = await aspect.SaveHashList("1", "2");
                Assert.That(list?.Count == 2 && list.Where(item => item.Name == "SaveList").Count() == 2);
                list = await aspect.LoadHashListAbstract("1", "2");
                Assert.That(list?.Count == 2 && list.Where(item => item.Name == "SaveList").Count() == 2);
                list = await aspect.LoadHashList("1", "2");
                Assert.That(list?.Count == 2 && list.Where(item => item.Name == "SaveList").Count() == 2);

                await aspect.DeleteHashListAbstract("1", "2");
            }
            //  字典
            {
                IDictionary<string, TestCache>? map = await aspect.LoadDictAbstract("10");
                Assert.That(map?.Any() != true);
                map = await aspect.LoadDict("10");
                Assert.That(map?.Count == 1);
                await aspect.DeleteDictAbstract("10");
                map = await aspect.LoadDictAbstract("10");
                Assert.That(map?.Any() != true);
            }
            //  单个对象
            {
                TestCache cache = await aspect.LoadAbstract("20");
                Assert.That(cache == null);
                cache = await aspect.Load("20");
                Assert.That(cache?.Id == "20");
                cache = await aspect.LoadAbstract("20");
                Assert.That(cache?.Id == "20");
                await aspect.DeleteAbstract("20");
                cache = await aspect.LoadAbstract("20");
                Assert.That(cache == null);
                cache = await aspect.Save("20");
                Assert.That(cache?.Name == "Save");
                await aspect.LoadAbstract("20");
                Assert.That(cache?.Name == "Save");
                await aspect.DeleteAbstract("20");
            }
            //  数组
            {
                TestCache[] caches = await aspect.LoadArrayAbstract(["1", "2"]);
                Assert.That(caches?.Any() != true);
                caches = await aspect.LoadArray(["1", "2"]);
                Assert.That(caches?.Length == 2);
                caches = await aspect.LoadArrayAbstract(["1", "2"]);
                Assert.That(caches?.Length == 2);
                await aspect.DeleteArrayAbstract(["1", "2"]);
                caches = await aspect.LoadArrayAbstract(["1", "2"]);
                Assert.That(caches?.Any() != true);

                await aspect.LoadList("1", "2");
                caches = await aspect.LoadArrayAbstract(["1", "2"]);
                Assert.That(caches?.Length == 2);

                caches = await aspect.SaveArray(["1", "2"]);
                Assert.That(caches?.Count(item => item.Name == "SaveArray") == 2);
                caches = await aspect.LoadArrayAbstract(["1", "2"]);
                Assert.That(caches?.Count(item => item.Name == "SaveArray") == 2);
                await aspect.DeleteArrayAbstract(["1", "2"]);
            }
            //  数据包-单个对象 
            {
                {
                    await aspect.DeleteDataBagAbstract("30");
                    IDataBag<TestCache> bag = ((await aspect.LoadDataBagAbstract("30")) as IDataBag<TestCache>)!;
                    Assert.That(bag == null);
                    bag = await aspect.LoadDataBag("30");
                    Assert.That(bag?.GetData()?.Id == "30");
                    TestCache cache = await aspect.LoadAbstract("30");
                    Assert.That(cache?.Id == "30");
                    bag = await aspect.SaveDataBag("30");
                    Assert.That(bag?.GetData()?.Id == "30" && bag?.GetData()?.Name == "SaveDataBag");
                    await aspect.DeleteDataBagAbstract("30");
                    cache = await aspect.LoadAbstract("30");
                    Assert.That(cache == null);
                }
                {
                    var bag = await aspect.LoadDataBagAbstract("30");
                    Assert.That(bag == null);
                }
            }
            //  数据包-list
            {
                {
                    TestDataBag<List<TestCache>> bags = await aspect.LoadBagListAbstract("40", "41");
                    Assert.That(bags == null);
                }
                {
                    IDataBag<ListChild2<TestCache>> bags = await aspect.LoadBagList("40", "41");
                    Assert.That(bags?.GetData()?.Count == 2);
                    TestCache[] caches = await aspect.LoadArrayAbstract(["40", "41"]);
                    Assert.That(caches?.Length == 2);
                    await aspect.DeleteBagList("40", "41");

                }
                {
                    var bags = await aspect.LoadBagListAbstract("40", "41");
                    Assert.That(bags == null);
                }
            }
            //  整理逻辑和List重合，Array、Dictionary自身初始化等逻辑，和LoadArray、LoadDictionary等一致
        }
        #endregion


        #region 内部类型
        public class DataBagTest : IDataBag<TestCache>
        {
            /// <summary>
            /// 获取数据
            /// </summary>
            /// <returns></returns>
            public TestCache GetData() => new TestCache() { Id = "1" };
            /// <summary>
            /// 设置数据
            /// </summary>
            /// <param name="data"></param>
            public void SetData(TestCache? data)
            {
            }
        }

        public class DataBagsTest : IDataBag<IList<TestCache>>
        {
            /// <summary>
            /// 获取数据
            /// </summary>
            /// <returns></returns>
            public IList<TestCache> GetData() => [new TestCache() { Id = "1" }];
            /// <summary>
            /// 设置数据
            /// </summary>
            /// <param name="data"></param>
            public void SetData(IList<TestCache>? data)
            {
            }
        }
        #endregion
    }
}

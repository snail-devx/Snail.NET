using Snail.Test.Aspect.Components;
using Snail.Test.Aspect.DataModels;
using Snail.Utilities.Common.Interfaces;

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
            Application app = new Application();
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
                Dictionary<string, TestCache>? map = await aspect.LoadDictAbstract("10");
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
            //  IPayload-单个对象 
            {
                {
                    await aspect.DeletePayloadAbstract("30");
                    IPayload<TestCache> bag = ((await aspect.LoadPayloadAbstract("30")) as IPayload<TestCache>)!;
                    Assert.That(bag == null);
                    bag = await aspect.LoadPayload("30");
                    Assert.That(bag?.Payload?.Id == "30");
                    TestCache cache = await aspect.LoadAbstract("30");
                    Assert.That(cache?.Id == "30");
                    bag = await aspect.SavePayload("30");
                    Assert.That(bag?.Payload?.Id == "30" && bag?.Payload?.Name == "SavePayload");
                    await aspect.DeletePayloadAbstract("30");
                    cache = await aspect.LoadAbstract("30");
                    Assert.That(cache == null);
                }
                {
                    var bag = await aspect.LoadPayloadAbstract("30");
                    Assert.That(bag == null);
                }
            }
            //  IPayload-list
            {
                {
                    TestPayload<List<TestCache>> bags = await aspect.LoadPayloadListAbstract("40", "41");
                    Assert.That(bags == null);
                }
                {
                    TestPayload<ListChild2<TestCache>> bags = await aspect.LoadPayloadList("40", "41");
                    Assert.That(bags?.Payload?.Count == 2);
                    TestCache[] caches = await aspect.LoadArrayAbstract(["40", "41"]);
                    Assert.That(caches?.Length == 2);
                    await aspect.DeletePayloadList("40", "41");

                }
                {
                    var bags = await aspect.LoadPayloadListAbstract("40", "41");
                    Assert.That(bags == null);
                }
            }
            //  整理逻辑和List重合，Array、Dictionary自身初始化等逻辑，和LoadArray、LoadDictionary等一致
        }
        #endregion


        #region 内部类型
        public class PayloadTest : IPayload<TestCache>
        {
            public TestCache? Payload
            {
                get => new TestCache() { Id = "1" };
                set { }
            }
        }

        public class PayloadsTest : IPayload<IList<TestCache>>
        {
            public IList<TestCache>? Payload
            {
                get => [new TestCache() { Id = "1" }];
                set { }
            }
        }
        #endregion
    }
}

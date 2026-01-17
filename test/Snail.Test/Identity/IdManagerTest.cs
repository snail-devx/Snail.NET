using Snail.Abstractions.Identity;
using Snail.Abstractions.Identity.Interfaces;
using Snail.Identity;
using Snail.Identity.Components;

namespace Snail.Test.Identity
{
    /// <summary>
    /// 主键Id管理器
    /// </summary>
    public sealed class IdManagerTest : UnitTestApp
    {
        /// <summary>
        /// 测试默认管理器
        /// </summary>
        [Test]
        public void TestDefault()
        {
            var provider = App.Resolve<IIdProvider>();
            TestIdProvider(provider, "默认管理器", typeof(SnowFlakeIdProvider));
            provider = App.Resolve<IIdProvider>(key: DIKEY_Guid);
            TestIdProvider(provider, "Guid管理器", typeof(GuidIdProvider));
            provider = App.Resolve<IIdProvider>(key: DIKEY_SnowFlake);
            TestIdProvider(provider, "SnowFlake管理器", typeof(SnowFlakeIdProvider));
        }


        #region 私有方法
        /// <summary>
        /// 测试指定管理器
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="title"></param>
        /// <param name="targetType"></param>
        private void TestIdProvider(IIdProvider? provider, string title, Type targetType)
        {
            IIdGenerator generator = new IdGenerator(App, null, provider);

            Assert.That(provider != null && provider.GetType() == targetType, $"{title}是{targetType.FullName}");
            string id = provider!.NewId();
            Assert.That(id?.Length > 0, $"{title}生成的Id不能为空：{id}");
            id = generator.NewId();
            Assert.That(id?.Length > 0, $"{title}生成的Id不能为空：{id}");
            //  同线程获取确保唯一
            Dictionary<string, string> map = new Dictionary<string, string>();
            Dictionary<string, string> map2 = new Dictionary<string, string>();
            for (int index = 0; index < 10000; index++)
            {
                id = provider.NewId();
                TestContext.Out.WriteLine($"{title}生成的Id：{id}");
                map[id] = id;
                id = generator.NewId();
                map2[id] = id;
            }
            Assert.That(map.Count == 10000, $"provider:{title}单线程循环10000次应当生成10000个主键Id值:{map.Count}");
            Assert.That(map2.Count == 10000, $"generator:{title}单线程循环10000次应当生成10000个主键Id值:{map.Count}");
            //  多线程并行确保唯一
            map.Clear();
            map2.Clear();
            Parallel.For(0, 10000, index =>
            {
                string id = provider.NewId();
                string id2 = generator.NewId();
                lock (map)
                {
                    map[id] = id;
                    map2[id] = id2;
                }
            });
            Assert.That(map.Count == 10000, $"provider:{title}10000多线程应当生成10000个主键Id值:{map.Count}");
            Assert.That(map2.Count == 10000, $"generator:{title}10000多线程应当生成10000个主键Id值:{map.Count}");
        }
        #endregion
    }
}

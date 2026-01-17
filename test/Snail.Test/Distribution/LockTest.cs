using Snail.Abstractions.Distribution;
using Snail.Abstractions.Distribution.Attributes;
using Snail.Abstractions.Web.Extensions;
using Snail.Redis;
using StackExchange.Redis;

namespace Snail.Test.Distribution
{
    /// <summary>
    /// 锁测试
    /// </summary>
    public sealed class LockTest : UnitTestApp
    {
        #region 公共方法
        /// <summary>
        /// Redis原生方法
        /// </summary>
        [Test]
        public void TestRedisOrigin()
        {
            //  加锁时value没啥用，始终是根据key判定锁
            IDatabase db;
            {
                RedisManager manager = App.ResolveRequired<RedisManager>();
                string server = manager.GetServer(workspace: "Test", code: "Default")!.Server;
                ConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(server);
                db = multiplexer.GetDatabase(2);
            }

            Assert.That(db.LockTake("snaillock", "111", TimeSpan.FromSeconds(10)) == true, "第一次加锁");
            Assert.That(db.LockTake("snaillock", "111", TimeSpan.FromSeconds(10)) == false, "第二次次加锁");
            //      不同值，同Key加锁
            Assert.That(db.LockTake("snaillock", "222", TimeSpan.FromSeconds(10)) == false, "不同value加锁");
            Assert.That(db.LockTake("snaillock", "222", TimeSpan.FromSeconds(10)) == false, "不同value第二次加锁");
            //  睡眠后，重新加锁
            Thread.Sleep(12 * 1000);
            Assert.That(db.LockTake("snaillock", "111", TimeSpan.FromSeconds(10)) == true, "睡眠后加锁");
            //  删除锁时，解锁时，要传入加锁时的值
            Assert.That(db.LockTake("snaillock-delete", "111", TimeSpan.FromSeconds(10)) == true, "测试删除加锁");
            Assert.That(db.LockRelease("snaillock-delete", "信息进行进行进行") == false, "删除锁，value随便传的");
            Assert.That(db.LockRelease("snaillock-delete", "111") == true, "删除锁，value为加锁时的值");
            Assert.That(db.LockTake("snaillock-delete", "111", TimeSpan.FromSeconds(10)) == true, "删除后再次加锁");
            Assert.That(db.LockRelease("snaillock-delete", "信息进行进行进行") == false, "第二次删除锁，value随便传的");
            Assert.That(db.LockRelease("snaillock-delete", "111") == true, "删除后再次解锁，value为加锁时的值");
        }

        /// <summary>
        /// 测试管理器加锁
        /// </summary>
        [Test]
        public async Task TestLocker()
        {
            ILocker locker = App.ResolveRequired<LockerProxy>().Locker;
            Assert.That(locker != null, "加锁器不能为null");

            Assert.That(await locker!.Lock("snaillock2", "111", expireSeconds: 10) == true, "第一次加锁");
            Assert.That(await locker.Lock("snaillock2", "111", maxTryCount: 10, expireSeconds: 10) == false, "第二次加锁");
            //  不同值，同Key加锁
            Assert.That(await locker.Lock("snaillock2", "222", maxTryCount: 10, expireSeconds: 10) == false, "不同value加锁");
            Assert.That(await locker.Lock("snaillock2", "222", maxTryCount: 10, expireSeconds: 10) == false, "不同value第二次加锁");
            //  睡眠后，重新加锁；测试失效时间是否生效
            Thread.Sleep(10 * 1000);
            Assert.That(await locker.Lock("snaillock2", "111", expireSeconds: 10) == true, "睡眠后加锁");
            //  测试解锁
            Assert.That(await locker.Lock("snaillock-delete2", "111", expireSeconds: 100) == true, "测试删除加锁");
            Assert.That(await locker.Unlock("snaillock-delete2", "随便传值") == false, "删除锁，value随便传的");
            Assert.That(await locker.Unlock("snaillock-delete2", "111") == true, "删除锁，value为加锁时的值");
            Assert.That(await locker.Lock("snaillock-delete2", "111", expireSeconds: 100) == true, "删除后再次加锁");
            Assert.That(await locker.Lock("snaillock-delete2", "111", expireSeconds: 100) == false, "删除后第二次加锁");
            Assert.That(await locker.Unlock("snaillock-delete2", "111") == true, "删除锁，value为加锁时的值");

            //  测试多线程加锁
            Dictionary<int, bool> dict = new Dictionary<int, bool>();
            await Parallel.ForAsync(0, 10, async (index, _) =>
            {
                bool bValue = await locker.Lock("snail-threadlock", "dddddddddd", expireSeconds: 30);
                lock (dict)
                {
                    dict[index] = bValue;
                }
            });
            Thread.Sleep(TimeSpan.FromSeconds(4));
            Assert.That(dict.Count(kv => kv.Value == true) == 1, "只有一个加锁成功才对");
        }
        #endregion

        #region 私有类型
        /// <summary>
        /// 加锁代理
        /// </summary>
        [Component]
        private class LockerProxy
        {
            /// <summary>
            /// 加锁器
            /// </summary>
            [Locker, Server(Workspace = "Test", Code = "Default")]
            public required ILocker Locker { init; get; }
        }
        #endregion
    }
}

using Snail.Utilities.Collections;
using Snail.Utilities.Common.Extensions;
using Snail.Utilities.Threading;

namespace Snail.Test.Concurrent
{
    /// <summary>
    /// C#常用锁测试
    /// </summary>
    public sealed class LockTest
    {
        #region SemaphoreSlim
        /// <summary>
        /// 测试<see cref="SemaphoreSlim"/>,异步锁
        /// </summary>
        /// <returns></returns>
        [Test]
        public void TestSemaphoreSlim()
        {
            object lockObj = new object();
            SemaphoreSlim slim = new SemaphoreSlim(1, 1);
            IDictionary<int, string> map = new Dictionary<int, string>();
            IList<Task> tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                Action<int> action = index =>
                {
                    Task.Run(async () =>
                    {
                        await slim.WaitAsync();
                        await Task.Delay(10);
                        await Task.Delay(10);
                        await Task.Delay(10);
                        map[index] = index.ToString();

                        slim.Release();
                    }).AddTo(tasks);
                };
                action.Invoke(i);
            }
            Task.WaitAll(tasks);
            Assert.That(map.Count == 100, $"期望100，实际：{map.Count}");
        }

        /// <summary>
        /// 异步锁测试
        /// </summary>
        [Test]
        public void TestAsyncLock()
        {
            AsyncLock aLock = new AsyncLock();
            IDictionary<int, string> map = new Dictionary<int, string>();
            IList<Task> tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                Action<int> action = index =>
                {
                    Task.Run(async () =>
                    {
                        using (await aLock.Await())
                        {
                            await Task.Delay(10);
                            await Task.Delay(10);
                            await Task.Delay(10);
                            map[index] = index.ToString();
                        }
                    }).AddTo(tasks);
                };
                action.Invoke(i);
            }
            Task.WaitAll(tasks);
            Assert.That(map.Count == 100, $"期望100，实际：{map.Count}");
        }
        /// <summary>
        /// 异步锁测试
        /// </summary>
        [Test]
        public void TestAsyncLock2()
        {
            AsyncLock aLock = new AsyncLock();
            IDictionary<int, string> map = new Dictionary<int, string>();
            IList<Task> tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                Action<int> action = index =>
                {
                    Task.Run(() =>
                    {
                        using (aLock.Wait())
                        {
                            map[index] = index.ToString();
                        }
                    }).AddTo(tasks);
                };
                action.Invoke(i);
            }
            Task.WaitAll(tasks);
            Assert.That(map.Count == 100, $"期望100，实际：{map.Count}");
        }
        #endregion

        #region ReaderWriterLockSlim
        /// <summary>
        /// 测试读写锁
        /// </summary>
        [Test]
        public void TestReaderWriterLockSlim()
        {
            ReaderWriterLockSlim rwLock = new();
            List<String> strs = new();
            List<Task> tasks = new();
            #region 读、写锁
            //  两个读锁：不受影响，各自独立运行
            {
                /*
                    [0]: "9 EnterReadLock：16:46:57:100"
                    [1]: "12 EnterReadLock：16:46:57:100"
                    [2]: "12 ExitReadLock：16:47:02:127"
                    [3]: "9 ExitReadLock：16:47:02:127"
                 */
                //tasks.Add(new Task(() =>
                //{
                //    rwLock.EnterReadLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitReadLock();
                //}));
                //tasks.Add(new Task(() =>
                //{
                //    rwLock.EnterReadLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitReadLock();
                //}));
            }
            //  两个写锁：互斥
            {
                /* 互斥
                    [0]: "12 EnterWriteLock：16:51:58:497"
                    [1]: "12 ExitWriteLock：16:52:03:523"
                    [2]: "10 EnterWriteLock：16:52:03:523"
                    [3]: "10 ExitWriteLock：16:52:08:532"
                 */
                //tasks.Add(new Task(() =>
                //{
                //    rwLock.EnterWriteLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterWriteLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitWriteLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitWriteLock();
                //}));
                //tasks.Add(new Task(() =>
                //{
                //    rwLock.EnterWriteLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterWriteLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitWriteLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitWriteLock();
                //}));
            }

            //  一个读、一个写；写先进，则读得等着
            {
                /*
                    [0]: "10 EnterWriteLock：16:54:12:856"
                    [1]: "10 ExitWriteLock：16:54:17:873"
                    [2]: "12 EnterReadLock：16:54:17:873"
                    [3]: "12 ExitReadLock：16:54:22:889"
                 */
                //tasks.Add(new Task(() =>
                //{
                //    rwLock.EnterWriteLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterWriteLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitWriteLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitWriteLock();
                //}));
                //tasks.Add(new Task(() =>
                //{
                //    //  确保写先进入
                //    Thread.Sleep(TimeSpan.FromSeconds(1));
                //    rwLock.EnterReadLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitReadLock();
                //}));
            }
            //  一个读、一个写；读先进；写得等读完了才进入
            {
                /* 
                    [0]: "10 EnterReadLock：16:57:50:528"
                    [1]: "10 ExitReadLock：16:57:53:565"
                    [2]: "12 EnterWriteLock：16:57:53:566"
                    [3]: "12 ExitWriteLock：16:57:58:574"*/
                //tasks.Add(new Task(() =>
                //{
                //    rwLock.EnterReadLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(3));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitReadLock();
                //}));
                //tasks.Add(new Task(() =>
                //{
                //    //  确保读先进入
                //    Thread.Sleep(TimeSpan.FromSeconds(1));
                //    rwLock.EnterWriteLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterWriteLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitWriteLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitWriteLock();
                //}));
            }

            #endregion

            #region 可升级锁
            //      两个可升级写锁，互斥
            {
                /* 两个可升级读写锁同时运行时，只有一个运行完了，另一个才能进入
                    [0]: "10 EnterUpgradeableReadLock：16:30:25:153"
                    [1]: "10 ExitUpgradeableReadLock：16:30:30:173"
                    [2]: "9 EnterUpgradeableReadLock：16:30:30:173"
                    [3]: "9 ExitUpgradeableReadLock：16:30:35:179"
                 */
                //tasks.Add(new Task(() =>
                //{
                //    rwLock.EnterUpgradeableReadLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitUpgradeableReadLock();
                //}));
                //tasks.Add(new Task(() =>
                //{
                //    rwLock.EnterUpgradeableReadLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitUpgradeableReadLock();
                //}));
            }
            //      两个可升级锁+1个读锁:读锁不受影响，正常逻辑；两个可升级锁互斥
            {
                /*  读锁不受影响，正常逻辑；两个可升级锁互斥
                    [0]: "9 EnterUpgradeableReadLock：17:05:02:038"
                    [1]: "17 EnterReadLock：17:05:03:073"
                    [2]: "17 ExitReadLock：17:05:06:083"
                    [3]: "9 ExitUpgradeableReadLock：17:05:07:061"
                    [4]: "10 EnterUpgradeableReadLock：17:05:07:061"
                    [5]: "10 ExitUpgradeableReadLock：17:05:12:076"
                 */
                //tasks.Add(new Task(() =>
                //{
                //    rwLock.EnterUpgradeableReadLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitUpgradeableReadLock();
                //}));
                //tasks.Add(new Task(() =>
                //{
                //    rwLock.EnterUpgradeableReadLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitUpgradeableReadLock();
                //}));
                //tasks.Add(new Task(() =>
                //{
                //    //  确保升级先进入
                //    Thread.Sleep(TimeSpan.FromSeconds(1));
                //    rwLock.EnterReadLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(3));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitReadLock();
                //}));
            }
            //      两个可升级锁+1个写锁：先两个升级锁，再一个写锁
            {
                /*  三者互斥：先两个升级锁，再一个写锁
                    [0]: "9 EnterUpgradeableReadLock：17:03:35:689"
                    [1]: "9 ExitUpgradeableReadLock：17:03:40:723"
                    [2]: "17 EnterWriteLock：17:03:40:724"
                    [3]: "17 ExitWriteLock：17:03:43:728"
                    [4]: "12 EnterUpgradeableReadLock：17:03:43:728"
                    [5]: "12 ExitUpgradeableReadLock：17:03:48:729"
                */
                //tasks.Add(new Task(() =>
                //{
                //    rwLock.EnterUpgradeableReadLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitUpgradeableReadLock();
                //}));
                //tasks.Add(new Task(() =>
                //{
                //    rwLock.EnterUpgradeableReadLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitUpgradeableReadLock();
                //}));
                //tasks.Add(new Task(() =>
                //{
                //    //  确保升级先进入
                //    Thread.Sleep(TimeSpan.FromSeconds(1));
                //    rwLock.EnterWriteLock();
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterWriteLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    Thread.Sleep(TimeSpan.FromSeconds(3));
                //    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitWriteLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                //    rwLock.ExitWriteLock();
                //}));
            }

            //      升级锁内部转写、一个读锁：写独占，才能读
            {
                /*  升级锁内部转写、一个读锁：升级先。写优先
                    [0]: "10 EnterUpgradeableReadLock：17:12:15:683"
                    [1]: "10 EnterWriteLock：17:12:15:691"
                    [2]: "10 ExitWriteLock：17:12:16:706"
                    [3]: "12 EnterReadLock：17:12:16:707"
                    [4]: "10 ExitUpgradeableReadLock：17:12:19:720"
                    [5]: "12 ExitReadLock：17:12:21:713"
                 */
                tasks.Add(new Task(() =>
                {
                    rwLock.EnterUpgradeableReadLock();
                    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                    rwLock.EnterWriteLock();
                    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterWriteLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    rwLock.ExitWriteLock();
                    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitWriteLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitUpgradeableReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                    rwLock.ExitUpgradeableReadLock();
                }));
                tasks.Add(new Task(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    rwLock.EnterReadLock();
                    strs.Add($"{Thread.CurrentThread.ManagedThreadId} EnterReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    strs.Add($"{Thread.CurrentThread.ManagedThreadId} ExitReadLock：{DateTime.Now.ToString("HH:mm:ss:fff")}");
                    rwLock.ExitReadLock();
                }));
            }

            #endregion
            //  运行任务
            tasks.ForEach(task => task.Start());
            Task.WaitAll(tasks.ToArray());
        }
        /// <summary>
        /// 递归策略测试
        /// </summary>
        [Test]
        public void TesttReaderWriterLockSlimRecursionPolicy()
        {
            //  固定长度2；若为0，后续操作都无效
            Span<String> span = new Span<string>(["1", "2"]);
            //      fill填充所有数据，最终索引0、1数据都是 3333333333
            span.Fill("1111111111");
            span.Fill("3333333333");
            //      将索引0数据改为 惺惺惜惺惺想
            span[0] = "惺惺惜惺惺想";


            List<Task> tasks;
            ReaderWriterLockSlim rwLock;
            //  不允许递归：
            // rwLock = new(LockRecursionPolicy.NoRecursion);
            //tasks = new List<Task>();
            //new Task(() => EnterReadLock(rwLock)).AddToList(tasks).Start();
            //new Task(() => EnterReadLock(rwLock)).AddToList(tasks).Start();
            //Task.WaitAll(tasks);


            //  允许递归
            rwLock = new(LockRecursionPolicy.SupportsRecursion);
            tasks = new();
            new Task(() => EnterReadLock(rwLock)).AddTo(tasks).Start();
            new Task(() => EnterReadLock(rwLock)).AddTo(tasks).Start();

            Task.WaitAll(tasks);
        }

        private void EnterReadLock(ReaderWriterLockSlim rwLock, bool needRe = true)
        {
            rwLock.EnterReadLock();
            //Assert.Warn("CurrentReadCount:" + rwLock.CurrentReadCount);
            TestContext.Out.WriteLine("CurrentReadCount:" + rwLock.CurrentReadCount);
            //  构建递归；若出现递归，在LockRecursionPolicy.NoRecursion时会报错
            if (needRe == true) EnterReadLock(rwLock, false);
            Thread.Sleep(1000);
            rwLock.ExitReadLock();
        }
        #endregion

        #region Interlocked
        /// <summary>
        /// 测试 interlocked 加锁机制
        /// </summary>
        /// <returns></returns>
        [Test]
        public void Testinterlocked()
        {
            int count = 0;
            List<Task> tasks = [];
            for (int i = 0; i < 1000; i++)
            {
                Task.Run(() =>
                {
                    Interlocked.Increment(ref count);
                }).AddTo(tasks);
            }
            Task.WaitAll(tasks);
            Assert.That(count == 1000);

            tasks.Clear();
            count = 0;
            LockList<int> values = new();
            for (int i = 0; i < 1000; i++)
            {
                Task.Run(() =>
                {
                    //  Interlocked 有问题：无法保证顺序性，无法按照先后顺序得到顺序值
                    //      Interlocked 保证的是 “操作的原子性”，而非 “请求的全局顺序性”。
                    //int current, next;
                    //do
                    //{
                    //    current = count;
                    //    next = (current == int.MaxValue) ? 1 : current + 1;
                    //}
                    //while (Interlocked.CompareExchange(ref count, next, current) != current);
                    //values.Add(next);
                    //Debug.WriteLine(next);
                    lock (values)
                    {
                        count += 1;
                        values.Add(count);
                    }
                }).AddTo(tasks);

            }
            Task.WaitAll(tasks);
        }
        #endregion
    }
}

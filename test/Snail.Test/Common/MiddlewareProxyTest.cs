using Snail.Abstractions.Common.Interfaces;
using Snail.Utilities.Collections.Extensions;

namespace Snail.Test.Common
{
    /// <summary>
    /// 中间件代理测试
    /// </summary>
    public sealed class MiddlewareProxyTest
    {
        /// <summary>
        /// 测试中间件
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public delegate bool TestMiddleware(IList<string> x);
        /// <summary>
        /// 测试中间件：异步
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public delegate Task<bool> TestMiddlewareAsync(IList<string> x);

        /// <summary>
        /// 测试
        /// </summary>
        [Test]
        public async Task Test()
        {
            IMiddlewareProxy<TestMiddleware> proxy = new MiddlewareProxy<TestMiddleware>();
            TestMiddleware px = proxy.Build(strs =>
            {
                strs.Add("得失");
                return true;
            });
            IList<string> strs = new List<string>();
            Assert.That(px(strs), "返回值为true");
            Assert.That(strs.Count == 1 && strs.First() == "得失", "只有一条数据");

            //  测试use和build的高阶用法
            TestMiddlewareUse(new MiddlewareProxy<TestMiddleware>());
            TestMiddlewareBuild(new MiddlewareProxy<TestMiddleware>());
            await TestMiddlewareUse(new MiddlewareProxy<TestMiddlewareAsync>());
            await TestMiddlewareBuild(new MiddlewareProxy<TestMiddlewareAsync>());
            TestContext.Out.WriteLine("测试完成");
        }

        #region 私有方法
        /// <summary>
        /// 测试use
        /// </summary>
        /// <param name="proxy"></param>
        private static void TestMiddlewareUse(IMiddlewareProxy<TestMiddleware> proxy)
        {
            proxy.Use("name", next =>
            {
                return strs =>
                {
                    strs.Add("1");
                    return next.Invoke(strs);
                };
            });
            proxy.Use(next =>
            {
                return strs =>
                {
                    strs.Add("2");
                    return next.Invoke(strs);
                };
            });
            proxy.Use("name", next =>
            {
                return strs =>
                {
                    strs.Add("name-1");
                    return next.Invoke(strs);
                };
            });
            proxy.Use("name4", next =>
            {
                return strs =>
                {
                    strs.Add("4");
                    return next.Invoke(strs);
                };
            });
            proxy.Use(next =>
            {
                return strs =>
                {
                    strs.Add("5");
                    return next.Invoke(strs);
                };
            });
            proxy.Use("name4", next =>
            {
                return strs =>
                {
                    strs.Add("use-4");
                    return next.Invoke(strs);
                };
            });

            IList<string> list = new List<string>();
            proxy.Build(strs => true, onionMode: true).Invoke(list);
            Assert.That("name-1 2 use-4 5" == string.Join(' ', list), "测试use替换逻辑：name-1 2 use-4 5");
        }
        /// <summary>
        /// 测试中间件构建
        /// </summary>
        /// <param name="proxy"></param>
        private static void TestMiddlewareBuild(IMiddlewareProxy<TestMiddleware> proxy)
        {
            //  加几个中间件
            proxy.Use(next =>
            {
                return strs =>
                {
                    strs.Add("1");
                    return next(strs);
                };
            });
            proxy.Use(next =>
            {
                return strs =>
                {
                    strs.Add("2");
                    return next(strs);
                };
            });
            proxy.Use(next =>
            {
                return strs =>
                {
                    strs.Add("3");
                    return next(strs);
                };
            });
            TestMiddleware start = strs =>
            {
                strs.Add("4");
                return true;
            };

            //  洋葱模型
            IList<string> list = new List<string>();
            proxy.Build(start, onionMode: true).Invoke(list);
            Assert.That("1 2 3 4" == string.Join(' ', list), "洋葱模式下，输出结果：1 2 3 4");
            list.Clear();
            proxy.Build(start, onionMode: false).Invoke(list);
            Assert.That("3 2 1 4" == string.Join(' ', list), "非洋葱模式下，输出结果：3 2 1 4");
        }


        /// <summary>
        /// 测试use
        /// </summary>
        /// <param name="proxy"></param>
        private static async Task TestMiddlewareUse(IMiddlewareProxy<TestMiddlewareAsync> proxy)
        {
            proxy.Use("name", next =>
            {
                return strs =>
                {
                    strs.Add("1");
                    return next.Invoke(strs);
                };
            });
            proxy.Use(next =>
            {
                return strs =>
                {
                    strs.Add("2");
                    return next.Invoke(strs);
                };
            });
            proxy.Use("name", next =>
            {
                return strs =>
                {
                    strs.Add("name-1");
                    return next.Invoke(strs);
                };
            });
            proxy.Use("name4", next =>
            {
                return strs =>
                {
                    strs.Add("4");
                    return next.Invoke(strs);
                };
            });
            proxy.Use(next =>
            {
                return strs =>
                {
                    strs.Add("5");
                    return next.Invoke(strs);
                };
            });
            proxy.Use("name4", next =>
            {
                return strs =>
                {
                    strs.Add("use-4");
                    return next.Invoke(strs);
                };
            });

            IList<string> list = new List<string>();
            await proxy.Build(async strs =>
            {
                return await Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    return true;
                });
            }, onionMode: true).Invoke(list);
            Assert.That("name-1 2 use-4 5" == string.Join(' ', list), "测试use替换逻辑：name-1 2 use-4 5");
            TestContext.Out.WriteLine("顶顶顶顶顶顶顶顶顶顶顶顶顶");
        }
        /// <summary>
        /// 测试中间件构建
        /// </summary>
        /// <param name="proxy"></param>
        private static async Task TestMiddlewareBuild(IMiddlewareProxy<TestMiddlewareAsync> proxy)
        {
            //  加几个中间件
            proxy.Use(next =>
            {
                return strs =>
                {
                    strs.Add("1");
                    return next(strs);
                };
            });
            proxy.Use(next =>
            {
                return strs =>
                {
                    strs.Add("2");
                    return next(strs);
                };
            });
            proxy.Use(next =>
            {
                return strs =>
                {
                    strs.Add("3");
                    return next(strs);
                };
            });
            TestMiddlewareAsync start = async strs =>
            {
                strs.Add("4");
                await Task.Run(() => Thread.Sleep(1000));
                return true;
            };

            //  洋葱模型
            IList<string> list = new List<string>();
            await proxy.Build(start, onionMode: true).Invoke(list);
            Assert.That("1 2 3 4" == string.Join(' ', list), "洋葱模式下，输出结果：1 2 3 4");
            list.Clear();
            await proxy.Build(start, onionMode: false).Invoke(list);
            Assert.That("3 2 1 4" == string.Join(' ', list), "非洋葱模式下，输出结果：3 2 1 4");
            TestContext.Out.WriteLine("西溪新");
        }
        #endregion
    }
}

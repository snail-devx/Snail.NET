using Snail.Abstractions.Logging;
using Snail.Abstractions.Logging.Attributes;
using Snail.Abstractions.Logging.Extensions;

namespace Snail.Test.Logging
{
    /// <summary>
    /// 日志助手类
    /// </summary>
    public sealed class LoggingTest : UnitTestApp
    {

        /// <summary>
        /// 日志测试
        /// </summary>
        [Test]
        public void LogTest()
        {
            ILogger? logger = App.Resolve<ILogger>();
            Assert.That(logger != null, "日志代理管理器不能为null");

            LoggerProxy proxy = App.ResolveRequired<LoggerProxy>();
            Assert.That(proxy.Default != proxy.FileLogger, "proxy.Default != proxy.FileLogger");
            Assert.That(proxy.Default != proxy.Log4Net, "proxy.Default != proxy.Log4Net");
            Assert.That(proxy.Log4Net != proxy.FileLogger, "proxy.Log4Net != proxy.FileLogger");

            TestLog(proxy!.Default, "默认管理器");
            TestLog(proxy.FileLogger, "FileLogger管理器");
            TestLog(proxy.Log4Net, "Log4Net管理器");
            RunContext.New();
            TestLog(proxy!.Default, "默认管理器");
            TestLog(proxy.FileLogger, "FileLogger管理器");

            TestLog(proxy!.Default!.Scope("测试子管理"), "默认管理器Scope");
            TestLog(proxy.FileLogger!.Scope("测试子管理"), "FileLogger管理器Scope");
            TestLog(proxy.Log4Net!.Scope("测试子管理"), "Log4Net管理器Scope");

            Parallel.For(0, 100, index =>
            {
                TestLog(proxy!.Default, "默认管理器多线程");
                TestLog(proxy.FileLogger, "FileLogger管理器多线程");
                TestLog(proxy.Log4Net, "Log4Net管理器多线程");
            });
        }

        #region 私有方法
        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="title"></param>
        private static void TestLog(ILogger? manager, string title)
        {
            Assert.That(manager != null, $"{title}不能为null");
            manager!.Trace("测试Trace", $"{title}:{DateTime.Now}：+顶顶顶顶顶顶顶顶顶顶顶顶顶\r\n");
            manager!.Debug("测试Debug", $"{title}:的点点滴滴党史学习嘻嘻嘻");
            manager!.Info("测试Info", $"{title}:的点点滴滴党史学习嘻嘻嘻");
            manager!.Warn("测试Warn", $"{title}:的点点滴滴党史学习嘻嘻嘻");
            manager!.Error("测试Error", $"{title}:的点点滴滴党史学习嘻嘻嘻");
            manager!.System("测试System", $"{title}:的点点滴滴党史学习嘻嘻嘻");
        }
        #endregion

        #region 私有类型
        /// <summary>
        /// 日志记录器代理，测试依赖注入
        /// </summary>
        [Component]
        private class LoggerProxy
        {
            /// <summary>
            /// 默认日志管理器
            /// </summary>
            [Logger]
            public required ILogger Default { init; get; }

            /// <summary>
            /// 文件日志
            /// </summary>
            [Logger(ProviderKey = DIKEY_FileLogger)]
            public required ILogger FileLogger { init; get; }

            /// <summary>
            /// Log4Net日志记录器
            /// </summary>

            [Logger(ProviderKey = "Log4Net")]
            public required ILogger Log4Net { init; get; }
        }
        #endregion
    }
}

using Snail.Utilities.Common;

namespace Snail.Test.Common
{
    /// <summary>
    /// 多线程测试
    /// </summary>
    public sealed class ThreadTest
    {
        #region 公共方法
        /// <summary>
        /// 测试在foreach中的异步线程
        /// </summary>
        [Test]
        public async Task TestAwaitInForeach()
        {
            //  看输出结果是否正确，输出顺序是否按照遍历顺序来的

            static async Task action(int index)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                await TestContext.Out.WriteLineAsync("测试异步----" + index.ToString());
            }

            int[] items = [1, 2, 3, 4, 5];
            foreach (var item in items)
            {
                await action(item);
            }
        }

        /// <summary>
        /// 测试ConfigureAwait方法
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestTaskConfigureAwait()
        {
            RunContext context = RunContext.Current;
            context.Add<string>("Snail_", "1");

            await Task.Run(() =>
            {
                string? va = RunContext.Current.Get<string>("Snail_");
                RunContext.Current.Add<string>("Snail_", "2");

                RunContext.New();
                TestContext.Out.WriteLine(va ?? "未读取到值");
            }).ConfigureAwait(false);

            string? vals = RunContext.Current.Get<string>("Snail_");
        }
        #endregion
    }
}

using Timer = System.Timers.Timer;

namespace Snail.Utilities.Common.Utils
{
    /// <summary>
    /// 定时器助手类
    /// <para>1、封装<see cref="Timer"/></para>
    /// </summary>
    public static class TimerHelper
    {
        #region 公共方法
        /// <summary>
        /// 启动运行一个定时器
        /// </summary>
        /// <param name="interval">定时器间隔多少ms再次执行</param>
        /// <param name="action">定时执行的业务操作</param>
        /// <param name="runRightNow">是否立马执行一次<paramref name="action"/></param>
        /// <returns>构建的定时器对象</returns>
        public static Timer Start(double interval, Action action, bool runRightNow = true)
        {
            ThrowIfNull(action);
            //  初始化Timer对象
            Timer timer = new Timer(interval)
            {
                AutoReset = true,
                Enabled = true,
            };
            timer.Elapsed += (sender, args) => action();
            //  若需要立马执行一次，则先模拟掉一下
            if (runRightNow == true)
            {
                action();
            }
            return timer;
        }
        /// <summary>
        /// 启动运行一个定时器
        /// </summary>
        /// <param name="interval">定时器间隔多少ms再次执行</param>
        /// <param name="action">定时执行的业务操作</param>
        /// <param name="runRightNow">是否立马执行一次<paramref name="action"/></param>
        /// <returns>构建的定时器对象</returns>
        public static Timer Start(TimeSpan interval, Action action, bool runRightNow = true)
            => Start(interval.TotalMilliseconds, action, runRightNow);
        #endregion
    }
}

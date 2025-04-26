using System.Diagnostics;
using System.Timers;

namespace Snail.Common.Utils
{
    /// <summary>
    /// 内部的定时器；每个1s运行一次 <br />
    ///     1、提供一个固定的定时器实例，然后做事件分发 <br />
    ///     2、避免内置功能用到定时器时，定义大量定时器实例，反而浪费<br />
    /// </summary>
    internal sealed class InternalTimer
    {
        #region 属性变量
        /// <summary>
        /// 定时器运行时
        /// </summary>
        public static event Action? OnRun;
        #endregion

        #region 构造方法
        /// <summary>
        /// 静态构造方法
        /// </summary>
        static InternalTimer()
        {
            //  启动定时器做自动扫描，实现对象回收
            var timer = new System.Timers.Timer(TimeSpan.FromSeconds(1))
            {
                AutoReset = true,
                Enabled = true,
            };
            timer.Elapsed += (object? sender, ElapsedEventArgs e) =>
            {
                var events = OnRun?.GetInvocationList();
                if (events?.Any() != true)
                {
                    return;
                }
                //  遍历事件监听委托，做异常捕捉，避免影响其他事件监听者
                foreach (Action action in events)
                {
                    RunResult rt = Run(action);
                    if (rt.Exception != null)
                    {
                        string msg = $"{nameof(InternalTimer)}执行事件发生错误。Action:{action.ToString()};Exception：{rt.Exception.ToString()}";
                        Debug.WriteLine(msg);
                    }
                }
            };
        }
        #endregion
    }
}

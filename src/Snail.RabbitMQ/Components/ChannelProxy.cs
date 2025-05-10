using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Snail.Abstractions.Common.DataModels;
using Snail.Abstractions.Common.Interfaces;

namespace Snail.RabbitMQ.Components
{
    /// <summary>
    /// RabbitMQ信道代理器
    /// </summary>
    internal sealed class ChannelProxy : PoolObject<IChannel>, IPoolObject
    {
        #region 属性变量
        /// <summary>
        /// 事件：信道发生异常时；回调参数：事件标题和异常详细信息
        /// </summary>
        public event Action<string, string>? OnError;
        #endregion

        #region 构造方法
        /// <summary>
        /// 默认无参构造方法
        /// </summary>
        public ChannelProxy(IChannel channel) : base(channel)
        {
            channel.ChannelShutdownAsync += Channel_Shutdown;
            channel.CallbackExceptionAsync += Channel_CallbackException;
        }
        #endregion

        #region 继承方法
        /// <summary>
        /// 对象释放
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (IsDisposed == false)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    Object.ChannelShutdownAsync -= Channel_Shutdown;
                    Object.CallbackExceptionAsync -= Channel_CallbackException;
                }
                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                OnError = null;
            }
            //  执行基类回收
            base.Dispose(disposing);
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 信道回调错误
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task Channel_CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            OnError?.Invoke("Channel.CallbackException", $"{e}");
            await Task.Yield();
        }
        /// <summary>
        /// 信道关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task Channel_Shutdown(object sender, ShutdownEventArgs e)
        {
            OnError?.Invoke("Channel.Shutdown", $"{e}");
            await Task.Yield();
        }
        #endregion
    }
}

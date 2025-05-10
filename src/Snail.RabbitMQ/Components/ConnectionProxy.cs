using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Snail.Abstractions.Common.DataModels;
using Snail.Abstractions.Common.Interfaces;
using Snail.Common.Components;
using Snail.Utilities.Common.Extensions;

namespace Snail.RabbitMQ.Components
{
    /// <summary>
    /// RabbitMQ链接代理类
    /// </summary>
    internal class ConnectionProxy : PoolObject<IConnection>, IPoolObject
    {
        #region 属性变量
        /// <summary>
        /// 信道池：空闲超过10s，自动回收
        /// </summary>
        private readonly ObjectAsyncPool<ChannelProxy> _channelPool = new ObjectAsyncPool<ChannelProxy>(TimeSpan.FromSeconds(10));

        /// <summary>
        /// 事件：连接异常时触发；回调参数：事件标题和异常详细信息
        /// </summary>
        public event Action<string, string>? OnError;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        public ConnectionProxy(IConnection connection) : base(connection)
        {
            connection = ThrowIfNull(connection);
            connection.ConnectionShutdownAsync += Connection_Shutdown;
            connection.CallbackExceptionAsync += Connection_CallbackException;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 获取信道
        /// </summary>
        /// <returns></returns>
        public async Task<ChannelProxy?> GetChannel()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            //  进行异常拦截；判定是否是已经超过最大信道数了
            try
            {
                IdleTime = default;
                ChannelProxy proxy = await _channelPool.GetOrAdd(async () =>
                {
                    IChannel channel = await Object.CreateChannelAsync();
                    return new ChannelProxy(channel);
                });
                return proxy;
            }
            catch (Exception ex)
            {
                //  发送异常时，当前连接不可用，更新空闲时间，方便下次回收
                IdleTime = DateTime.UtcNow;
                //  分配信道异常，忽略掉；其他异常抛出
                if (ex is ChannelAllocationException)
                {
                    return null;
                }
                throw;
            }
        }
        #endregion

        #region IPoolObject
        /// <summary>
        /// 闲置时间 <br />
        ///     1、从什么时候开始处理闲置状态；超过配置的闲置时间则自动回收<br />
        /// </summary>
        DateTime IPoolObject.IdleTime
        {
            set => IdleTime = value;
            get
            {
                //  若链接的信道池为空，则强制为【空闲状态】
                if (_channelPool.Count == 0 && IdleTime == default)
                {
                    IdleTime = DateTime.UtcNow;
                }
                return IdleTime;
            }
        }
        #endregion

        #region 继承方法
        /// <summary>
        /// 销毁对象
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (IsDisposed == false)
            {
                if (disposing == true)
                {
                    // TODO: 释放托管状态(托管对象)
                    //  信道池同步销毁
                    _channelPool.TryDispose();
                    //  链接对象销毁
                    Object.ConnectionShutdownAsync -= Connection_Shutdown;
                    Object.CallbackExceptionAsync -= Connection_CallbackException;
                    Object.Dispose();
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
        /// 链接回调异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task Connection_CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            await Task.Yield();
            OnError?.Invoke("Connection.CallbackException", $"{e}");
        }
        /// <summary>
        /// 链接关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task Connection_Shutdown(object sender, ShutdownEventArgs e)
        {
            await Task.Yield();
            OnError?.Invoke("Connection.Shutdown", $"e");
        }
        #endregion
    }
}

using Snail.RabbitMQ.Exceptions;
using Snail.Utilities.Collections.Extensions;

namespace Snail.RabbitMQ.Components
{
    /// <summary>
    /// 消息接收器代理
    /// </summary>
    internal sealed class ReceiverProxy<T>
    {
        #region 属性变量
        /// <summary>
        /// 重视次数
        /// </summary>
        private readonly int _attempt;
        /// <summary>
        /// 接收器
        /// </summary>
        private readonly Func<T, Task<bool>> _receiver;
        /// <summary>
        /// 计数器
        /// </summary>
        private int _counter = 0;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        public ReceiverProxy(int attempt, Func<T, Task<bool>> receiver)
        {
            _attempt = attempt;
            _receiver = ThrowIfNull(receiver);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> OnReceive(T message)
        {
            Exception? tmpEx = null;
            bool isSuccess = false;
            //  接收消息处理，拦截异常
            try
            {
                isSuccess = await _receiver.Invoke(message);
            }
            catch (Exception ex)
            {
                tmpEx = ex.Optimize();
            }
            //  进行尝试次数判断
            if (isSuccess != true)
            {
                _counter += 1;
                //  超过重视次数，强制清除
                if (_counter >= _attempt)
                {
                    tmpEx = tmpEx != null
                        ? new ForceAskException($"消息已经连续{_counter}次处理失败，发生异常", tmpEx)
                       : new ForceAskException($"消息已经连续{_counter}次处理失败，接收方返回false；");
                    _counter = 0;
                }
                if (tmpEx != null)
                {
                    throw tmpEx;
                }
            }
            else
            {
                _counter = 0;
            }

            return isSuccess;
        }
        #endregion
    }
}

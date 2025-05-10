using Snail.Abstractions.Message.Interfaces;

namespace Snail.Abstractions.Message.Attributes
{
    /// <summary>
    /// 特性标签：【队列消息】消息接收器；针对特定属性做默认值处理<br />
    ///     1、基于消息名称，自动创建队列名称，初始化交换机等信息<br />
    ///     2、<see cref="ReceiverAttribute"/>简化版处理，减少配置量
    /// </summary>
    /// <remarks>默认处理：<br />
    ///     1、交换机=空、路由=<see cref="_message"/>、队列名=<see cref="_message"/><br />
    ///     2、重视次数=3；并发=1<br />
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class MQReceiverAttribute : Attribute, IReceiveOptions
    {
        #region 属性变量
        /// <summary>
        /// 接收消息名称
        /// </summary>
        private string _message { init; get; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="message">接收消息名称</param>
        public MQReceiverAttribute(string message)
        {
            _message = ThrowIfNullOrEmpty(message);
        }
        #endregion

        #region IReceiverOptions
        /// <summary>
        /// 消息交换机名称<br />
        /// </summary>
        string? IMessageOptions.Exchange => null;

        /// <summary>
        /// 消息交换机和队列之间的路由<br />
        /// </summary>
        string? IMessageOptions.Routing => _message;

        /// <summary>
        /// 消息队列名称
        /// </summary>
        string IReceiveOptions.Queue => _message;

        /// <summary>
        /// 接收消息的尝试次数 <br />
        ///     1、接收方发生异常后，尝试多少次后仍失败，则强制确认，避免消息堆积 <br />
        ///     2、== 0 失败则自动确认消费 <br />
        ///     3、&lt;0 不自动确认 <br />
        /// </summary>
        public int Attempt { init; get; } = 3;

        /// <summary>
        /// 消息接收器并发数量 <br />
        ///     1、当前接收器从<see cref="IReceiveOptions.Queue"/>接收消息的并发量 <br />
        ///     2、大于1时生效，合理设置，提高消息消费效率
        /// </summary>
        public int Concurrent { init; get; } = 1;
        #endregion
    }
}

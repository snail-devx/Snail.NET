using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;


namespace Snail.Abstractions.Message.Attributes
{
    /// <summary>
    /// 特性标签：消息接收器<br />
    ///     1、接收什么消息、消息重视、并发等配置<br />
    /// </summary>
    /// <remarks>简化版接收器参照：<see cref="MQReceiverAttribute"/>和<see cref="PubSubReceiverAttribute"/></remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class ReceiverAttribute : Attribute, IReceiveOptions
    {
        #region 属性变量
        /// <summary>
        /// 消息类型
        /// </summary>
        public required MessageType Type { init; get; }
        #endregion

        #region IReceiverOptions
        /// <summary>
        /// 消息队列名称
        /// </summary>
        public required string Queue { init; get; }

        /// <summary>
        /// 消息交换机名称
        /// </summary>
        public string? Exchange { init; get; }

        /// <summary>
        /// 消息交换机和队列之间的路由
        /// </summary>
        public string? Routing { init; get; }

        /// <summary>
        /// 接收消息的尝试次数 <br />
        ///     1、接收方发生异常后，尝试多少次后仍失败，则强制确认，避免消息堆积 <br />
        ///     2、== 0 失败则自动确认消费 <br />
        ///     3、&lt;0 不自动确认 <br />
        /// </summary>
        public int Attempt { init; get; }

        /// <summary>
        /// 消息接收器并发数量 <br />
        ///     1、当前接收器从<see cref="Queue"/>接收消息的并发量 <br />
        ///     2、大于1时生效，合理设置，提高消息消费效率
        /// </summary>
        public int Concurrent { init; get; }
        #endregion
    }
}

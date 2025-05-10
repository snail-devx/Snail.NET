using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Identity.Interfaces;
using Snail.Abstractions.Logging.DataModels;

namespace Snail.Message.DataModels
{
    /// <summary>
    /// 消息发送日志描述器
    /// </summary>
    public sealed class MessageSendLogDescriptor : LogDescriptor, IIdentity
    {
        #region 属性变量
        /// <summary>
        /// 日志操作Id值
        /// </summary>
        public required string Id { get; init; }
        /// <summary>
        /// 消息发送方服务器
        /// </summary>
        public required string ServerOptions { get; init; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="isForce"></param>
        public MessageSendLogDescriptor(bool isForce) : base(isForce) { }
        #endregion
    }

    /// <summary>
    /// 消息接收日志描述器
    /// </summary>
    public sealed class MessageReceiveLogDescriptor : LogDescriptor, IPerformance
    {
        /// <summary>
        /// 接收的消息来自哪里
        /// </summary>
        public required string ServerOptions { get; init; }

        /// <summary>
        /// 请求耗时毫秒
        /// </summary>
        public long? Performance { set; get; }
    }
}

using Snail.Abstractions.Message.Interfaces;

namespace Snail.Abstractions.Message.DataModels
{
    /// <summary>
    /// 消息配置选项
    /// </summary>
    public class MessageOptions : IMessageOptions
    {
        /// <summary>
        /// 消息交换机名称
        /// </summary>
        public string? Exchange { init; get; }

        /// <summary>
        /// 消息交换机和队列之间的路由
        /// </summary>
        public string? Routing { init; get; }
    }
}

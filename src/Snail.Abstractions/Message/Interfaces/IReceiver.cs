using Snail.Abstractions.Message.DataModels;

namespace Snail.Abstractions.Message.Interfaces
{
    /// <summary>
    /// 接口约束：消息接收器
    /// </summary>
    public interface IReceiver
    {
        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="message">接收/发送的消息数据</param>
        /// <returns>处理使用成功，成功返回true，否则返回false</returns>
        Task<bool> OnReceive(MessageData message);
    }
}

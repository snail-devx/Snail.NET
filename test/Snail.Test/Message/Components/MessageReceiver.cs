using Snail.Abstractions.Message.Attributes;
using Snail.Abstractions.Message.DataModels;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;


namespace Snail.Test.Message.Components
{
    /// <summary>
    /// 测试消息接收器
    /// </summary>
    [Receiver(Type = MessageType.MQ, Queue = "1111")]
    [MQReceiver(message: "TestMQ")]
    [PubSubReceiver(name: "TestNet9", message: "TestPubSub")]
    [PubSubReceiver(name: "TestNet9", message: "Snail.TestPubSub.Message")]
    [Server(Workspace = "Test", Code = "Test")]
    public sealed class MessageReceiver : IReceiver
    {
        #region 属性变量
        #endregion

        #region 构造方法
        /// <summary>
        /// 默认无参构造方法
        /// </summary>
        public MessageReceiver()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="message">接收/发送的消息数据</param>
        /// <returns>处理使用成功，成功返回true，否则返回false</returns>
        async Task<bool> IReceiver.OnReceive(MessageDescriptor message)
        {
            //throw new NotImplementedException();
            await Task.Yield();
            return true;
        }
        #endregion
    }
}

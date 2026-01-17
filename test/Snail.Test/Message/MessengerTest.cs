using Snail.Abstractions.Message;
using Snail.Abstractions.Message.Attributes;
using Snail.Abstractions.Message.DataModels;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Extensions;
using Snail.Abstractions.Message.Interfaces;
using Snail.Utilities.Common.Extensions;
using System.Diagnostics;

namespace Snail.Test.Message
{
    /// <summary>
    /// 消息接收器测试
    /// </summary>
    public sealed class MessengerTest
    {
        #region 公共方法
        /// <summary>
        /// 测试消息接收器注册
        /// </summary>
        [Test]
        public void Test()
        {
            IApplication app = new Application();
            app.AddMessageService();
            app.Run();

            MQReceiverAttribute re = new MQReceiverAttribute("xxx") { Attempt = 1, Concurrent = 10 };
            string json = re.AsJson();
            json = ((IReceiveOptions)re).AsJson();
            string testS = $"{MessageType.MQ}";


        }

        /// <summary>
        /// 测试发送消息
        /// </summary>
        [Test]
        public async Task TestSendMessage()
        {
            IApplication app = new Application();
            app.AddMessageService();
            app.Run();

            Stopwatch sw = new Stopwatch();
            IMessenger messenger = app.ResolveRequired<MessengerProxy>().Messenger;
            //  单线程测试
            sw.Restart();
            await TestSendMessage(messenger, -1);
            sw.Stop();
            TestContext.Out.WriteLine($"单线程测试，耗时：{sw.ElapsedMilliseconds}");
            //  单线程循环
            sw.Restart();
            for (int index = 0; index < 1000; index++)
            {
                await TestSendMessage(messenger, index);
            }
            sw.Stop();
            TestContext.Out.WriteLine($"单线程For循环，耗时：{sw.ElapsedMilliseconds}");
            //  多线程测试
            sw.Restart();
            await Parallel.ForAsync(0, 100, async (index, token) =>
            {
                await TestSendMessage(messenger, 10000 + index);

            });
            sw.Stop();
            TestContext.Out.WriteLine($"多线程测试，耗时：{sw.ElapsedMilliseconds}");
        }

        /// <summary>
        /// 测试接收消息
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestReceiveMessage()
        {
            IApplication app = new Application();
            app.AddMessageService();
            app.Run();

            Stopwatch sw = new Stopwatch();
            IMessenger messenger = app.ResolveRequired<MessengerProxy>().Messenger;
            //  测试接收消息；仅做调试使用，看是否报错
            bool hasReceiveMQ = false, hasReceivePubSub = false;
            //      MQ消息
            IReceiveOptions options = new ReceiveOptions()
            {
                Exchange = null,
                Queue = "Snail.TestMQ.Message",
                Routing = "Snail.TestMQ.Message"
            };
            await messenger.Receive(MessageType.MQ, async message =>
            {
                await Task.Yield();
                hasReceiveMQ = true;
                return true;
            }, options);
            //      PubSub消息
            options = new ReceiveOptions()
            {
                Exchange = "Snail.TestPubSub.Message",
                Queue = "Snail.TestPubSub.Message",
                Routing = null,
            };
            await messenger.Receive(MessageType.PubSub, async message =>
            {
                await Task.Yield();
                hasReceivePubSub = true;
                return true;
            }, options);
            //  发送一个消息
            await TestSendMessage(messenger, -100);
            //  等待一会儿，让接收器能够接收到消息
            await Task.Delay(TimeSpan.FromSeconds(5));
            Assert.That(hasReceiveMQ, "已经接收到了MQ消息");
            Assert.That(hasReceivePubSub, "已经接收到了PubSub消息");
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 测试发送消息
        /// </summary>
        /// <returns></returns>
        private static async Task TestSendMessage(IMessenger messenger, int index)
        {
            //  发送mq消息：发送到默认交换机，路由非null
            MessageDescriptor message = new MessageDescriptor()
            {
                Name = "Snail.TestMQ.Message",
                Data = $"{index}:{Guid.NewGuid()}".AsJson(),
            };
            await messenger.Send(MessageType.MQ, message, new SendOptions()
            {
                Exchange = null,
                Routing = "Snail.TestMQ.Message"
            });
            //  发送pubsub消息：发送到具名交换机，路由为空
            message = new MessageDescriptor()
            {
                Name = "Snail.TestPubSub.Message",
                Data = Guid.NewGuid().AsJson()
            };
            await messenger.Send(MessageType.PubSub, message, new SendOptions()
            {
                Exchange = "Snail.TestPubSub.Message",
                Routing = null,
                Compress = index % 2 == 0
            });
        }
        #endregion

        #region 内部类型
        /// <summary>
        /// 
        /// </summary>
        [Component]
        private class MessengerProxy
        {
            [Messenger, Server(Workspace = "Test", Code = "Test")]
            public required IMessenger Messenger { init; get; }
        }
        #endregion
    }
}

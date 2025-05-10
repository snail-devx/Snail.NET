﻿using System.Diagnostics;
using Snail.Abstractions.Dependency.DataModels;
using Snail.Abstractions.Message.Attributes;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;
using Snail.Abstractions.Web.Attributes;
using Snail.Utilities.Common.Extensions;

namespace Snail.Abstractions.Message.Extensions
{
    /// <summary>
    /// 针对<see cref="IApplication"/>的扩展方法
    /// </summary>
    public static class ApplicationExtensions
    {
        #region 公共方法
        /// <summary>
        /// 添加消息服务
        /// </summary>
        /// <param name="app"></param>
        /// <param name="useLogging">是否启用【日志】中间件</param>
        /// <param name="useShareKeyChain">是否启用【共享钥匙串】中间件</param>
        /// <param name="useRunContext">是否启用【运行时上下文】中间件</param>
        /// <returns></returns>
        public static IApplication AddMessageService(this IApplication app, bool useLogging = true, bool useShareKeyChain = true, bool useRunContext = true)
        {
            //  程序集扫描时，进行消息接收器扫描，整理出需要接收哪些消息  
            IList<ReceiverTypeDescriptor> descriptors = new List<ReceiverTypeDescriptor>();
            app.OnScan += (Type type, ReadOnlySpan<Attribute> attributes) =>
            {
                //  实现接口的类型才做处理：IReceiver
                if (type.IsAssignableTo(typeof(IReceiver)) == false)
                {
                    return;
                }
                //  遍历特性标签：约束必须得有消息服务器地址配置
                ServerAttribute? server = null;
                IList<ReceiverDescriptor> receivers = new List<ReceiverDescriptor>();
                foreach (Attribute attr in attributes)
                {
                    switch (attr)
                    {
                        //  消息服务器
                        case ServerAttribute:
                            server = attr as ServerAttribute;
                            break;
                        //  通用消息接收配置
                        case ReceiverAttribute receiver:
                            receivers.Add(new ReceiverDescriptor(receiver.Type, receiver));
                            break;
                        //  MQ消息接收配置
                        case MQReceiverAttribute mq:
                            receivers.Add(new ReceiverDescriptor(MessageType.MQ, mq));
                            break;
                        //  发布订阅消息接收
                        case PubSubReceiverAttribute pubsub:
                            receivers.Add(new ReceiverDescriptor(MessageType.PubSub, pubsub));
                            break;
                    }
                }
                if (server == null)
                {
                    string msg = $"请使用[MessageServerAttribute]标签配置消息服务器，type：{type.FullName}";
                    throw new ApplicationException(msg);
                }
                //  梳理出有效值，加入注册集合中
                if (receivers.Count > 0)
                {
                    ReceiverTypeDescriptor descriptor = new ReceiverTypeDescriptor(type, Guid.NewGuid().ToString(), server, receivers);
                    descriptors.Add(descriptor);
                }
            };
            //  服务注册时：进行服务注册
            app.OnRegister += () =>
            {
                //  先进行消息中间件配置
                IMessageManager manager = app.ResolveRequired<IMessageManager>();
                if (useLogging == true)
                {
                    IMessageMiddleware middleware = app.ResolveRequired<IMessageMiddleware>(key: MIDDLEWARE_Logging);
                    manager.Use(name: MIDDLEWARE_Logging, middleware);
                }
                if (useShareKeyChain == true)
                {
                    IMessageMiddleware middleware = app.ResolveRequired<IMessageMiddleware>(key: MIDDLEWARE_ShareKeyChain);
                    manager.Use(name: MIDDLEWARE_ShareKeyChain, middleware);
                }
                if (useRunContext == true)
                {
                    IMessageMiddleware middleware = app.ResolveRequired<IMessageMiddleware>(key: MIDDLEWARE_RunContext);
                    manager.Use(name: MIDDLEWARE_RunContext, middleware);
                }
                //  遍历消息接收器，注册依赖
                IList<DIDescriptor> dis = descriptors
                    .Select(descriptor => new DIDescriptor(descriptor.Guid, from: typeof(IReceiver), LifetimeType.Transient, descriptor.Type))
                    .ToList();
                app.DI.Register(dis);
            };
            //  运行时，启动消息接收
            app.OnRun += () =>
            {
                //  遍历消息接收信息，动态构建消息接收器
                foreach (var descriptor in descriptors)
                {
                    IReceiver receiver = app.ResolveRequired<IReceiver>(key: descriptor.Guid);
                    IMessenger? messenger = app.DI.Resolve(key: null, from: typeof(IMessenger), [descriptor.Server]) as IMessenger;
                    ThrowIfNull(messenger, $"{nameof(IMessenger)}实例构建失败");
#if DEBUG
                    Debug.WriteLine($"接收消息。接收器：{receiver.GetType().FullName}；服务器：{descriptor.Server.AsJson()}；消息信息：{descriptor.Receivers.AsJson()}");
#endif
                    foreach (var item in descriptor.Receivers)
                    {
                        Task<bool> task = messenger!.Receive(item.MessageType, item.Options, receiver.OnReceive);
                        task.Wait();
                    }
                    //  用完以后，从依赖注入中移除掉，不再管理
                    app.DI.Unregister(key: descriptor.Guid, from: typeof(IReceiver));
                }
            };

            return app;
        }
        #endregion

        #region 私有类型
        /// <summary>
        /// 消息接收器描述信息
        /// </summary>
        /// <param name="MessageType">消息类型</param>
        /// <param name="Options">消息配置选项</param>
        private record ReceiverDescriptor(MessageType MessageType, IReceiveOptions Options);
        /// <summary>
        /// 消息接收器类型描述器
        /// </summary>
        /// <param name="Type">接收器类型</param>
        /// <param name="Guid">针对接收器生成的唯一标记</param>
        /// <param name="Server">接收消息的消息服务器</param>
        /// <param name="Receivers">消息接收器信息</param>
        private record ReceiverTypeDescriptor(Type Type, string Guid, ServerAttribute Server, IList<ReceiverDescriptor> Receivers);
        #endregion
    }
}

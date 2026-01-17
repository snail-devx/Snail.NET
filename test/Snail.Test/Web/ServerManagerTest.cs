using Snail.Abstractions.Web;
using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Extensions;
using Snail.Web;

namespace Snail.Test.Web
{
    /// <summary>
    /// 服务器 管理器测试
    /// </summary>
    public sealed class ServerManagerTest : UnitTestApp
    {
        [Test]
        public void Test()
        {
            //  不存在的资源编码
            IServerManager server = new ServerManager(App, "xxxx");
            ServerDescriptor? descriptor = server.GetServer(workspace: "Test", type: null, code: "BAIDU");
            Assert.That(descriptor == null, "BAIDU服务器信息为null才对");
            server.RegisterServer(new ServerDescriptor(workspace: "Test", type: null, "BAIDU", "https://www.baidu.com"));
            descriptor = server.GetServer(workspace: "Test", type: null, code: "BAIDU");
            Assert.That(descriptor != null, "BAIDU服务器信息不为null才对");
            Assert.That(descriptor!.Server == "https://www.baidu.com", "BAIDU服务器地址为：https://www.baidu.com");
            //      重复注册采用最后注册的为准
            server.RegisterServer(new ServerDescriptor(workspace: "Test", type: null, "BAIDU", "https://www.baidu.com2222"));
            descriptor = server.GetServer(workspace: "Test", type: null, code: "BAIDU");
            Assert.That(descriptor!.Server == "https://www.baidu.com2222", "BAIDU服务器地址为：https://www.baidu.com2222");

            //  搞一个存在的管理器
            server = new ServerManager(App, "server");
            descriptor = server.GetServer(workspace: "Test", type: null, code: "BAIDU");
            Assert.That(descriptor != null, "BAIDU服务器信息不为null才对");
            Assert.That(descriptor!.Server == "https://www.baidu.com", "BAIDU服务器地址为：https://www.baidu.com");
            //      重复注册采用最后注册的为准
            server.RegisterServer(new ServerDescriptor(workspace: "Test", type: null, "BAIDU", "https://www.baidu.com2222ddd"));
            descriptor = server.GetServer(workspace: "Test", type: null, code: "BAIDU");
            Assert.That(descriptor!.Server == "https://www.baidu.com2222ddd", "BAIDU服务器地址为：https://www.baidu.com2222ddd");
        }
    }
}

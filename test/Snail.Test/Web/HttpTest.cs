using Snail.Abstractions.Web;
using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Extensions;
using Snail.Abstractions.Web.Interfaces;
using Snail.Web;
using Snail.Web.Components;

namespace Snail.Test.Web
{
    /// <summary>
    /// HTTP相关测试;<see cref="IHttpManager"/>、<see cref="IHttpRequestor"/>
    /// </summary>
    public sealed class HttpTest
    {
        #region 属性变量
        public readonly IApplication App;
        #endregion

        #region 构造方法
        public HttpTest()
        {
            App = new Application();
            App.AddHttpService();
            App.Run();
        }
        #endregion

        #region 公共方法
        [Test]
        public async Task Test()
        {
            IHttpManager http = App.ResolveRequired<IHttpManager>();
            //  常规测试
            //      服务器地址
            Assert.That(http.GetServer(workspace: "Test", code: "BAIDU") != null, "BAIDU服务器地址不能为空");
            IServerOptions server = new ServerOptions(workspace: "Test", code: "BAIDU");
            //      http请求
            IHttpRequestor requestor = new HttpRequestor(App, server, provider: null);
            HttpResult hr = await requestor.Get("/s?wd=xx");
            //      中间件：直接使用的 MiddlewareProxy<> 实现，不用测试

            //  测试IHttpRequestor依赖注入构建效果
            var proxy = App.ResolveRequired<HttpRequestorProxy>();
            Assert.That(proxy.Requestor != null, "Requestor不应该为null");
            Assert.That(proxy.Requestor2 != null, "Requestor2不应该为null");
            Assert.That(proxy.Requestor2 != proxy.Requestor, "Requestor2!=Requestor");
            requestor = proxy.Requestor!;
            hr = await requestor.Get("/s?wd=xx");

            await Parallel.ForAsync(0, 100, async (index, token) =>
            {
                await requestor.Get("/s?wd=" + index);
            });
        }
        /// <summary>
        /// 测试回收逻辑
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestAutoRecycle()
        {
            IHttpManager manager = App.ResolveRequired<IHttpManager>();
            manager.RegisterServer(new ServerDescriptor("Test", "BAIDU", "https://www.baidu.com"));
            //  发送HTTP请求
            IHttpProvider http = new HttpProvider(manager);
            await http.Send(new HttpRequestMessage(HttpMethod.Get, "s?wd=111"), new ServerOptions("Test", "BAIDU"));
            //  后续测试的时候，将 HttpProvider 中将闲置时间改为 5s；用于测试复用情况
            //await Task.Delay(6000);
            //await http.Send(new HttpRequestMessage(HttpMethod.Get, "s?wd=111"), new ServerOptions("Test", "BAIDU"));

        }
        #endregion

        #region 私有类型
        [Component]
        private class HttpRequestorProxy
        {
#pragma warning disable CS8618 // 测试属性注入，不用管null的情况
            /// <summary>
            /// 测试指定服务器构建
            /// </summary>
            [HttpRequestor, Server(Workspace = "Test", Code = "BAIDU")]
            public IHttpRequestor Requestor { private init; get; }
            /// <summary>
            /// 和<see cref="Requestor"/>同服务器配置，测试瞬时生命周期
            /// </summary>

            [HttpRequestor, Server(Workspace = "Test", Code = "BAIDU")]
            public IHttpRequestor Requestor2 { private init; get; }
#pragma warning disable CS8618
        }
        #endregion
    }
}

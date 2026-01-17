using Snail.Abstractions.ErrorCode;
using Snail.Abstractions.ErrorCode.Extensions;
using Snail.Abstractions.ErrorCode.Interfaces;

namespace Snail.Test.ErrorCode
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ErrorCodeManagerTest
    {
        [Test]
        public void Test()
        {
            IApplication app = new Application();
            app.Run();
            IErrorCodeManager manager = app.ResolveRequired<IErrorCodeManager>();

            IErrorCode? error = manager.Get("0");
            Assert.That(error != null && error.Code == "0" && error.Message == "成功", "默认中文环境");
            error = manager.Get("-2");
            Assert.That(error != null && error.Code == "-2" && error.Message == "令牌无效", "默认中文环境");

            error = manager.Get(culture: "en-US", code: "0");
            Assert.That(error != null && error.Code == "0" && error.Message == "Success", "en-US环境");
            error = manager.Get(culture: "en-US", code: "-2");
            Assert.That(error != null && error.Code == "-2" && error.Message == "Token Invalid", "en-US环境");

            error = manager.Get(culture: "en-US", code: "-3");
            Assert.That(error != null && error.Code == "-3" && error.Message == "令牌无效xxx", "en-US环境没有，从默认环境查找");
        }
    }
}

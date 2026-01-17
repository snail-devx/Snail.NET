using Snail.Aspect.Web.Attributes;
using Snail.Aspect.Web.Enumerations;

namespace Snail.Test.Aspect.Components
{
    [HttpAspect(Workspace = "Test", Code = "BAIDU", Analyzer = Cons.Analyzer)]
    abstract class HttpAspectTest
    {
        [HttpMethod(Method = HttpMethodType.Get, Url = "12312")]
        public abstract Task TestVoid(string url = "", string url1 = "22");
        [HttpMethod(Url = "12312")]
        public abstract Task<string> TestString(string url1 = "");

        [HttpMethod(Method = HttpMethodType.Post, Url = "12312")]
        public abstract Task<string> TestClass(string url, string url2, [HttpBody] IDictionary<string, object> postData);

        public void Test()
        {
        }
    }
}

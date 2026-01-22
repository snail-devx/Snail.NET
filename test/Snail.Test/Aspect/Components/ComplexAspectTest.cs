using Snail.Aspect.Distribution.Attributes;
using Snail.Aspect.Distribution.Enumerations;
using Snail.Aspect.Web.Attributes;
using Snail.Aspect.Web.Enumerations;
using Snail.Test.Aspect.DataModels;

namespace Snail.Test.Aspect.Components
{
    /// <summary>
    /// 复杂切面测试；同时多个切面如http、Cache、lock、、、
    /// </summary>
    [CacheAspect(Workspace = "Test", Code = "Default")]
    [HttpAspect(Workspace = "Test", Code = "BAIDU", Analyzer = Cons.Analyzer)]
    [LockAspect(Workspace = "Test", Code = "Default")]
    [MethodAspect(Interceptor = "")]
    [ValidateAspect]
    abstract class ComplexAspectTest
    {
        [LockMethod(Key = "111", Value = "111")]
        [CacheMethod(Action = CacheActionType.Delete, DataType = typeof(TestCache))]
        [HttpMethod(Method = HttpMethodType.Post, Url = "/dddd")]
        public abstract Task TestVoid([CacheKey] List<string> keys, [HttpBody] Dictionary<string, object> map);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/dddd")]
        public abstract Task<string> TestString([CacheKey] List<string> keys, [HttpBody, Required] Dictionary<string, object> map);
    }
}

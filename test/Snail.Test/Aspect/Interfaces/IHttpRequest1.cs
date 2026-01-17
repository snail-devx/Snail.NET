using Snail.Abstractions.Web.DataModels;
using Snail.Aspect.Web.Attributes;
using Snail.Utilities.Collections;
using static Snail.Aspect.Web.Enumerations.HttpMethodType;

namespace Snail.Test.Aspect.Interfaces
{
    using Snail.Abstractions.Dependency.Enumerations;
    using Snail.Aspect.Distribution.Attributes;
    using Snail.Aspect.Distribution.Enumerations;
    using Snail.Aspect.Web.Enumerations;
    using Snail.Test.Aspect.Attributes;
    using Snail.Test.Aspect.DataModels;
    using Snail.Utilities.Common;

    /// <summary>
    /// 
    /// </summary>
    //[AspectTest(Type = typeof(Xx.Xxx))]
    [AspectTest<XC<LockList<Disposable[]>>>]
    //[HttpAspect(Workspace = "Test", Code = "BAIDU", Analyzer = "TestAnalyzer")]
    //[HttpAspect(Workspace = "Test", Code = Xx.Xxx.Code, Analyzer = "TestAnalyzer")]
    //[HttpAspect(Workspace = "Test", Code = XC<Disposable>.XCC.Code, Analyzer = "TestAnalyzer")]
    [HttpAspect(Workspace = "Test", Code = "BAIDU", Analyzer = Cons.Analyzer)]
    //[HttpAspect(Workspace = "Test", Code = nameof(IHttpRequest1), Analyzer = Cons.Analyzer)]
    //[CacheAspect(Workspace = "Test", Code = "Default", Runner = "dd")]
    public interface IHttpRequest1
    {
        //[HttpMethod(Url = "1")]
        //Task Test(string str, Xx.Xxx x1);

        #region 缓存-Load、Save、Delete测试
        //  load
        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Load, Type = CacheType.ObjectCache, DataType = typeof(TestCache))]
        public Task TestLoadCacheVoid([CacheKey] string x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Load, Type = CacheType.ObjectCache, DataType = typeof(TestCache))]
        public Task TestLoadCacheVoid2([CacheKey] IList<string> x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Load, Type = CacheType.HashCache, DataType = typeof(TestCache), MasterKey = "111")]
        public Task TestLoadCacheVoid3([CacheKey] string x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Load, Type = CacheType.HashCache, DataType = typeof(TestCache), MasterKey = "111")]
        public Task TestLoadCacheVoid4([CacheKey] IList<string> x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Load, Type = CacheType.ObjectCache, DataType = typeof(TestCache))]
        public Task<TestCache> TestLoadCache([CacheKey] string x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Load, Type = CacheType.ObjectCache, DataType = typeof(TestCache))]
        public Task<IList<TestCache>> TestLoadCache2([CacheKey] IList<string> x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Load, Type = CacheType.HashCache, DataType = typeof(TestCache), MasterKey = "111")]
        public Task<IList<TestCache>> TestLoadCache3([CacheKey] string x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Load, Type = CacheType.HashCache, DataType = typeof(TestCache), MasterKey = "111")]
        public Task<IList<TestCache>> TestLoadCache4([CacheKey] IList<string> x);

        //  save相关：必须得有返回值，不然无保存数据，后续再支持直接传入saveData，这样可以同步保存传入数据、保存
        /**
        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Save, Type = CacheType.ObjectCache, DataType = typeof(TestCache))]
        public Task TestSaveCacheVoid([CacheKey] string x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Save, Type = CacheType.ObjectCache, DataType = typeof(TestCache))]
        public Task TestSaveCacheVoid2([CacheKey] IList<string> x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Save, Type = CacheType.HashCache, DataType = typeof(TestCache), MasterKey = "111")]
        public Task TestSaveCacheVoid3([CacheKey] string x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Save, Type = CacheType.HashCache, DataType = typeof(TestCache), MasterKey = "111")]
        public Task TestSaveCacheVoid4([CacheKey] IList<string> x);
        */

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Save, Type = CacheType.ObjectCache, DataType = typeof(TestCache))]
        public Task<TestCache> TestSaveCache([CacheKey] string x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Save, Type = CacheType.ObjectCache, DataType = typeof(TestCache))]
        public Task<IList<TestCache>> TestSaveCache2([CacheKey] IList<string> x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Save, Type = CacheType.HashCache, DataType = typeof(TestCache), MasterKey = "111")]
        public Task<IList<TestCache>> TestSaveCache3([CacheKey] string x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Save, Type = CacheType.HashCache, DataType = typeof(TestCache), MasterKey = "111")]
        public Task<IList<TestCache>> TestSaveCache4([CacheKey] IList<string> x);

        // Delete
        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Delete, Type = CacheType.ObjectCache, DataType = typeof(TestCache))]
        public Task TestDeleteCacheVoid([CacheKey] string x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Delete, Type = CacheType.ObjectCache, DataType = typeof(TestCache))]
        public Task TestDeleteCacheVoid2([CacheKey] IList<string> x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Delete, Type = CacheType.HashCache, DataType = typeof(TestCache), MasterKey = "111")]
        public Task TestDeleteCacheVoid3([CacheKey] string x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Delete, Type = CacheType.HashCache, DataType = typeof(TestCache), MasterKey = "111")]
        public Task TestDeleteCacheVoid4([CacheKey] IList<string> x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Delete, Type = CacheType.ObjectCache, DataType = typeof(TestCache))]
        public Task<string> TestDeleteCache([CacheKey] string x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Delete, Type = CacheType.ObjectCache, DataType = typeof(TestCache))]
        public Task<IList<string>> TestDeleteCache2([CacheKey] IList<string> x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Delete, Type = CacheType.HashCache, DataType = typeof(TestCache), MasterKey = "111")]
        public Task<IList<string>> TestDeleteCache3([CacheKey] string x);

        [HttpMethod(Method = HttpMethodType.Post, Url = "/TestPostCacheVoid")]
        [CacheMethod(Action = CacheActionType.Delete, Type = CacheType.HashCache, DataType = typeof(TestCache), MasterKey = "111")]
        public Task<IList<string>> TestDeleteCache4([CacheKey] IList<string> x);
        #endregion


        /// <summary>
        /// 测试Get方法
        /// </summary>
        [HttpMethod(Method = HttpMethodType.Post, Url = "/s?wd=xxxxx&{x}&userid={x}")]
        [HttpLog]
        public Task TestPostVoid(string x, LockList<string>? x2, [HttpBody, Inject] string xx1);
        /// <summary>
        /// 测试Get方法
        /// </summary>
        [HttpMethod(Method = HttpMethodType.Post, Url = "/s?wd=xxxxx&{x}&userid={x}")]
        Task TestPostVoid2(string x, LockList<string>? x2, string xx1 = "1111111111111111111");

        [HttpMethod(Method = Get, Url = "/s?wd=xxxxx")]
        Task TestVoidAsync();

        [HttpMethod(Method = Get, Url = "/s?wd=xxxxx")]
        Task<string> TestAsync([CacheKey] string key);

        [HttpMethod(Method = Get, Url = "/s?wd=xxxxx")]
        Task<HttpResult> TestAsync2();
        [HttpMethod(Method = Get, Url = "/s?wd=xxxxx")]
        Task<List<Disposable>> TestAsync3();
        [HttpMethod(Method = HttpMethodType.Get, Url = "/s?wd=xxxxx")]
        Task<List<XC<string>.XCC>> TestAsync4();

        //[HttpMethod(Method = HttpMethodType.Get, Url = "/TestGet")]
        //Task<List<T>> TestAsync2<T>();

        /// <summary>
        /// 此方法无实现方法，且未标记为http方法
        /// </summary>
        [HttpMethod(Method = Snail.Aspect.Web.Enumerations.HttpMethodType.Get, Url = "/TestGet")]
        abstract Task TestError();

        /// <summary>
        /// 测试默认实现
        /// </summary>
        virtual void TestDefaultImp()
        {
            void TestWri()
            {
                Console.WriteLine("11111111");

            }
            TestWri();
            TestWri();
        }

        virtual void TestArrow() => Console.WriteLine("111111111111111");

        static string? TestStatic()
        {
            return null;
        }


        #region 内部类型
        [Component(Lifetime = LifetimeType.Transient)]
        public class Xx
        {
            public const string Code = "BAIDU";

            public class Xxx
            {
                public const string Code = "BAIDU";
            }
        }
        #endregion
    }

    public class XC<T>
    {
        public const HttpMethodType Type = HttpMethodType.Get;

        public class XCC
        {
            public const string Code = "ddddd";
        }
    }

    public class XXXX
    {
        public virtual string Get()
        {
            return "";
        }
    }

    public class X21 : XXXX
    {

    }
}

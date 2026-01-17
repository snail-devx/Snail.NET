using Snail.Aspect.Web.Interfaces;
using Snail.Test.Aspect.Interfaces;
namespace Snail.Test.Aspect
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class HttpSourceGeneratorTest : UnitTestApp
    {
        #region 属性变量
        #endregion

        #region 构造方法
        /// <summary>
        /// 默认无参构造方法
        /// </summary>
        public HttpSourceGeneratorTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        #endregion

        #region 公共方法
        [Test]
        public async Task Test()
        {
            IHttpRequest1 request = App.ResolveRequired<IHttpRequest1>();
            await request.TestAsync("1");

            await request.TestPostVoid("1", null, "2");

            new Dictionary<string, string?>
            {
                { "", "1" }
            };
            string url = "";
            IHttpAnalyzer analyzer = null!;
            analyzer?.AnalysisUrl(url, new Dictionary<string, object?> { { "", "" } });
            await Task.Yield();
        }
        #endregion
    }
}

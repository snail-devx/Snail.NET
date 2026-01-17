using Snail.Abstractions.Identity.Interfaces;

namespace Snail.Test.Aspect.DataModels
{
    public class TestCacheBase : IIdentity
    {
        /// <summary>
        /// 数据主键Id值
        /// </summary>
        public required string Id { set; get; }
    }


    /// <summary>
    /// 
    /// </summary>
    public class TestCache : TestCacheBase
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string? Name { set; get; }
    }
}

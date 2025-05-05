using Snail.WebApp.Enumerations;

namespace Snail.WebApp.Attributes
{
    /// <summary>
    /// API接收内容特性标签；配合完成请求content-type过滤
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ContentAttribute : Attribute
    {
        /// <summary>
        /// 允许的content-type类型；支持多个“|”拼接
        /// </summary>
        public ContentType Allow { init; get; } = ContentType.Ignore;
    }
}

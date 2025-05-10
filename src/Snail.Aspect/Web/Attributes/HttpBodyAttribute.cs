using System;

namespace Snail.Aspect.Web.Attributes
{
    /// <summary>
    /// 特性标签：标记参数是HTTP请求的提交数据 <br />
    ///     1、一个方法中，只能有一个参数标记，多了报错
    ///     2、标记此属性的参数，会自动进行json序列化
    ///     3、配合<see cref="HttpMethodAttribute"/>使用 <br />
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class HttpBodyAttribute : Attribute
    {
    }
}

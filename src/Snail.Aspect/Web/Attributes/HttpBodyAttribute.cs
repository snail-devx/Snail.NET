using System;
using System.Net.Http;

namespace Snail.Aspect.Web.Attributes
{
    /// <summary>
    /// 特性标签：标记参数是HTTP请求的提交数据 <br />
    ///     1、配合<see cref="HttpMethodAttribute"/>使用 <br />
    ///     2、一个方法中，只能有一个参数标记，多了报错 <br />
    ///     3、标记此属性的参数，会自动将参数进行json序列化；除非标记到<see cref="HttpContent"/>类型参数上<br />
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class HttpBodyAttribute : Attribute
    {
    }
}

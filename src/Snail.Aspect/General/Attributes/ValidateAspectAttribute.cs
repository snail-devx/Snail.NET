using System;

namespace Snail.Aspect.General.Attributes
{
    /// <summary>
    /// 特性标签：标记此类有方法需要走验证逻辑 <br />
    ///     1、验证方法参数的有效性 <br />
    ///     2、验证字段属性的有效性 <br />
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class ValidateAspectAttribute : Attribute
    {
    }
}
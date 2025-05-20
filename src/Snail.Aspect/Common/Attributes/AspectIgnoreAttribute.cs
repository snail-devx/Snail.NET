using System;

namespace Snail.Aspect.Common.Attributes
{
    /// <summary>
    /// 特性标签：标记此类在代码分析时，忽略面向切面相关功能逻辑<br />
    ///     1、一般用到基类型中
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AspectIgnoreAttribute : Attribute
    {
    }
}

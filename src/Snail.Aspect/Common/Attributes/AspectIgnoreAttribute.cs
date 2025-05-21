using System;

namespace Snail.Aspect.Common.Attributes
{
    /// <summary>
    /// 特性标签：标记此类、接口、方法在代码分析时，忽略面向切面相关功能逻辑 <br />
    ///     1、用在类、接口上时，进行切面编程源码生成时，忽略此类型下所有方法的自动实现 <br />
    ///     2、用在方法上时，进行切面编程源码生成时，忽略此方法的自动实现 <br />
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class AspectIgnoreAttribute : Attribute
    {
    }
}

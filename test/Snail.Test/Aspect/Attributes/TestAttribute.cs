namespace Snail.Test.Aspect.Attributes
{
    /// <summary>
    /// 特性标签：切面编程测试，用于语法分析中做示例，方便语法分析测试不同情况
    /// </summary>
    public sealed class AspectTestAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public required Type Type { set; get; }
    }
    /// <summary>
    /// 特性标签：切面编程测试，用于语法分析中做示例，方便语法分析测试不同情况
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class AspectTestAttribute<T> : Attribute
    {

    }
}

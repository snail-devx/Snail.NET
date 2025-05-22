using System;

namespace Snail.Aspect.General.Attributes
{
    /// <summary>
    /// 特性标签：非null验证，有此标记的参数，自动生成验证代码 <br />
    ///     1、参数为null是，验证失败报错<br />
    /// </summary>

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class NotNullAttribute : Attribute
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="message">验证失败时的错误消息</param>
        public NotNullAttribute(string message = null)
        {
        }
    }
}

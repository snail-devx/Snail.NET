using System;

namespace Snail.Aspect.General.Attributes
{
    /// <summary>
    /// 特性标签：有任意值验证，有此标记的参数，自动生成验证代码 <br />
    ///     1、参数不为null且不是空值，验证失败抛出错误 <br />
    ///     2、空值条件：string为空串、array、dictionary、list为空集合 <br />
    ///     3、可验证参数数据类型string、array、dictionary、list <br />
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class HasAnyAttribute : Attribute
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="message">验证失败时的错误消息</param>
        public HasAnyAttribute(string message = null)
        {
        }
    }
}

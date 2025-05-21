using System;

namespace Snail.Aspect.Common.Attributes
{
    /// <summary>
    /// 特性标签：标记此类需要进行面向切面编程 <br />
    ///     1、有此标记的class、interface方法进行自动重写，拦截方法调用 <br />
    ///     2、结合<see cref="RunHandle"/>实现方法拦截，并调用此句柄 <br />
    ///     3、仅拦截实例方法（可override实例方法，接口方法），不拦截静态方法 <br />
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AspectAttribute : Attribute
    {
        /// <summary>
        /// 方法运行句柄 <br/>
        ///     1、必传；实现<see cref="Interfaces.IMethodRunHandle"/>的类型依赖注入Key值 <br/>
        /// </summary>
        public string RunHandle { get; set; }
    }
}


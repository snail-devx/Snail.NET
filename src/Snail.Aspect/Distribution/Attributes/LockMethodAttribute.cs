using System;

namespace Snail.Aspect.Distribution.Attributes
{
    /// <summary>
    /// 特性标签：并发锁方法，标记此方法中代码执行时进行并发锁控制 <br />
    ///     1、配合<see cref="LockAspectAttribute"/>使用，可指定并发的Key等信息
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class LockMethodAttribute : Attribute
    {
        /// <summary>
        /// 加锁的Key；确保唯一；<br />
        ///     1、支持从方法参数上进行动态key构建，如 "/api/x/{orgId}" 则orgId为方法参数名，自动取值做替换<br />
        ///     2、支持动态参数，从方法传入参数值动态构建<br />
        /// </summary>
        public string Key { set; get; }

        /// <summary>
        /// 锁的值<br />
        ///     1、在释放锁时使用；只有值正确才能被释放掉<br />
        ///     2、支持动态参数，从方法传入参数值动态构建<br />
        /// </summary>
        public string Value { set; get; }

        /// <summary>
        /// 本次加锁尝试失败的最大重试次数<br />
        ///     1、为0则表示不尝试等待加锁，互斥锁；最大重试400次<br />
        ///     2、；每次重试间隔100ms<br />
        /// </summary>
        public uint TryCount { set; get; }

        /// <summary>
        /// 锁的过期时间（单位秒）<br />
        ///     1、防止死锁<br />
        ///     2、&lt;=0 则默认10分钟
        /// </summary>
        public int ExpireSeconds { set; get; }
    }
}

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
        /// 加锁的Key；确保唯一；
        /// </summary>
        public string Key { set; get; }

        /// <summary>
        /// 锁的值；在释放锁时使用；只有值正确才能被释放掉
        /// </summary>
        public string Value { set; get; }

        /// <summary>
        /// 本次加锁尝试失败的最大重试次数；默认20次；每次重试间隔100ms。最大重试400次；为0则表示不尝试等待加锁，互斥锁
        /// </summary>
        public uint TryCount { set; get; }

        /// <summary>
        /// 锁的过期时间（单位秒），防止死锁；&lt;=0 则默认10分钟
        /// </summary>
        public int ExpireSeconds { set; get; }
    }
}

using System;

namespace Snail.Aspect.Distribution.Attributes;

/// <summary>
/// 特性标签：并发锁方法，标记此方法中代码执行时进行并发锁控制
/// <para>1、配合<see cref="LockAspectAttribute"/>使用，可指定并发的Key等信息 </para>
/// <para>2、配合<see cref="ExpireAttribute"/>使用，可在加锁时指定过期时间，避免死锁</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class LockMethodAttribute : Attribute
{
    /// <summary>
    /// 加锁的Key；确保唯一
    /// <para>1、支持从方法参数上进行动态key构建，如 "/api/x/{orgId}" 则orgId为方法参数名，自动取值做替换 </para>
    /// <para>2、支持动态参数，从方法传入参数值动态构建 </para>
    /// </summary>
    public required string Key { init; get; }

    /// <summary>
    /// 锁的值
    /// <para>1、在释放锁时使用；只有值正确才能被释放掉 </para>
    /// <para>2、支持动态参数，从方法传入参数值动态构建 </para>
    /// </summary>
    public required string Value { init; get; }

    /// <summary>
    /// 本次加锁尝试失败的最大重试次数
    /// <para>1、为0则表示不尝试等待加锁，互斥锁；最大重试400次 </para>
    /// <para>2、每次重试间隔100ms </para>
    /// </summary>
    public uint TryCount { init; get; }
}

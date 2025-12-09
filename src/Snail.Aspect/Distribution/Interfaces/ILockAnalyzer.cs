using Snail.Aspect.Distribution.Attributes;
using System.Collections.Generic;

namespace Snail.Aspect.Distribution.Interfaces;

/// <summary>
/// 接口：并发锁分析器
///     1、处理并发锁操作时的Key上的动态参数信息<br />
///     2、配合<see cref="LockAspectAttribute"/>在进行并发锁操作干预<br />
/// </summary>
public interface ILockAnalyzer
{
    /// <summary>
    /// 分析并发锁的key和value数据
    /// </summary>
    /// <param name="lockKey">并发锁Key值</param>
    /// <param name="lockValue">并发锁Value值</param>
    /// <param name="parameters">外部传入的已有参数字典；key为参数名、value为具体参数值</param>
    void Analysis(ref string lockKey, ref string lockValue, IDictionary<string, object> parameters);
}

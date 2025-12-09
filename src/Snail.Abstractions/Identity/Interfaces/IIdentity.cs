namespace Snail.Abstractions.Identity.Interfaces;

/// <summary>
/// 接口约束：身份信息，主键Id <br />
///     1、约束必须有Id值，用于唯一标记 <br />
/// </summary>
public interface IIdentity
{
    /// <summary>
    /// Id值，唯一标记当前对象
    /// </summary>
    public string Id { get; }
}

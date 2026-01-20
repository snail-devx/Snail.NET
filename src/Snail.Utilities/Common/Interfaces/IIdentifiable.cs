namespace Snail.Utilities.Common.Interfaces;
/// <summary>
/// 接口约束：可识别对象
/// <para>1、使用唯一<see cref="Id"/>值作为主键值，唯一标记当前对象</para>
/// </summary>
public interface IIdentifiable
{
    /// <summary>
    /// 唯一Id值
    /// </summary>
    public string Id { get; }
}
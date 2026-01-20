namespace Snail.Utilities.Common.Interfaces;

/// <summary>
/// 接口：标记实现类可进行验证操作
/// <para>1、具体验证逻辑由实现类自定义完成 </para>
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// 执行验证操作
    /// </summary>
    /// <param name="message">验证失败时的错误消息</param>
    /// <returns>验证成功返回true；否则返回false</returns>
    bool Validate(out string? message);
}

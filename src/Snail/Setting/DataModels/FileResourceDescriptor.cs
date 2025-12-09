namespace Snail.Setting.DataModels;

/// <summary>
/// 应用程序的配置文件资源描述器
/// </summary>
internal sealed class FileResourceDescriptor
{
    /// <summary>
    /// 所属工作空间
    /// </summary>
    public required string Workspace { init; get; }
    /// <summary>
    /// 所属项目；为null表示是直属【工作空间】下的资源
    /// </summary>
    public string? Project { init; get; }

    /// <summary>
    /// 配置编码
    /// </summary>
    public required string Code { init; get; }
    /// <summary>
    /// 配置文件路径全路径
    /// </summary>
    public required string Path { init; get; }
}

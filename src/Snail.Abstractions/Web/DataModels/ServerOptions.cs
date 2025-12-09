using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Web.DataModels;

/// <summary>
/// 服务器配置选项
/// </summary>
public class ServerOptions : IServerOptions
{
    #region 属性变量
    /// <summary>
    /// 服务器所在工作空间Key值；<br /> 
    ///     1、为null根据实际需要走默认值或者报错
    /// </summary>
    public string? Workspace { private init; get; }

    /// <summary>
    /// 服务器类型；用于对多个服务器做分组用<br /> 
    ///     1、无分组的服务器取null即可<br /> 
    ///     2、如http请求服务器，可分为http、https、sdk等分组，做不同用途使用
    /// </summary>
    public string? Type { private init; get; }

    /// <summary>
    /// 服务器编码
    /// </summary>
    public string Code { private init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法；workspace、type为null
    /// </summary>
    /// <param name="code">服务器编码；不可为空</param>
    public ServerOptions(string code) : this(workspace: null, type: null, code)
    {
    }
    /// <summary>
    /// 构造方法；type为null
    /// </summary>
    /// <param name="workspace">工作空间Key值；为空强制null</param>
    /// <param name="code">服务器编码；不可为空</param>
    public ServerOptions(string? workspace, string code) : this(workspace, type: null, code)
    { }
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="workspace">工作空间Key值；为空强制null</param>
    /// <param name="type">服务器组名；为空强制null</param>
    /// <param name="code">服务器编码；不可为空</param>
    public ServerOptions(string? workspace, string? type, string code)
    {
        Workspace = Default(workspace, defaultStr: null);
        Type = Default(type, defaultStr: null);
        Code = ThrowIfNullOrEmpty(code)!;
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 重写tostring方法
    /// </summary>
    /// <returns></returns>
    public override string ToString() => $"Workspace={Workspace ?? "null"} Section={Type ?? "null"} Code={Code ?? "null"}";
    #endregion
}

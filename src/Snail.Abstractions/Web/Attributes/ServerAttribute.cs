using Snail.Abstractions.Dependency.Interfaces;
using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Web.Attributes;

/// <summary>
/// 特性标签：【依赖注入】构建实例时的构造方法中<see cref="IServerOptions"/>类型参数值 <br />
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ServerAttribute : Attribute, IParameter<IServerOptions>
{
    #region 属性变量
    /// <summary>
    /// 服务器所在工作空间Key值；<br /> 
    ///     1、为null根据实际需要走默认值或者报错
    /// </summary>
    public string? Workspace { init; get; }

    /// <summary>
    /// 服务器类型；用于对多个服务器做分组用<br /> 
    ///     1、无分组的服务器取null即可<br /> 
    ///     2、如http请求服务器，可分为http、https、sdk等分组，做不同用途使用
    /// </summary>
    public string? Type { init; get; }

    /// <summary>
    /// 服务器编码
    /// </summary>
    public required string Code { init; get; }
    #endregion

    #region IParameter
    /// <summary>
    /// 参数名称：和<see cref="System.Type"/>配合使用，选举要传递信息的目标参数 <br />
    /// 1、Name为空时，则选举第一个类型为Type的参数
    /// 2、Name非空时，则选举类型为Type、且参数名为Name的参数
    /// </summary>
    string? IParameter.Name => null;

    /// <summary>
    /// 获取参数值；由外部自己构建
    /// </summary>
    /// <param name="manager">DI管理器实例</param>
    /// <returns></returns>
    IServerOptions? IParameter<IServerOptions>.GetParameter(in IDIManager manager)
        => new ServerOptions(Workspace, Type, Code);
    #endregion
}

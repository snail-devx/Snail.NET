using Snail.Aspect.Web.Interfaces;
using System;

namespace Snail.Aspect.Web.Attributes;

/// <summary>
/// 特性标签：HTTP请求，标记当前interface是为了发送HTTP请求使用 <br />
///     1、内部所有方法都是用于发送http请求的 <br />
///     2、配合【Snail.Aspect】项目使用，自动为接口生成实现类，并注册为依赖注入组件 <br />
///     3、可使用<see cref="Analyzer"/>实现内部请求相关处理，如分析替换url上的参数信息 <br />
/// </summary>
/// <remarks>------------------------------------------------------------------------------------------- <br />
///     1、目标项目引入【Snail.Aspect】项目包，并在引用包的条目上增加配置： <br />
///         OutputItemType="Analyzer" ReferenceOutputAssembly="true" <br />
///     2、目标项目文件“PropertyGroup”节点下增加如下配置，即可看到生成的源代码<br />
///          &lt;EmitCompilerGeneratedFiles&gt;true&lt;/EmitCompilerGeneratedFiles&gt; <br />
///     3、class时，仅支持abstract标记class
/// </remarks>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class HttpAspectAttribute : Attribute
{
    /// <summary>
    /// HTTP请求目标服务器所在工作空间Key值；<br /> 
    ///     1、为null根据实际需要走默认值或者报错
    /// </summary>
    public string Workspace { set; get; }

    /// <summary>
    /// HTTP请求目标服务器类型；用于对多个服务器做分组用<br /> 
    ///     1、无分组的服务器取null即可<br /> 
    ///     2、如http请求服务器，可分为http、https、sdk等分组，做不同用途使用
    /// </summary>
    public string Type { set; get; }

    /// <summary>
    /// HTTP请求目标服务器编码
    /// </summary>
    public string Code { set; get; }

    /// <summary>
    /// HTTP请求分析器<see cref="IHttpAnalyzer"/>的依赖注入Key值
    /// </summary>
    /// <remarks>不传入则采用默认的分析器</remarks>
    public string Analyzer { set; get; }
}

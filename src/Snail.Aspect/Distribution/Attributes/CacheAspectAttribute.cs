using Snail.Aspect.Distribution.Interfaces;
using System;

namespace Snail.Aspect.Distribution.Attributes;

/// <summary>
/// 特性标签：缓存切面，标记当前类中有方法走缓存逻辑
/// <para>1、约束缓存服务器；此类下的<see cref="CacheMethodAttribute"/>标记的方法进行自动进行缓存操作 </para>
/// <para>2、配合【Snail.Aspect】项目使用，自动为接口生成实现类，并注册为依赖注入组件 </para>
/// </summary>
/// <remarks>-------------------------------------------------------------------------------------------
/// <para>1、目标项目引入【Snail.Aspect】项目包，并在引用包的条目上增加配置：  </para>
/// <para>    OutputItemType="Analyzer" ReferenceOutputAssembly="true"  </para>
/// <para>2、目标项目文件“PropertyGroup”节点下增加如下配置，即可看到生成的源代码 </para>
/// <para>     &lt;EmitCompilerGeneratedFiles&gt;true&lt;/EmitCompilerGeneratedFiles&gt; </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class CacheAspectAttribute : Attribute
{
    /// <summary>
    /// 缓存服务器所在工作空间Key值
    /// </summary>
    public string? Workspace { init; get; }

    /// <summary>
    /// 缓存服务器类型；用于对多个服务器做分组用
    /// <para>1、无分组的服务器取null即可 </para>
    /// </summary>
    public string? Type { init; get; }

    /// <summary>
    /// 缓存服务器编码
    /// </summary>
    public required string Code { init; get; }

    /// <summary>
    /// 缓存分析器<see cref="ICacheAnalyzer"/>的依赖注入Key值
    /// <para>1、若想使用默认的分析器，则传入null：Analyzer=null </para>
    /// <para>2、此属性不显式赋值，则表示不使用分析器 </para>
    /// </summary>
    public string? Analyzer { init; get; }
}

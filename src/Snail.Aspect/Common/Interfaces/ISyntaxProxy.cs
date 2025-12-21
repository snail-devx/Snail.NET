using Microsoft.CodeAnalysis;

namespace Snail.Aspect.Common.Interfaces;

/// <summary>
/// 接口约束：语法代理器
/// </summary>
internal interface ISyntaxProxy
{
    /// <summary>
    /// 唯一Key值，将作为生成的源码cs文件名称
    /// </summary>
    /// <remarks>若返回null，则不会生成cs文件</remarks>
    string Key { get; }

    /// <summary>
    /// 生成HTTP接口实现类源码
    /// </summary>
    /// <param name="context"></param>
    /// <returns>生成好的代码</returns>
    string? GenerateCode(SourceProductionContext context);
}

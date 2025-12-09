using System;

namespace Snail.Aspect.General.Attributes;

/// <summary>
/// 特性标签：标记此类有方法需要走验证逻辑 <br />
///     1、验证方法参数的有效性、验证字段属性的有效性<br />
///     2、配合<see cref="RequiredAttribute"/>等参数标记自动生成验证代码<br />
///     3、若类型中的方法参数为【Snail.Abstractions.Common.Interfaces.IValidatable】类型，则自动执行Validate()方法
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class ValidateAspectAttribute : Attribute
{
}
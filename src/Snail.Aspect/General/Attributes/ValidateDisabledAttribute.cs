using System;

namespace Snail.Aspect.General.Attributes;

/// <summary>
/// 特性标签：禁止验证切面
/// <para>1、有此标记的方法，<see cref="ValidateSyntaxMiddleware"/>在生成重写代码时，会忽略此方法</para>
/// <para>2、某些方法参数传入了实现<see cref="TYPENAME_IValidatable"/>接口的方法，但不需要验证时，使用此特性标签标记。如private、static方法</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ValidateDisabledAttribute : Attribute
{
}

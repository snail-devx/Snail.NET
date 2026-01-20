using Snail.Utilities.Common.Utils;
using System.Reflection;

namespace Snail.Utilities.Common.Delegates;

/// <summary>
/// 方法调用委托
/// <para>1、配合<see cref="TypeHelper.CreateDelegate(MethodInfo, out ParameterInfo[], out Type?)"/>方法使用</para>
/// </summary>
/// <param name="instance">方法所属实例，静态方法传null即可</param>
/// <param name="args">方法调用时所传递的参数</param>
/// <returns>方法返回值，若方法为void，则返回值强制为null</returns>
public delegate object? MethodDelegate(object? instance, params object[] args);
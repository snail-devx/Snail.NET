using Snail.WebApp.Interfaces;

namespace Snail.WebApp.DataModels;

/// <summary>
/// 记录：API动作标签任务
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="Attr">API动作特性标签：<see cref="IActionAttribute.Disabled"/>为false时，才执行<paramref name="Task"/>任务</param>
/// <param name="Task">要执行的任务委托：返回若为非null的Task时，则需要等待执行</param>
/// <param name="Predicate">例外断言条件：非null时执行返回true后，再执行<paramref name="Task"/>任务</param>
public record ActionAttributeTask<T>(T? Attr, Func<Task?> Task, Func<bool>? Predicate = null) where T : IActionAttribute;
using Snail.Abstractions.Dependency.DataModels;
using Snail.Abstractions.Dependency.Interfaces;
using Snail.Utilities.Common.Extensions;
using System.Diagnostics;

namespace Snail.Abstractions.Dependency.Extensions;

/// <summary>
/// 依赖注入针对<see cref="ICustomAttributeProvider"/>的扩展方法
/// </summary>
public static class CustomAttributeExtensions
{
    #region IInject和、IParameter配合使用
    /// <summary>
    /// 是否有实现<see cref="IInject"/>接口的特性标签 <br />
    ///     1、内部判断传入type的GetCustomAttributes方法判断特性标签是否实现了<see cref="IInject"/>接口 <br />
    ///     2、只找第一个；若有多个实现<see cref="IInject"/>的特性，注意明确 <br />
    /// </summary>
    /// <param name="provider">要判断的成员</param>
    /// <param name="inject">inject示例</param>
    /// <returns>是返回true，否则返回false</returns>
    public static bool HasInjectAttribute(this ICustomAttributeProvider provider, out IInject? inject)
    {
        //  遍历找第一个
        inject = null;
        foreach (var attr in provider.GetCustomAttributes(inherit: false))
        {
            if (attr is IInject tmpInject)
            {
                inject = tmpInject;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取指定成员的实现<see cref="IParameter"/>接口的标签信息 <br />
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="inject">输出参数：打在<paramref name="provider"/>上的第一个<see cref="IInject"/>接口实例</param>
    /// <returns></returns>
    public static IList<IParameter> GetParameterAttribute(this ICustomAttributeProvider provider, out IInject? inject)
    {
        inject = null;
        List<IParameter> parameters = new List<IParameter>();
        foreach (var attr in provider.GetCustomAttributes(inherit: false))
        {
            //  注入特性，仅取第一个
            if (inject == null && attr is IInject tmpInject)
            {
                inject = tmpInject;
            }
            //  注入参数，可以有多个；这里不用用switch，避免特性标签同时实现 注入和注入参数  接口
            if (attr is IParameter param)
            {
                param?.AddTo(parameters);
            }
        }
        return parameters;
    }
    #endregion

    #region IComponent
    /// <summary>
    /// 是否能够作为依赖注入的to类型
    /// </summary>
    /// <param name="to"></param>
    /// <param name="error">不能作为to类型的原因</param>
    /// <returns>能返回true；否则返回false</returns>
    public static bool CanAsToType(this Type to, out string? error)
    {
        error = null;
        if (to.IsInterface) error = $"to不能是接口：{to.FullName}";
        else if (to.IsAbstract && to.IsSealed) error = $"to不能是静态类：{to.FullName}";
        else if (to.IsAbstract) error = $"to不能是抽象类：{to.FullName}";
        return error == null;
    }

    /// <summary>
    /// 是否是依赖注入的实现组件
    /// </summary>
    /// <param name="type"></param>
    /// <param name="components">输出参数：组件信息</param>
    /// <returns></returns>
    public static bool IsComponent(this Type type, out IList<IComponent>? components)
    {
        components = new List<IComponent>();
        foreach (var attr in type.GetCustomAttributes())
        {
            if (attr is IComponent component)
            {
                components.Add(component);
            }
        }
        return components.Count > 0;
    }
    /// <summary>
    /// 是否是依赖注入的实现组件
    /// </summary>
    /// <param name="type"></param>
    /// <param name="descriptors">输出参数：组件的依赖注入相关信息</param>
    /// <returns></returns>
    public static bool IsComponent(this Type type, out IList<DIDescriptor>? descriptors)
    {
        descriptors = null;
        //  若type自身无法作为实现类，则标记了也无效，给出调试信息
        if (type.CanAsToType(out string? error) == false)
        {
            Debug.WriteLine($"不能作为组件使用，{error}");
            return false;
        }
        //  分析特性标签， 转成依赖注入信息描述器
        descriptors = type.GetCustomAttributes()
             .Select(attr => attr is IComponent com
                ? new DIDescriptor(com.Key, com.From ?? type, com.Lifetime, type)
                : null
             )
             .Where(com => com != null)
             .ToList()!;
        return descriptors.Count > 0;
    }
    #endregion
}

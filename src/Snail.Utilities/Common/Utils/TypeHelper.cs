using Snail.Utilities.Collections;
using System.Reflection;

namespace Snail.Utilities.Common.Utils;
/// <summary>
/// <see cref="Type"/>助手类；实现动态加载类型、加载类型属性，做一些缓存逻辑、、、
/// </summary>
public static class TypeHelper
{
    #region 属性变量
    /// <summary>
    /// 反射获取成员信息时的绑定标记
    ///     暂时先只要公共的，后期考虑进行private和protected修饰符访问
    /// </summary>
    private const BindingFlags BINDINGFLAGS = BindingFlags.Public | BindingFlags.Instance;
    /// <summary>
    /// 类型的属性映射字典
    /// </summary>
    private static readonly LockMap<Type, PropertyInfo[]> _propertyMap = new();
    /// <summary>
    /// 类型的字段映射字典
    /// </summary>
    private static readonly LockMap<Type, FieldInfo[]> _fieldMap = new();
    #endregion

    #region 公共方法

    #region Type值处理
    /// <summary>
    /// 获取类型信息
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    public static Type LoadType(in string typeName)
    {
        //  之前想基于typeName做缓存，并加锁；经过测试，大批量1w+实时获取时，还没有直接取快，放弃缓存
        ThrowIfNullOrEmpty(typeName);
        Type type = Type.GetType(typeName)
            ?? throw new ApplicationException($"加载类型失败，返回null。typeName:{typeName}");
        return type;
    }
    #endregion

    #region 属性字段值处理
    /// <summary>
    /// 获取指定类型的属性信息
    ///     1、继承父级属性同步带入
    ///     2、只取公共+实例级别属性
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static PropertyInfo[] GetProperties(Type type)
    {
        //  内部自动做缓存
        ThrowIfNull(type);
        return _propertyMap.GetOrAdd(type, type.GetProperties, BINDINGFLAGS);
    }
    /// <summary>
    /// 获取指定类型的字典信息
    ///     1、继承父级属性同步带入
    ///     2、只取公共+实例级别属性
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static FieldInfo[] GetFields(Type type)
    {
        //  内部自动做缓存
        ThrowIfNull(type);
        return _fieldMap.GetOrAdd(type, type.GetFields, BINDINGFLAGS);
    }

    /// <summary>
    /// 将源对象的属性值拷贝给指定的目标对象<br />
    ///     1、属性名相同；对象Type一样时
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public static TargetType CopyPropertyValue<TargetType>(object source, TargetType target)
        where TargetType : class
    {
        ThrowIfNull(source);
        ThrowIfNull(target);
        //	找属性信息；并验证
        PropertyInfo[] pSources = GetProperties(source.GetType());
        ThrowIfNullOrEmpty(pSources, "反射source的Property信息为空，无法进行属性值拷贝");
        PropertyInfo[] pTargets = GetProperties(target.GetType());
        ThrowIfNullOrEmpty(pTargets, "反射target的Property信息为空，无法进行属性值拷贝");
        //	遍历source属性，相同名称，类型相同的属性，进行值复制
        foreach (PropertyInfo pSource in pSources)
        {
            PropertyInfo? pTarget = pTargets.FirstOrDefault(item => item.Name == pSource.Name && item.PropertyType == pSource.PropertyType);
            if (pTarget != null && pTarget.CanWrite)
            {
                pTarget.SetValue(target, pSource.GetValue(source));
            }
        }
        return target;
    }
    #endregion

    #endregion
}

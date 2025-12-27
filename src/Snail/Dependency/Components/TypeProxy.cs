using Snail.Abstractions.Common.DataModels;
using Snail.Abstractions.Dependency.Interfaces;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Snail.Dependency.Components;

/// <summary>
/// 类型代理器 
/// <para>1、代理类型构建实例逻辑  </para>
/// <para>2、将<see cref="Type"/>用于实例构建的相关信息缓存起来，加快后续构建性能 </para>
/// <para>3、代理器基于对象池，针对长时间不使用的<see cref="Type"/>类型，做自动清理 </para>
/// </summary>
public sealed class TypeProxy : PoolObject<Type>
{
    #region 属性变量
    /// <summary>
    /// 反射时的绑定类型：实例级的公共和非公共方法
    /// </summary>
    private const BindingFlags BINDINGFLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// 类型代理池
    /// <para>1、超过5分钟不用自动清理掉 </para>
    /// <para>2、【全局单例】生命周期依赖注入，类型代理只会分析一次，以后就不会再用，始终占着没有意义 </para>
    /// </summary>
    private static readonly ObjectPool<TypeProxy> _typePool = new ObjectPool<TypeProxy>(FromMinutes(1));

    /// <summary>
    /// 依赖注入时，执行的构造方法
    /// </summary>
    public ConstructorInfo Constructor { private init; get; }
    /// <summary>
    /// 依赖注入时，需要初始化的字段集合
    /// </summary>
    public ReadOnlyCollection<FieldInfo>? Fields { private init; get; }
    /// <summary>
    /// 依赖注入时，需要初始化的属性集合
    /// </summary>
    public ReadOnlyCollection<PropertyInfo>? Properties { private init; get; }
    /// <summary>
    /// 依赖注入时，需要执行的方法集合
    /// </summary>
    public ReadOnlyCollection<MethodInfo>? Methods { private init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="type">类型</param>
    private TypeProxy(Type type) : base(type)
    {
        //  1、选举构造方法
        {
            Constructor = ThrowIfNull(ElectConstructor(type), $"传入类型无实例构造方法：{type.FullName}");
#if DEBUG
            Debug.WriteLine($"-----------代理类型： {type.FullName} 构造方法： {Constructor.ToString()}");
#endif
        }
        //  2、选举需要注入的字段：仅要可写的引用类型
        {
            IList<FieldInfo> fields = new List<FieldInfo>();
            foreach (var field in type.GetFields(BINDINGFLAGS))
            {
                var injectField = field.IsInitOnly || field.FieldType.IsValueType || field.HasInjectAttribute(out _) == false
                      ? null
                      : field;
                injectField?.AddTo(fields);
            }
            Fields = fields.Count > 0 ? new ReadOnlyCollection<FieldInfo>(fields) : null;
        }
        //  3、选举属性：仅要可写的引用类型
        {
            IList<PropertyInfo> properties = new List<PropertyInfo>();
            foreach (var pi in type.GetProperties(BINDINGFLAGS))
            {
                var injectProperty = pi.CanWrite == false || pi.PropertyType.IsValueType || pi.HasInjectAttribute(out _) == false
                    ? null
                    : pi;
                injectProperty?.AddTo(properties);
            }
            Properties = properties.Count > 0 ? new ReadOnlyCollection<PropertyInfo>(properties) : null;
        }
        //  4、选举需要执行的注入方法
        {
            IList<MethodInfo> methods = new List<MethodInfo>();
            foreach (var method in type.GetMethods(BINDINGFLAGS))
            {
                var injectMethod = method.HasInjectAttribute(out _) ? method : null;
                injectMethod?.AddTo(methods);
            }
            Methods = methods.Count > 0 ? new ReadOnlyCollection<MethodInfo>(methods) : null;
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 获取类型代理器
    /// </summary>
    /// <param name="type"></param>
    public static TypeProxy GetProxy(Type type)
    {
        TypeProxy proxy = _typePool.GetOrAdd
        (
            predicate: proxy => proxy.Object == type,
            addFunc: () => new TypeProxy(type),
            autoUsing: true
        );
        return proxy;
    }

    /// <summary>
    /// 动态构建<see cref="Type"/>实例值
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public object? BuildInstance(IDIManager manager, IParameter[]? parameters)
    {
        //  加异常捕捉，做好【闲置时间】管理
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        try
        {
            IdleTime = default;
            object? instance = null;
            //  构造方法注入，创建实例；外部传入的parameters参数传递过来作为构造方法的补充参数
            {
                object?[]? paramValues = BuildMethodParamVlaues(manager, Constructor, parameters);
                instance = Constructor.Invoke(paramValues);
            }
            //  注入初始化字段、属性
            Fields?.ForEach(field =>
            {
                object? value = BuildValueByAttribute(manager, field.FieldType, field);
                field.SetValue(instance, value);
            });
            Properties?.ForEach(property =>
            {
                object? value = BuildValueByAttribute(manager, property.PropertyType, property);
                property.SetValue(instance, value);
            });
            //  执行初始化方法：这里不能将外部传入的parameters参数传递进去（parameters仅用于构造方法）
            Methods?.ForEach(method =>
            {
                object?[]? paramValues = BuildMethodParamVlaues(manager, method, extParams: null);
                method.Invoke(instance, paramValues);
            });

            return instance;
        }
        finally
        {
            IdleTime = DateTime.UtcNow;
        }
    }
    #endregion

    #region 继承方法
    /// <summary>
    /// 销毁对象
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed == false)
        {
            if (disposing)
            {
                Object.TryDispose();
            }
            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
            Debug.WriteLine($"闲置类型代理销毁：{Object.FullName}");
        }
        //  执行基类回收
        base.Dispose(disposing);
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 选举类型的构造方法
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static ConstructorInfo? ElectConstructor(Type type)
    {
        /** 优先有依赖注入标签的，否则根据访问优先级（public->internal->protected->private） */
        ConstructorInfo[] ctors = type.GetConstructors(BINDINGFLAGS);
        ConstructorInfo? ctor = null;
        if (ctors?.Length > 0)
        {
            //  优先取有依赖注入标签的，取到第一个就算
            ctor = ctors.FirstOrDefault(item => item.HasInjectAttribute(out _));
            //  按访问权限排序；取优先级最高的一组  private > internal > protected > public；优先public
            if (ctor == null)
            {
                //  1、按访问权限枚举值分组排序，取最高的一组
                IGrouping<int, ConstructorInfo> group = ctors.GroupBy(item =>
                {
                    MethodAttributes attr = item.Attributes & MethodAttributes.MemberAccessMask;
                    switch (attr)
                    {
                        //  取枚举值的实际数值
                        case MethodAttributes.Private://  private                         1
                        case MethodAttributes.FamANDAssem://  internal and protected      2
                        case MethodAttributes.Assembly://  internal                       3
                        case MethodAttributes.Family://   protected                       4
                        case MethodAttributes.FamORAssem://  internal or protected        5
                        case MethodAttributes.Public://  public                           6
                            return Convert.ToInt32(attr) * 1000;
                        //  其他情况，强制最低优先级：0
                        default:
                            return 0;
                    }
                }).OrderByDescending(item => item.Key).First();
                //  2、按照构造方法参数数量分组排序，取参数最高的一组
                group = group.GroupBy(item => item.GetParameters().Length).OrderByDescending(item => item.Key).First();
                //  3、取第一个参数最多的构造方法作为依赖注入的构造方法
                ctor = group.First();
            }

            // if (ctor == null)
            // {
            //     var enumerable = ctors.OrderByDescending(item => item.GetParameters().Length);
            //     ctor = enumerable.FirstOrDefault(item => item.IsPublic)
            //         ?? ctors.FirstOrDefault(item => item.Attributes)
            //         ?? ctors.FirstOrDefault(item => item)
            //         ?? ctors.FirstOrDefault(item => item.IsPrivate);
            // }
        }

        return ctor;
    }
    /// <summary>
    /// 构建方法的参数数值
    /// </summary>
    /// <param name="manager">依赖注入管理器</param>
    /// <param name="method">方法对象</param>
    /// <param name="extParams">执行方法时，外部已经传入的参数信息；不用再动态构建了</param>
    /// <returns></returns>
    private static object?[]? BuildMethodParamVlaues(in IDIManager manager, in MethodBase method, in IParameter[]? extParams)
    {
        ParameterInfo[] pis = method.GetParameters();
        object?[]? paramValues = pis.Any() ? new object[pis.Length] : null;
        //  构建需要已有参数列表，方便命中后直接移除，避免多次名称
        IList<IParameter> parameters = extParams?.ToList() ?? new List<IParameter>();
        //  遍历属性，从外部传入的parameters优先取值，娶不到进行构建；确保parameters只会被用一次
        for (int index = 0; index < pis.Length; index++)
        {
            ParameterInfo pi = pis[index];
            //  1、先找外部传入Parameters是否存在：先Type类型，然后Name做筛选（无则取类型匹配的第一个）
            IParameter? parameter = parameters.FirstOrDefault(param => param.Type == pi.ParameterType && param.Name == pi.Name)
                ?? parameters.FirstOrDefault(param => param.Type == pi.ParameterType);
            parameter?.RemoveFrom(parameters);
            //  2、构建值存储：先基于外部param处理，再基于特性标签构建值（构建前，若参数存在默认值，则使用非null默认值）
            object? value = pi.HasDefaultValue ? pi.DefaultValue : null;
            value = parameter != null
                ? (parameter.GetParameter(manager) ?? value)
                : BuildValueByAttribute(manager, pi.ParameterType, pi, defaultValue: value);
            paramValues![index] = value;
        }
        return paramValues;
    }
    /// <summary>
    /// 基于依赖注入的特性标签构建值
    /// </summary>
    /// <param name="manager">依赖注入管理器</param>
    /// <param name="from">构建值的源类型</param>
    /// <param name="provider">自定义属性提供程序，如<see cref="FieldInfo" />、<see cref="PropertyInfo"/>、<see cref="ParameterInfo"/></param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>构建出来的值；若如依赖注入标签，则构建默认值（如int为0）</returns>
    private static object? BuildValueByAttribute(in IDIManager manager, in Type from, in ICustomAttributeProvider provider, in object? defaultValue = null)
    {
        //  构建类型值时；按照值类型和引用类型做一下区分，后续看情况值类型也走依赖注入
        object? value;
        if (from.IsValueType == true)
        {
            value = defaultValue ?? Activator.CreateInstance(from);
        }
        else
        {
            IList<IParameter> parameters = provider.GetParameterAttribute(out IInject? inject);
            string? key = inject?.GetKey(manager);
            value = manager.Resolve(key, from, parameters.ToArray()) ?? defaultValue;
        }
        return value;
    }
    #endregion
}

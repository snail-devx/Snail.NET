using Snail.Utilities.Collections;
using Snail.Utilities.Common.Delegates;
using System.Linq.Expressions;
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
    /// 将源对象的属性值拷贝给指定的目标对象
    /// <para>1、属性名相同；对象Type一样时 </para>
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

    #region 方法处理
    /// <summary>
    /// 创建方法的调用委托
    /// <para>1、做性能优化使用；避免频繁使用<see cref="MethodBase.Invoke(object?, object?[])"/>执行方法</para>
    /// <para>2、外部做好缓存，别针对相同方法进行重复调用</para>
    /// </summary>
    /// <param name="method">要构建委托的方法</param>
    /// <returns>方法委托</returns>
    public static MethodDelegate CreateDelegate(MethodInfo method)
        => CreateDelegate(method, out _, out _);
    /// <summary>
    /// 创建方法的调用委托
    /// <para>1、做性能优化使用；避免频繁使用<see cref="MethodBase.Invoke(object?, object?[])"/>执行方法</para>
    /// <para>2、外部做好缓存，别针对相同方法进行重复调用</para>
    /// </summary>
    /// <param name="method">要构建委托的方法</param>
    /// <param name="parameterInfos">out参数：方法的参数信息</param>
    /// <param name="returnType">out参数：方法返回值类型，若为null则为void，无返回值</param>
    /// <returns>方法委托</returns>
    public static MethodDelegate CreateDelegate(MethodInfo method, out ParameterInfo[] parameterInfos, out Type? returnType)
    {
        //  1、准备工作，参数准备：通用参数：instance（先不区分静态和实例方法，都构建instance参数，但静态方法不传入instance参数）、args
        ThrowIfNull(method);
        ThrowIf(method.IsGenericMethodDefinition, $"不支持泛型定义方法{method}构建委托");
        ThrowIf(method.IsAbstract, $"不支持抽象方法{method}构建委托");
        {
            parameterInfos = method.GetParameters();
            returnType = method.ReturnType;
            if (returnType == typeof(void))
            {
                returnType = null;
            }
        }
        ParameterExpression instanceParam = Expression.Parameter(typeof(object), "instance");
        ParameterExpression argsParam = Expression.Parameter(typeof(object[]), "args");
        //  2、构建方法调用表达式：将method参数和args进行桥接转换成实际类型；针对in、out、ref等参数，转换成实际类型（IsByRef）
        Expression callExpression;
        {
            UnaryExpression[] castedParams = new UnaryExpression[parameterInfos.Length];
            for (var index = 0; index < parameterInfos.Length; index++)
            {
                Type paramType = parameterInfos[index].ParameterType;
                BinaryExpression argAccess = Expression.ArrayIndex(argsParam, Expression.Constant(index));
                castedParams[index] = Expression.Convert(argAccess, paramType.IsByRef ? paramType.GetElementType()! : paramType);
            }
            callExpression = method.IsStatic
                ? Expression.Call(method, castedParams)
                : Expression.Call(instance: Expression.Convert(instanceParam, method.DeclaringType!), method, castedParams);
        }
        //  3、构建lambda表达式，编译返回委托：构建返回值，void类型时，强制返回null，否则对返回值转换成object
        callExpression = returnType == null
                ? Expression.Block(callExpression, Expression.Constant(null))
                : Expression.Convert(callExpression, typeof(object));
        //      调试时：VSCode能看结果，VS提示内部错误，但不影响最终运行；具体原因不清楚
        return Expression.Lambda<MethodDelegate>(callExpression, instanceParam, argsParam)
            .Compile();

        /* 下面是使用IL代码编织直接生成委托（秘塔AI搜索）；创建委托效率确实高，还没搞懂原理，先不对外开放 
         // 1. 参数验证与初始化
        ThrowIfNull(method);
        ThrowIf(method.IsGenericMethodDefinition, $"不支持泛型定义方法{method}构建委托");
        ThrowIf(method.IsAbstract, $"不支持抽象方法{method}构建委托");
        
        parameterInfos = method.GetParameters();
        returnType = method.ReturnType == typeof(void) ? null : method.ReturnType;

        // 2. 创建 DynamicMethod（返回值为 object，参数为 object instance, object[] args）
        var dynamicMethod = new DynamicMethod(
            $"DynamicInvoker_{method.Name}_{Guid.NewGuid():N}",
            typeof(object),
            new Type[] { typeof(object), typeof(object[]) },
            typeof(DynamicMethodHelper).Module,
            true // 跳过可见性检查
        );

        var il = dynamicMethod.GetILGenerator();

        // 3. 处理实例（如果是实例方法，需要加载 instance 并转换类型）
        if (!method.IsStatic)
        {
            // 加载 instance 参数 (args[0]) 并转换为声明类型
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, method.DeclaringType!);
        }

        // 4. 处理参数数组（args）
        for (int i = 0; i < parameterInfos.Length; i++)
        {
            var paramInfo = parameterInfos[i];
            var paramType = paramInfo.ParameterType;

            // 加载 args 参数数组 (args[1])
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4, i); // 索引
            il.Emit(OpCodes.Ldelem_Ref); // 取出 args[i]

            // 处理 ByRef 参数（ref/out）
            if (paramType.IsByRef)
            {
                // ByRef 参数需要传入地址，而不是值
                var elementType = paramType.GetElementType()!;
                // 将 args[i] 转换为实际类型的引用
                // 这里我们创建一个局部变量来存储解引用后的值
                var local = il.DeclareLocal(elementType);
                // 将 args[i] 转换为元素类型
                il.Emit(OpCodes.Unbox_Any, elementType);
                // 存入局部变量
                il.Emit(OpCodes.Stloc, local);
                // 加载局部变量的地址（ref）
                il.Emit(OpCodes.Ldloca_S, local);
            }
            else
            {
                // 非 ByRef 参数直接转换类型
                // 注意：如果参数是值类型，需要使用 Unbox_Any；引用类型则使用 Castclass
                if (paramType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, paramType);
                else
                    il.Emit(OpCodes.Castclass, paramType);
            }
        }

        // 5. 调用原始方法
        il.Emit(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, method);

        // 6. 处理返回值
        if (returnType == null)
        {
            // void 方法返回 null
            il.Emit(OpCodes.Ldnull);
        }
        else if (returnType.IsValueType)
        {
            // 将值类型装箱为 object
            il.Emit(OpCodes.Box, returnType);
        }
        // 引用类型直接是 object，无需处理

        // 7. 返回
        il.Emit(OpCodes.Ret);

        // 8. 创建委托
        return (MethodDelegate)dynamicMethod.CreateDelegate(typeof(MethodDelegate));
         */

    }
    #endregion

    #endregion
}

namespace Snail.Abstractions.Dependency.DataModels
{
    /// <summary>
    /// 依赖注入描述器
    /// </summary>
    public class DIDescriptor
    {
        #region 属性变量
        /// <summary>
        /// 依赖注入Key值，用于DI动态构建实例
        /// </summary>
        public string? Key { protected init; get; }
        /// <summary>
        /// 依赖注入源类型
        /// </summary>
        public Type From { protected init; get; }
        /// <summary>
        /// 生命周期，默认【瞬时】
        /// </summary>
        public LifetimeType Lifetime { protected init; get; }

        /// <summary>
        /// 实现类的类型。和<see cref="ToFunc"/>互斥
        /// </summary>
        public Type? To { protected init; get; }
        /// <summary>
        /// 实现类的实例构建委托，构建实例时，调用此委托完成实例构建。和<see cref="To"/>”互斥
        /// </summary>
        public Func<IDIManager, object?>? ToFunc { protected init; get; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法：源类型和目标类型
        /// </summary>
        /// <param name="key">依赖注入Key值；唯一</param>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="lifetime">依赖注入实现类生命周期</param>
        /// <param name="to">依赖注入实现类类型；确保to能够实例化构建from</param>
        public DIDescriptor(in string? key, in Type from, in LifetimeType lifetime, in Type to)
        {
            //   不再检查类型；否则【Microsoft.Extensions.DependencyInjection】对接时，会报错
            //ChekFromToType(from, to);

            Key = key;
            From = ThrowIfNull(from);
            Lifetime = lifetime;
            To = ThrowIfNull(to);
        }

        /// <summary>
        /// 构造方法：源类型和目标构建委托
        /// </summary>
        /// <param name="key">依赖注入Key值；唯一</param>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="lifetime">依赖注入实现类生命周期</param>
        /// <param name="toFunc">构建to实例的委托方法</param>
        public DIDescriptor(in string? key, in Type from, in LifetimeType lifetime, in Func<IDIManager, object?> toFunc)
        {
            //   不再检查类型；否则【Microsoft.Extensions.DependencyInjection】对接时，会报错
            //ThrowIfTrue(from!.IsAbstract && from.IsSealed, $"{nameof(from)}不能是静态类:{from.FullName}");

            Key = key;
            From = ThrowIfNull(from);
            Lifetime = lifetime;
            ToFunc = ThrowIfNull(toFunc);
        }
        /// <summary>
        /// 构造方法：源类型和目标实例
        /// </summary>
        /// <param name="key">依赖注入Key值；唯一</param>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="lifetime">依赖注入实现类生命周期</param>
        /// <param name="instance">对象实例；内部转换成 Func委托：manager=>instance</param>
        public DIDescriptor(in string? key, in Type from, in LifetimeType lifetime, object instance)
            : this(key, from, lifetime, manager => instance)
        { }
        #endregion

        #region 私有方法：不再检查类型；否则【Microsoft.Extensions.DependencyInjection】对接时，会报错
        ///// <summary>
        ///// 检测依赖注入的From和To类型，确保To能实例化From类型 <br />
        /////     1、这里面规则会比较多，需要充分考虑泛型、继承等情况 <br />
        /////     2、若to无法作为from的实例化，则抛出异常
        ///// </summary>
        ///// <param name="from"></param>
        ///// <param name="to"></param>
        ///// <exception cref="ArgumentNullException"></exception>
        ///// <exception cref="ArgumentException"></exception>
        //public static void ChekFromToType(in Type? from, in Type? to)
        //{
        //    /* 核心规则：from泛型，to泛型；from可构建，则to可构建；from不可构建，则to可构建；from和to同类型，或者是to的父类型
        //     * ------------------------------------------------------------------------------------------------------------------
        //     *  from                    to                          是否可构建
        //     * ------------------------------------------------------------------------------------------------------------------
        //     *  P<T1, T2>               C1<T1, T2>:P<T1, T2>        可构建，继承；类型参数都没确定
        //     *  P<T1, T2>               C1<T1>:P<T1, int>           不可构建，继承；类型参数个数都没确定
        //     *  P<T1, T2>               C1<T1, T2>                  不可构建，不继承
        //     *  P<T1, T2>               C1<int, int>:P<int, int>    不可构建，继承；from类型参数不固定，但to类参数固定
        //     *  P<T1, T2>               C1:P<int, int>              不可构建，继承，from类型参数不固定，但to类参数固定
        //     *  P<int, int>             C1<T1, T2>:P<T1, T2>        不可构建，继承，from类型参数固定，但to参类型不固定
        //     *  P<int, int>             C1<int, int>:P<int, int>    可构建，继承
        //     *  P<int, int>             C1:P<int, int>              可构建，继承
        //     *  P<int, int>             C1                          不可构建，不继承
        //     */
        //    /*  判断父级类型，泛型类型不确定类型时，子类判断比较玄幻，类型举例：P<T1, T2>，C1<T1, T2>:P<T1, T2>  
        //     *   typeof(C1<,>).BaseType.FullName                                                                    null
        //     *   typeof(C1<,>).GetGenericTypeDefinition().BaseType.FullName                                         null
        //     *   typeof(C1<,>).IsSubclassOf(typeof(P<,>))                                                           false
        //     *   typeof(C1<,>).GetGenericTypeDefinition().IsSubclassOf(typeof(P<,>))                                false
        //     *   typeof(C1<,>).GetGenericTypeDefinition().IsSubclassOf(typeof(P<,>).GetGenericTypeDefinition())     false
        //     *   typeof(C1<,>).GetGenericTypeDefinition().BaseType==typeof(P<,>)                                    false
        //     *   
        //     *   typeof(C1<,>).BaseType.GetGenericTypeDefinition()==typeof(P<,>)                                    true
        //     *   typeof(C1<String,String>).IsSubclassOf(typeof(P<String,String>))                                   true
        //     */

        //    //  1、基础验证：非空验证；to不能是不能实例化的类型（接口和抽象类，静态类(静态类IsAbstract也为true））
        //    ThrowIfNull(from, $"{nameof(from)}不能为null");
        //    ThrowIfTrue(from!.IsAbstract && from.IsSealed, $"{nameof(from)}不能是静态类:{from.FullName}");
        //    ThrowIfNull(to, $"{nameof(to)}不能为null");
        //    ThrowIfFalse(CanAsToType(to!, out string? error), error);
        //    //      from和to同类型，不用验证了
        //    if (from == to) return;
        //    //  2、类型判断，from和to涉及到泛型参数时，必须同时确定（List<string>，MyList<string>)、或者不确定(List<>,MyList<>)参数类型
        //    //      from不是泛型：to为确定参数的泛型（如List<String>)、或者非泛型
        //    if (from!.IsGenericType == false)
        //    {
        //        ThrowIfFalse(
        //            to!.IsGenericType == false || to.IsConstructedGenericType == true,
        //            $"from[{from.FullName}]非泛型；to[{to.FullName}]必须为非泛型类型，或者可构建的泛型类型，如List<String，不能是List<T>"
        //        );
        //    }
        //    //      from是泛型，且可构建(List<String>)时；to为确定参数的泛型（如List<String>)、或者非泛型；和from非泛型规则一致
        //    else if (from.IsConstructedGenericType == true)
        //    {
        //        ThrowIfFalse(
        //            to!.IsGenericType == false || to.IsConstructedGenericType == true,
        //            $"from[{from.FullName}]可构建泛型（如List<Stirng>时；to[{to.FullName}]必须为非泛型类型，或者可构建的泛型类型，如List<String>，不能是List<T>"
        //        );
        //    }
        //    //      from是泛型，且不可构建时(List<>)；to为泛型，且不可构建，且参数个数必须已知
        //    else
        //    {
        //        ThrowIfFalse(
        //            to!.IsGenericType && to.IsConstructedGenericType == false && to.GetGenericArguments().Length == from.GetGenericArguments().Length,
        //            $"from[{from.FullName}]不可构建泛型(List<>)时，to[{to.FullName}]必须为不可构建泛型，且泛型参数个数保持一致，如List<>"
        //        );
        //    }
        //    //  3、to必须继承from（基类），或者实现from（接口）。仍然需要基于泛型+是否可构建判断
        //    //      from是接口，则to必须实现此即接口；注意接口泛型的情况
        //    if (from.IsInterface == true)
        //    {
        //        bool bv = false;
        //        foreach (var type in to.GetInterfaces())
        //        {
        //            bv = type.FullName == null
        //               ? type.GetGenericTypeDefinition() == from
        //               : type == from;
        //            if (bv == true) break;
        //        }
        //        ThrowIfFalse(bv, $"from[{from.FullName}]接口时，to[{to.FullName}]必须实现此接口");
        //    }
        //    //      from是类，则to必须继承此基类；注意泛型不可构时，比较玄幻
        //    else
        //    {
        //        //  typeof(C1<,>).BaseType==typeof(P<,>)                                false
        //        //  typeof(C1<,>).BaseType.FullName                                     null
        //        //  typeof(C1<,>).IsSubclassOf(typeof(P<,>))                            false
        //        //  typeof(C1<,>).BaseType.GetGenericTypeDefinition()==typeof(P<,>)     true
        //        bool bv = false;
        //        Type? bType = to;
        //        while (bType != null)
        //        {
        //            bv = bType == from;
        //            if (bv == true) break;

        //            bType = bType.BaseType;
        //            bType = bType != null && bType.FullName == null
        //                ? bType.GetGenericTypeDefinition()
        //                : bType;
        //        }
        //        ThrowIfFalse(bv, $"from[{from.FullName}]类时，to[{to.FullName}]必须继承此类");
        //    }
        //}
        ///// <summary>
        ///// 是否能够作为依赖注入的to类型
        ///// </summary>
        ///// <param name="to"></param>
        ///// <param name="error">不能作为to类型的原因</param>
        ///// <returns>能返回true；否则返回false</returns>
        //private static bool CanAsToType(Type to, out string? error)
        //{
        //    error = null;
        //    if (to.IsInterface) error = $"to不能是接口：{to.FullName}";
        //    else if (to.IsAbstract && to.IsSealed) error = $"to不能是静态类：{to.FullName}";
        //    else if (to.IsAbstract) error = $"to不能是抽象类：{to.FullName}";
        //    return error == null;
        //}
        #endregion
    }

    /// <summary>
    /// 【泛型依赖】注入描述器
    /// </summary>
    /// <typeparam name="T">依赖注入源、目标类型</typeparam>
    public class DIDescriptor<T> : DIDescriptor where T : class
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="key">依赖注入Key值；唯一</param>
        /// <param name="lifetime">依赖注入实现类生命周期</param>
        public DIDescriptor(in string? key, in LifetimeType lifetime) : base(key, typeof(T), lifetime, typeof(T))
        { }

        /// <summary>
        /// 构造方法；Func委托实现to示例构建
        /// </summary>
        /// <param name="key">依赖注入Key值；唯一</param>
        /// <param name="lifetime">依赖注入实现类生命周期</param>
        /// <param name="toFunc">构建to实例的委托方法</param>
        public DIDescriptor(in string? key, in LifetimeType lifetime, Func<IDIManager, T?> toFunc)
            : base(key, typeof(T), lifetime, toFunc == null ? null! : toFunc.Invoke)
        { }
        /// <summary>
        /// 构造方法：源类型和目标实例
        /// </summary>
        /// <param name="key">依赖注入Key值；唯一</param>
        /// <param name="lifetime">依赖注入实现类生命周期</param>
        /// <param name="instance">对象实例；内部转换成 Func委托：manager=>instance</param>
        public DIDescriptor(in string? key, in LifetimeType lifetime, T instance)
            : base(key, typeof(T), lifetime, instance)
        { }
    }

    /// <summary>
    /// 泛型依赖注入描述器；自动获取From和Type值
    /// </summary>
    /// <typeparam name="FromType">依赖注入源类型</typeparam>
    /// <typeparam name="ToType">依赖注入实现类型</typeparam>
    public class DIDescriptor<FromType, ToType> : DIDescriptor where ToType : class, FromType
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="key">依赖注入Key值；唯一</param>
        /// <param name="lifetime">依赖注入实现类生命周期</param>
        /// <remarks>执行【 base(key, typeof(T), lifetime, typeof(T), checkType: false)】不检查类型，有弊端<br />
        /// 1、如直接from来个静态方法、抽象方法，这种情况完全外部自己负责
        /// </remarks>
        public DIDescriptor(in string? key, in LifetimeType lifetime) : base(key, typeof(FromType), lifetime, typeof(ToType))
        { }

        //  在用toFunc没有意义，使用DIDescriptor<T> 构建toFunc即可
        ///// <summary>
        ///// 构造方法；Func委托实现to示例构建
        ///// </summary>
        ///// <param name="key">依赖注入Key值；唯一</param>
        ///// <param name="lifetime">依赖注入实现类生命周期</param>
        ///// <param name="toFunc">构建to实例的委托方法</param>
        //public DIDescriptor(in string? key, in LifetimeType lifetime, Func<IDIManager, ToType?> toFunc)
        //    : base(key, typeof(FromType), lifetime, toFunc == null ? null! : toFunc.Invoke)
        //{ }
    }
}

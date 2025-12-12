using Snail.Abstractions.Dependency.DataModels;
using Snail.Abstractions.Dependency.Interfaces;
using Snail.Dependency.Components;
using Snail.Dependency.Interfaces;
using Snail.Utilities.Collections;

namespace Snail.Dependency;

/// <summary>
/// 依赖注入管理器
/// <para>1、负责进行依赖管理 </para>
/// <para>2、实现依赖反射，实例动态创建等功能 </para>
/// <para>3、对象属性销毁+对象回收功能 </para>
/// </summary>
public sealed class DIManager : Disposable, IDIManager
{
    #region 属性变量

    #region 静态属性变量
    /** 暂时不对外提供，DI实例管理，交给<see cref="Application"/>完成
    /// <summary>
    /// 依赖注入管理器根实例
    /// <para>1、程序启动时，自动将依赖注入信息注册到此实例下 </para>
    /// <para>2、如webapi站点，在程序启动时自动赋值 </para>
    /// </summary>
    public static readonly IDIManager Root;*/
    /// <summary>
    /// 空的管理器实例
    /// </summary>
    public static IDIManager Empty => new DIManager();

    /// <summary>
    /// 当前线程管理器实例
    /// </summary>
    private static readonly AsyncLocal<IDIManager> _current = new();
    /// <summary>
    /// 当前线程上管理器实例；创建一份
    /// <para>1、不存在自动基于<see cref="Empty"/>创建一份新的 </para>
    /// <para>2、内部使用<see cref="AsyncLocal{T}"/>做管理，子线程会自动继承父线程值 </para>
    /// </summary>
    public static IDIManager Current
    {
        //get => _current.Value = _current.Value ?? Root.New();
        get => _current.Value = _current.Value ?? Empty;
        set => _current.Value = value;
    }
    #endregion

    #region 实例级属性变量
    /// <summary>
    /// 注册的依赖注入信息
    /// </summary>
    private readonly LockList<DIDescriptor> _registers = new LockList<DIDescriptor>();
    /// <summary>
    /// 动态注册的依赖注册信息 
    /// <para>1、基于泛型动态构建 </para>
    /// <para>2、基于List动态构建 </para>
    /// </summary>
    private readonly LockList<DIDescriptor> _dynamicRegister = new LockList<DIDescriptor>();
    /// <summary>
    /// 依赖注入实例存储器字典
    /// <para>1、key为依赖信息对象 </para>
    /// <para>2、value为实例存储器，用于生命周期管理对象 </para>
    /// </summary>
    private readonly LockMap<DIDescriptor, ITypeStorager> _storagerMap = new LockMap<DIDescriptor, ITypeStorager>();
    #endregion

    #endregion

    #region 构造方法
    /// <summary>
    /// 静态构造方法
    /// </summary>
    static DIManager()
    {
        //Root = new DIManager();
        //Current = Root;
    }

    /// <summary>
    /// 构造方法：从父级管理器继承依赖注入信息
    /// </summary>
    /// <param name="parent">父级管理器实例</param>
    private DIManager(in DIManager? parent = null)
    {
        //  父级管理器信息继承下来
        if (parent != null)
        {
            parent.EnsureEnable();
            //  1、继承注册信息（原始+动态注册）
            _registers.AddRange(parent._registers);
            _dynamicRegister.AddRange(parent._dynamicRegister);
            //  2、继承实例存储器
            parent._storagerMap.ForEach((di, storager) =>
            {
                ITypeStorager? st = storager.New();
                if (st != null)
                {
                    _storagerMap.Set(di, st);
                }
            });
        }
    }
    #endregion

    #region IDIManager
    /// <summary>
    /// 管理器销毁事件
    /// </summary>
    /// <remarks>使用IDIManager.OnDestroy会报错，这里改成显示实现模式</remarks>
    public event Action? OnDestroy;

    /// <summary>
    /// 基于当前管理器构建新管理实例
    /// <para>1、继承当前管理器中已注册的依赖注入信息 </para>
    /// <para>2、相当于继承当前管理器，创建一个全新的子管理器实例 </para>
    /// </summary>
    /// <returns></returns>
    IDIManager IDIManager.New()
    {
        EnsureEnable();
        return new DIManager(this);
    }

    /// <summary>
    /// 判断指定类型是否注册了
    /// </summary>
    /// <param name="key">依赖注入Key值</param>
    /// <param name="from">依赖注入源类型</param>
    /// <returns></returns>
    bool IDIManager.IsRegistered(string? key, Type from)
    {
        EnsureEnable();
        ThrowIfNull(from);
        return _registers.Any(di => IsFromDescriptor(key, from, di));
    }
    /// <summary>
    /// 注册依赖注入信息
    /// </summary>
    /// <param name="descriptors"></param>
    /// <returns></returns>
    IDIManager IDIManager.Register(IList<DIDescriptor> descriptors)
    {
        EnsureEnable();
        ThrowIfNull(descriptors);
        //  注册后，尝试锁一下【依赖注入】信息的存储器；详细参照方法内部说明
        _registers.AddRange(descriptors);
        foreach (var descriptor in descriptors)
        {
            TryLockStorager(descriptor);
        }
        return this;
    }
    /// <summary>
    /// 尝试注册依赖注入信息；已存在则不注册了
    /// </summary>
    /// <param name="descriptor">依赖注入信息，分析<see cref="DIDescriptor.Key"/>和<see cref="DIDescriptor.From"/>判断是否已经注册过了</param>
    /// <returns>是否注册成功</returns>
    bool IDIManager.TryRegister(DIDescriptor descriptor)
    {
        EnsureEnable();
        ThrowIfNull(descriptor);
        bool bValue = false;
        _registers.GetOrAdd(di => IsFromDescriptor(descriptor.Key, descriptor.From, di), () =>
        {
            bValue = true;
            return descriptor;
        });
        return bValue;
    }
    /// <summary>
    /// 反注册符合条件的依赖注入信息
    /// </summary>
    /// <param name="key">依赖注入Key值</param>
    /// <param name="from">依赖注入源类型</param>
    /// <returns>返回自身，方便链式调用</returns>
    IDIManager IDIManager.Unregister(string? key, Type from)
    {
        EnsureEnable();
        ThrowIfNull(from);
        //  查找符合条件数据，然后移除并销毁saver
        //      临时方法：处理移除依赖注入描述器；能够移除返回true
        bool dealRemoveDescriptor(DIDescriptor di)
        {
            if (IsFromDescriptor(key, from, di) == true)
            {
                _storagerMap.Remove(di, out ITypeStorager? storager);
                storager?.TryDestroy();
                //  di不销毁，避免共享注册的情况下，影响其他manager的使用
                //di.TryDispose();
                return true;
            }
            return false;
        }
        //      移除外部注册+动态注册类型数据
        _registers.RemoveAll(dealRemoveDescriptor);
        _dynamicRegister.RemoveAll(dealRemoveDescriptor);

        return this;
    }

    /// <summary>
    /// 基于依赖注入构建泛型实例
    /// </summary>
    /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
    /// <param name="from">源类型：基于此查找注册信息</param>
    /// <returns>构建完成的实例对象</returns>
    object? IDIManager.Resolve(string? key, Type from)
        => Resolve(key, from, parameters: null);
    /// <summary>
    /// 依赖注入构建实例
    /// </summary>
    /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
    /// <param name="from">源类型：基于此查找注册信息</param>
    /// <param name="parameters">实现类型的构造方法执行时注入的参数信息</param>
    /// <returns>构建完成的实例对象</returns>
    /// <remarks>此方法内部使用，暂不对外</remarks>
    object? IDIManager.Resolve(string? key, Type from, IParameter[] parameters)
         => Resolve(key, from, parameters);
    #endregion

    #region 继承方法
    /// <summary>
    /// 对象释放
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed == false)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
                OnDestroy?.Invoke();
                _registers.TryDispose();
                _dynamicRegister.TryDispose();
                _storagerMap.TryDispose();
            }
            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
            OnDestroy = null;
        }
        //  执行基类回收
        base.Dispose(disposing);
    }
    #endregion

    #region 私有方法

    #region 实例方法，this便捷访问
    /// <summary>
    /// 确保当前实例可用；检测当前Manager是否销毁了
    /// </summary>
    private void EnsureEnable()
        => ObjectDisposedException.ThrowIf(IsDisposed, this);

    /// <summary>
    /// 尝试【依赖注入】存储器
    /// </summary>
    /// <param name="descriptor"></param>
    /// <returns>返回<see cref="DIDescriptor"/>自身，方便链式调用</returns>
    private DIDescriptor? TryLockStorager(DIDescriptor? descriptor)
    {
        //  针对【单例生命周期】，避免注册后不用，在多个子级manager使用时，单例锁不住
        if (descriptor != null && descriptor.Lifetime == LifetimeType.Singleton)
        {
            GetStorager(descriptor);
        }
        return descriptor;
    }
    /// <summary>
    /// 获取依赖注入存储器
    /// </summary>
    /// <param name="descriptor"></param>
    /// <returns></returns>
    private ITypeStorager GetStorager(in DIDescriptor descriptor)
    {
        return _storagerMap.GetOrAdd(descriptor, di =>
        {
            //  后期这里后期把 当前 manager 传递过去，作为manager生命周期绑定使用
            switch (di.Lifetime)
            {
                case LifetimeType.Singleton:
                    return new SingleStorager(manager: this, isScopeSingle: false);
                case LifetimeType.Scope:
                    return new SingleStorager(manager: this, isScopeSingle: true);
                case LifetimeType.Transient:
                    return new TransientStorager(manager: this);
                default:
                    throw new NotSupportedException($"不支持的依赖注入生命周期值：");
            }
        });
    }

    /// <summary>
    /// 依赖注入构建实例
    /// </summary>
    /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
    /// <param name="from">源类型：基于此查找注册信息</param>
    /// <returns>构建完成的实例对象</returns>
    /// <param name="parameters">执行实现类型的构造方法执行时注入的参数信息；一般不用关注</param>
    /// <returns>构建完成的实例对象</returns>
    private object? Resolve(in string? key, in Type from, in IParameter[]? parameters)
    {
        /** 一般不用关注，使用<see cref = "IDIManager" /> 接口即可，仅在需要针对构造方法传入参数时，使用此；暂时作为public对外提供 */
        EnsureEnable();
        ThrowIfNull(from);
        DIDescriptor? descriptor = FindResolveDescriptor(key, from);
        return descriptor != null
            ? BuildInstance(this, descriptor, parameters)
            : null;
    }
    /// <summary>
    /// 查找构建实例的依赖注入信息描述其
    /// </summary>
    /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
    /// <param name="from">源类型：基于此查找注册信息</param>
    /// <returns></returns>
    private DIDescriptor? FindResolveDescriptor(string? key, Type from)
    {
        /* IsFromDescriptor(key, from, di) 会用到 key和from ，无法用in标记做处理，后续想办法*/

        //  查找依赖注入信息；自身->泛型->可枚举对象
        //      查找自身依赖信息，注意descriptor在下面的委托中别作为变量使用，避免来回干扰报错
        DIDescriptor? descriptor = _registers.Get(di => IsFromDescriptor(key, from, di), isDescending: true);
        //      尝试进行泛型类型构建：from可构建的泛型，如List<>是不可构建泛型；List<String>是构建泛型
        if (descriptor == null && from.IsConstructedGenericType)
        {
            /* 实现思路：
             *      IList<String> 分析出 List<>,去注册信息中查找 List<>的实现类，然后动态构建出List<String>的实现类
             *      必须得是可以构建实例的泛型，才能这么干。如type为List<>，则不能搞；List<String>这种可以搞
             *      _dynamicRegister 这个不用从后往前找，只会维护一份出来
             *  查找路径：
             *      1、找动态注册信息中是否存在；若不存在则分析_registers中是否有可为此类型构建泛型注册信息数据
             */
            descriptor = _dynamicRegister.GetOrAdd(di => IsFromDescriptor(key, from, di), isDescending: false, () =>
            {
                //  从后往前查询现有注册数据，尝试构建泛型依赖
                DIDescriptor? dynamic = null;
                var _ = _registers.Get(di =>
                {
                    dynamic = TryCreateGenericDescriptor(key, from, di);
                    return dynamic != null;
                }, isDescending: true);
                //  返回：尝试锁住【依赖注入】信息对应的存储器
                return TryLockStorager(dynamic);
            });
        }
        //      尝试进行可枚举对象类型构建
        if (descriptor == null && from.IsConstructedGenericType && from.IsGenericMakeType(typeof(IEnumerable<>)))
        {
            /*  实现思路：类型必须得是构建实例，找符合条件数据时，找出所有的，然后做配置
             *       找子项是否符合条件时，针对泛型做处理
             *       构建集合注册信息，容易和List这类注册信息混淆，这里完全是为了兼容内置DI的GetServices
             *      _dynamicRegister 这个不用从后往前找，只会维护一份出来
             *       考虑判断 Type是否是IEnumerable；把非这个的忽略掉
             *           public static IEnumerable<object?> GetServices(this IServiceProvider provider, Type serviceType)
                         {
                            Type serviceType2 = typeof(IEnumerable<>).MakeGenericType(serviceType);
                            return (IEnumerable<object>)provider.GetRequiredService(serviceType2);
                         }
             */
            descriptor = _dynamicRegister.GetOrAdd(di => IsFromDescriptor(key, from, di), isDescending: false, () =>
            {
                //  取到集合的泛型参数类型，此类型也可能时泛型
                Type dynamicFrom = from.GenericTypeArguments.Single();
                //  遍历现有注册信息，获取符合FromType的注册信息，泛型时需要动态构建出来；先正序遍历，后面看情况进行倒序
                List<DIDescriptor> descriptors = new List<DIDescriptor>();
                _registers.Foreach(di =>
                {
                    DIDescriptor? dynamic = IsFromDescriptor(key, dynamicFrom, di) == false
                        ? TryCreateGenericDescriptor(key, dynamicFrom, di)
                        : di;
                    dynamic?.AddTo(descriptors);
                });
                //  分析依赖注入信息；用所有注册信息中，生命周期最小的
                Func<IDIManager, object?> toFunc = CreateDynamicEnumerableToFunc(dynamicFrom, descriptors);
                LifetimeType lifetime = descriptors.Any()
                    ? descriptors.Min(item => item.Lifetime)
                    : LifetimeType.Singleton;
                //  返回注册信息：尝试锁住【依赖注入】信息对应的存储器
                return TryLockStorager(new DIDescriptor(key, from, lifetime, toFunc));
            });
        }

        return descriptor;
    }
    #endregion

    #region 依赖注入信息管理、实例构建；静态方法，不牵扯this指向问题
    /// <summary>
    /// 是否是<paramref name="from"/>的依赖注入描述器，是则可用此构建实例
    /// </summary>
    /// <param name="descriptor">依赖注入信息</param>
    /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
    /// <param name="from">依赖注入源类型</param>
    /// <returns>可以返回true；否则返回false</returns>
    private static bool IsFromDescriptor(in string? key, in Type from, in DIDescriptor descriptor)
    {
        ThrowIfNull(from);
        ThrowIfNull(descriptor);
        return descriptor.Key == key && descriptor.From == from;
    }

    /// <summary>
    /// 基于<paramref name="descriptor"/>尝试创建<paramref name="from"/>泛型依赖注入描述器
    /// </summary>
    /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
    /// <param name="from">源类型</param>
    /// <param name="descriptor">参照的依赖注册信息</param>
    /// <returns>构建成功返回新的依赖注册信息；否则返回null</returns>
    private static DIDescriptor? TryCreateGenericDescriptor(in string? key, in Type from, in DIDescriptor descriptor)
    {
        /* 能构建的前提
         *      1、fromType必须是可构建实例的 IsConstructedGenericType
         *          它不能还是泛型类，如下所示，依次继承，只有 MyLockList<String>才能构建实例
         *              List<T> LockList<T> MyLockList<String>
         *      2、descriptor.From是当前from的泛型定义类：如List<String>的泛型定义类为 List<>
         */
        if (descriptor.Key == key && descriptor.To != null && from.IsGenericMakeType(descriptor.From) == true)
        {
            Type to = descriptor.To.MakeGenericType(from.GenericTypeArguments);
            return new DIDescriptor(descriptor.Key, from, descriptor.Lifetime, to);
        }
        return null;
    }
    /// <summary>
    /// 创建动态List的实现委托
    /// </summary>
    /// <param name="from">List的泛型类型</param>
    /// <param name="descriptors"></param>
    /// <returns></returns>
    private static Func<IDIManager, object?> CreateDynamicEnumerableToFunc(in Type from, in List<DIDescriptor> descriptors)
    {
        //  基础数据准备：提前准备好，方便委托调用时使用
        //      构建List<fromType>类型 ，用作返回值
        Type listType = typeof(_DynamicEnumerableList<>).MakeGenericType(from);
        MethodBase addMethod = listType.GetMethod("AddDynamicInstance", BindingFlags.Instance | BindingFlags.Public)!;
        //  构建返回委托：后期考虑做lambda表达式构建，不用每次Activator.CreateInstance
        //      注册信息转成Array，外部变化不再影响当前委托
        DIDescriptor[]? arrays = descriptors?.ToArray();
        //      构建委托，内部遍历依赖注入信息，逐个构建实例，并加到返回list中
        return manager =>
        {
            var objs = Activator.CreateInstance(listType);
            if (arrays?.Any() == true)
            {
                foreach (DIDescriptor di in arrays)
                {
                    //  addMethod内部会自动过滤掉instance为null的数据；
                    //  这里manager不要使用使用委托传递过来的，而不是this（已经做成了static方法了，忽略掉）
                    object? instace = BuildInstance((DIManager)manager, di, parameters: null);
                    addMethod.Invoke(objs, [instace]);
                }
            }
            return objs!;
        };
    }

    /// <summary>
    /// 依赖注入构建实例
    /// </summary>
    /// <param name="manager">依赖注入管理器</param>
    /// <param name="descriptor">源类型：基于此查找注册信息</param>
    /// <param name="parameters">执行实现类型的构造方法执行时注入的参数信息；一般不用关注</param>
    /// <returns>构建完成的实例对象</returns>
    private static object? BuildInstance(in DIManager manager, in DIDescriptor descriptor, in IParameter[]? parameters)
    {
        /** 这个方法为什么要做成static，因为<see cref="CreateDynamicEnumerableToFunc"/>也会调用此方法，此时不能使用this对象，而是要用实际的执行是传入的manager值，避免生命周期出问题*/
        ThrowIfNull(manager);
        ThrowIfNull(descriptor);
        //  基于存储器，查找是否已缓存实例；无则全新构建，并加入缓存中
        ITypeStorager storager = manager.GetStorager(descriptor);
        object? instance = storager.GetInstace();
        if (instance == null)
        {
            //  基于To类型反射构建；基于代理器，缓存构建相关信息，加快后续构建速度
            if (descriptor.ToFunc == null)
            {
                TypeProxy proxy = TypeProxy.GetProxy(descriptor.To!);
                instance = proxy.BuildInstance(manager, parameters);
            }
            //  基于委托构建实例，直接执行即可     
            else
            {
                instance = descriptor.ToFunc.Invoke(manager);
            }
            storager.SaveInstace(instance);
        }
        return instance;
    }
    #endregion

    #endregion

    #region 私有类型
    /// <summary>
    /// 私有类：动态List集合类
    /// <para>配合<see cref="CreateDynamicEnumerableToFunc"/>使用 </para>
    /// <para>动态构建实例添加到集合中时，也可以反射List的Add方法，但容易受List对象变化影响；这里直接封装一个类，搞一个唯一方法 </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class _DynamicEnumerableList<T> : List<T>
    {
        /// <summary>
        /// 添加动态实例
        /// <para>1、内部中转List的Add方法 </para>
        /// <para>2、为null的数据，自动忽略不添加 </para>
        /// </summary>
        /// <param name="value"></param>
        public void AddDynamicInstance(in T value) => value?.AddTo(this);
    }
    #endregion
}

using Snail.Utilities.Collections;
using Snail.Utilities.Threading.Extensions;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Snail.Utilities.Common;

/// <summary>
/// 运行时上下文；用于跨方法、类共享数据
/// <para>1、使用RunContext.Current逻辑；实现主子线程上下文共享机制；多线程操作时线程安全 </para>
/// <para>2、整体框架运行中，需要动态获取运行时上下文时，会强制始终此对象 </para>
/// <para>3、提供可脱离线程共享机制下的上下文对象，RunContext.New、、 </para>
/// </summary>
public sealed class RunContext
{
    #region 属性变量
    /// <summary>
    /// Id生成器
    /// </summary>
    private static Func<string>? _idGenerator = null;
    /// <summary>
    /// 上下文实例：线程同步，同步子线程的前提条件
    /// </summary>
    private static readonly AsyncLocal<RunContext> _context = new();
    /// <summary>
    /// 上下文数据集合
    /// </summary>
    /// <remarks>使用LList实现线程安全读写</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly LockList<ContextItem> _items = new();

    /// <summary>
    /// 当前上下文对象；不存在则使用构建一个RunContext实例
    /// </summary>
    public static RunContext Current => _context.GetOrSetValue(() => new RunContext());
    /// <summary>
    /// 空的运行时上下文
    /// <para>每次调用全新构建实例</para>
    /// </summary>
    public static RunContext Empty => new();
    /// <summary>
    /// 上下文Id，每个上下文实例唯一
    /// <para>等效于分布式系统中的 span_id </para>
    /// </summary>
    public string ContextId { private init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="parent">父级上下文对象，非null时从父级copy已有信息</param>
    public RunContext(RunContext? parent = null)
    {
        //  若无id生成器，则使用guid
        ContextId = _idGenerator == null
            ? Guid.NewGuid().ToString().Replace("-", string.Empty).ToLower()
            : (_idGenerator() ?? throw new ApplicationException("_idGenerator：返回id值为null；无法构建上下文实例"));
        //  继承上下文中的参数信息
        if (parent != null)
        {
            _items.AddRange(parent._items.ToList());
        }
    }
    #endregion

    #region 公共方法

    #region RunContext 自身
    /// <summary>
    /// 配置 运行时上下文 
    /// <para>1、确保在程序启动时做好配置 </para>
    /// <para>2、确保不重复调用 </para>
    /// </summary>
    /// <param name="idGenerator">主键生成器；不能为null</param>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void Config(Func<string> idGenerator)
    {
        if (_idGenerator != null)
        {
            string msg = $"不能重复调用{nameof(Config)}方法";
            throw new ApplicationException(msg);
        }
        ThrowIfNull(idGenerator);
        _idGenerator = idGenerator;
    }
    /// <summary>
    /// 构建新的运行时上下文
    /// <para>1、构建一个空的运行时上下文设置给<see cref="Current"/> </para>
    /// </summary>
    /// <returns></returns>
    public static RunContext New()
    {
        _context.Value = new RunContext();
        return _context.Value;
    }
    #endregion

    #region _Items属性维护
    /// <summary>
    /// 添加数据；若已经存在相同key类型数据，先替换
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">数据的key值；同一个类型，基于key做唯一区分。</param>
    /// <param name="obj">数据实例</param>
    /// <returns>自身，方便链式调用</returns>
    public RunContext Add<T>(string? key, T obj)
    {
        Type type = typeof(T);
        ContextItem item = new(key, type, obj);
        _items.Replace(item => item.Key == key && item.Type == type, item);
        return this;
    }
    /// <summary>
    /// 获取数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">数据的key值；同一个类型，基于key做唯一区分。</param>
    /// <returns>存在返回指定数据；否则返回类型默认值</returns>
    public T? Get<T>(string? key = null)
    {
        Type type = typeof(T);
        ContextItem? item = _items.Get(item => item.Key == key && item.Type == type, false);
        return item == null ? default : (T?)item.Value;
    }
    /// <summary>
    /// 获取数据；若不存在则添加
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">数据的key值；同一个类型，基于key做唯一区分。</param>
    /// <param name="addFunc"></param>
    /// <returns></returns>
    public T? GetOrAdd<T>(string? key, Func<string?, T> addFunc)
    {
        Type type = typeof(T);
        ThrowIfNull(addFunc);
        ContextItem? item = _items.GetOrAdd(item => item.Key == key && item.Type == type, false, () =>
        {
            T @new = addFunc(key);
            return @new == null ? null : new ContextItem(key, type, @new);
        });
        return item == null ? default : (T?)item.Value;
    }
    /// <summary>
    /// 是否存在指定类型数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">数据的key值；同一个类型，基于key做唯一区分。</param>
    /// <returns></returns>
    public Boolean Exists<T>(string? key = null)
    {
        Type type = typeof(T);
        return _items.Any(item => item.Key == key && item.Type == type);
    }
    /// <summary>
    /// 移除数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">数据的key值；同一个类型，基于key做唯一区分。</param>
    public void Remove<T>(string? key = null)
    {
        Type type = typeof(T);
        _items.RemoveAll(item => item.Key == key && item.Type == type);
    }

    /// <summary>
    /// 遍历数据
    /// </summary>
    /// <param name="each">key，type，实例数据</param>
    public void ForEach(Action<string?, Type, Object?> each)
    {
        ThrowIfNull(each);
        _items.Foreach(item => each(item.Key, item.Type, item.Value));
    }
    #endregion

    #endregion

    #region 私有类型
    /// <summary>
    /// 上下文数据项
    /// </summary>
    /// <param name="Key">数据项key值</param>
    /// <param name="Type">类型对象</param>
    /// <param name="Value">数据实例</param>
    private record ContextItem(string? Key, Type Type, Object? Value);
    #endregion
}

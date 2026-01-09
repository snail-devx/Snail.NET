using Snail.Abstractions.Logging.Extensions;
using Snail.Abstractions.Setting;
using Snail.Abstractions.Setting.Delegates;
using Snail.Abstractions.Web.Extensions;
using Snail.Aspect.Common.Attributes;
using Snail.Dependency;
using Snail.Setting;
using Snail.Utilities.Common.Utils;
using System.Diagnostics;
using System.Runtime.Loader;

namespace Snail;
/// <summary>
/// 应用程序：启动<see cref="IApplication.Run"/>时，执行顺序如下
/// <para>1、内置服务注册（在app构造方法执行）</para>
/// <para>2、扫描程序集，扫描<see cref="Type"/>完成特定<see cref="Attribute"/>分析注册，触发<see cref="IApplication.OnScan"/>事件</para>
/// <para>3、读取应用程序配置，外部通过<see cref="ISettingManager.Use(in bool, in string, SettingUserDelegate)"/>使用配置</para>
/// <para>4、自定义服务注册；触发<see cref="IApplication.OnRegister"/>事件，用于完成个性化di替换等</para>
/// <para>5、自定义服务注册完成；触发<see cref="IApplication.OnRegistered"/>事件，用于进行一些服务、组件预热</para>
/// <para>6、服务启动；触发<see cref="IApplication.OnRun"/>，运行WebApp应用</para>
/// </summary>
public class Application : IApplication
{
    #region 属性变量
    /// <summary>
    /// 依赖注入根服务
    /// </summary>
    protected IDIManager RootServices { get; private init; }
    /// <summary>
    /// 应用程序配置管理器
    /// </summary>
    protected ISettingManager Setting { get; private init; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    public Application()
    {
        RunContext.New();
        //  1、内置配置、实例 初始化
        Setting = SettingFactory.Create();
        RootServices = (DIManager.Current = DIManager.Empty);
        //  2、内置依赖注入实例
        RootServices.Register<IApplication>(LifetimeType.Singleton, this);
        //  3、强制内置服务初始化
        this.AddDIService();/*                  依赖注入服务：进行IoC相关功能实现*/
        this.AddLogServices();/*                日志服务：检测必备组件完整性*/
    }
    #endregion

    #region IApplication
    /// <summary>
    /// 事件：应用扫描时
    /// <para>1、触发时机：<see cref="Run"/>时，首先执行程序扫描</para>
    /// <para>2、用途说明：用于实现组件自动扫描注册</para>
    /// <para>3、参数说明：</para>
    /// <para>- <see cref="IDIManager"/> 为根服务注入实例</para>
    /// <para>- <see cref="Type"/> 为当前正在扫描的类型</para>
    /// <para>- <see cref="Attribute"/> 当前扫描类型的特性标签</para>
    /// </summary>
    /// <remarks>
    /// 注意事项：
    /// <para>1、只有打上<see cref="AppScanAttribute"/>标签的<see cref="Assembly"/>才会被扫描</para>
    /// <para>2、只有有<see cref="Attribute"/>标签的<see cref="Type"/>才会被扫描</para>
    /// </remarks>
    public event Action<IDIManager, Type, ReadOnlySpan<Attribute>>? OnScan;
    /// <summary>
    /// 事件：服务注册时
    /// <para>1、触发时机：系统内置依赖注入注册完成后</para>
    /// <para>2、用途说明：用于外部覆盖内置依赖注入配置完成个性化配置</para>
    /// <para>3、参数说明：</para>
    /// <para>- <see cref="IDIManager"/> 为根服务注入实例</para>
    /// </summary>
    /// <remarks>
    /// 注意事项：
    /// <para>1、不要在此事件中进行对象构建，否则可能导致依赖注入关系错误</para>
    /// </remarks>
    public event Action<IDIManager>? OnRegister;
    /// <summary>
    /// 事件：服务注册完成时
    /// <para>1、触发时机：事件<see cref="OnRegister"/>之后执行</para>
    /// <para>2、用途说明：用于进行服务预热处理，如提前构建实例</para>
    /// <para>3、参数说明：</para>
    /// <para>- <see cref="IDIManager"/> 为根服务注入实例</para>ara>
    /// </summary>
    public event Action<IDIManager>? OnRegistered;
    /// <summary>
    /// 事件：程序运行时触发
    /// <para>1、触发时机：app配置完成，准备启动前触发</para>
    /// <para>2、用途说明：用于启动依赖的相关服务，如启动mq接收消息</para>
    /// <para>3、参数说明：</para>
    /// <para>- <see cref="IDIManager"/> 为根服务注入实例</para>
    /// </summary>
    public event Action<IDIManager>? OnRun;
    /// <summary>
    /// 事件：程序停止时
    /// <para>1、触发时机：程序关闭时触发</para>
    /// <para>2、用途说明：实现程序关闭前的资源销毁等处理，若内部为异步任务，则返回Task，否则返回null</para>
    /// </summary>
    public event Func<Task?>? OnStop;

    /// <summary>
    /// 应用配置 管理器
    /// </summary>
    ISettingManager IApplication.Setting => Setting;
    /// <summary>
    /// 依赖注入根服务
    /// </summary>
    IDIManager IApplication.RootServices => RootServices;
    /// <summary>
    /// 当前作用域的依赖注入服务
    /// <para>1、实现作用域之间实例隔离</para>
    /// <para>2、如一个ASP.NET Core的HTTP请求，就是一个全新的作用域</para>
    /// </summary>
    IDIManager IApplication.ScopeServices => DIManager.TrySetCurrent(RootServices.New);

    /// <summary>
    /// 运行应用程序
    /// </summary>
    public void Run()
    {
        //  1、启用应用程序构建
        StartBuild();
        //  2、启动应用
        OnRun?.Invoke(RootServices);
        OnRun = null;
    }
    /// <summary>
    /// 停止应用程序
    /// </summary>
    /// <returns>异步任务；若内部存在异步处理，则返回Task，方便外部等待优雅退出</returns>
    public Task? Stop()
    {
        if (OnStop == null)
        {
            return null;
        }
        //  触发停止事件
        List<Task> tasks = [];
        foreach (Func<Task?> stop in OnStop.GetInvocationList().Cast<Func<Task?>>())
        {
            stop.Invoke()?.AddTo(tasks);
        }
        return tasks.Count > 1 ? Task.WhenAll(tasks) : tasks.FirstOrDefault();
    }
    #endregion

    #region 继承方法
    //  暂时不对外开放，意义不大
    ///// <summary>
    ///// 启动应用程序
    ///// <para>1、应用程序构建：执行<see cref="StartBuild"/>方法，完成应用程序构建逻辑</para>
    ///// <para>2、触发<see cref="OnRun"/>事件，完成程序程序</para>
    ///// </summary>
    //protected virtual void Start()
    //{
    //    //  1、启用应用程序构建
    //    StartBuild();
    //    //  2、启动应用
    //    OnRun?.Invoke(RootServices);
    //    OnRun = null;
    //}
    /// <summary>
    /// 启动应用构建
    /// <para>1、启动应用扫描：执行<see cref="StartScan"/>完成内置服务注册</para>
    /// <para>2、应用配置构建：加载 App_Setting 下的应用程序配置文件，执行<see cref="ISettingManager.Run"/></para>
    /// <para>3、启动服务注册：触发<see cref="OnRegister"/>、<see cref="OnRegistered"/>事件，完成自定义服务注册和内置服务替换</para>
    /// </summary>
    protected virtual void StartBuild()
    {
        //  1、启动服务扫描
        if (OnScan != null)
        {
            StartScan(this, OnScan);
            OnScan = null;
        }
        //  2、应用配置构建
        ((IApplication)this).Setting.Run();
        //  3、服务注册
        OnRegister?.Invoke(RootServices);
        OnRegister = null;
        OnRegistered?.Invoke(RootServices);
        OnRegistered = null;
    }
    /// <summary>
    /// 启动应用扫描
    /// <para>注意事项： </para>
    /// <para>1、不会执行<see cref="Assembly.LoadFile(string)"/>加载程序集，只会读取<see cref="AssemblyLoadContext.Default"/>已经加载好的程序集   </para>
    /// <para>2、已加载程序集，需要和<see cref="AppContext.BaseDirectory"/>同目录；否则不会识别（如微软自带的一些依赖程序集，扫描没有意义）   </para>
    /// <para>3、只扫描<see cref="AppScanAttribute"/>标记的程序集，只扫描有<see cref="Attribute"/>标记的<see cref="Type"/> </para>
    /// <para>4、按照<see cref="AppScanAttribute.Order"/>、<see cref="Assembly.FullName"/>升序扫描后，逐个进行type扫描，回调callback委托方法 </para>
    /// </summary>
    /// <param name="app">应用程序实例，会自动扫描对应的<see cref="IApplication.RootDirectory"/>目录下程序集做补偿</param>
    /// <param name="callback">扫描程序集后，遍历<see cref="Type"/>的回调方法</param>
    protected virtual void StartScan(IApplication app, in Action<IDIManager, Type, ReadOnlySpan<Attribute>> callback)
    {
        ThrowIfNull(callback);
        //  获取需要扫描的程序集
        IDictionary<Assembly, AppScanAttribute> map = GetScanAssemblies(AssemblyLoadContext.Default, extScanDir: app.RootDirectory);
        if (map?.Any() != true)
        {
            return;
        }
        //  按照扫描顺序分组升序，相同组内部，按照文件名称升序：检索程序集，整理需要进行遍历Type的程序集；后续这里可以考虑做多线程，提升匹配性能
        Dictionary<int, List<Tuple<Type, Attribute[]>>> typeMap = [];
        Parallel.ForEach(map.GroupBy(kv => kv.Value.Order), group =>
        {
            //  以order做key始终不会重复，直接add到字典中，但注意多线程影响
            List<Tuple<Type, Attribute[]>> types = [];
            {
                lock (typeMap)
                {
                    typeMap[group.Key] = types;
                }
            }
            //  遍历按照文件名称升序取类型；先不使用多线程
            foreach (var (assembly, _) in group.OrderBy(ass => ass.Key.FullName))
            {
#if DEBUG
                Debug.WriteLine($"-->扫描程序集 Order:{group.Key} FullName:{assembly.FullName} Location:{assembly.Location}");
#endif
                GetTypes(assembly, types);
            }
        });
        //  遍历类型类型数据；只有有特性标签的Type才会触发回调；避免一些数据类型等平白无故触发了
        foreach (var (_, types) in typeMap.OrderBy(kv => kv.Key))
        {
            foreach (var (type, attrs) in types)
            {
                if (attrs.Any() == true)
                {
                    ReadOnlySpan<Attribute> attrSpan = new([.. attrs]);
                    callback.Invoke(RootServices, type, attrSpan);
                }
            }
        }
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 获取需要扫描的程序集 
    /// <para>1、仅返回打了<see cref="AppScanAttribute"/>标签的程序集  </para>
    /// <para>2、<paramref name="extScanDir"/>下需扫描程序集，未在当前<paramref name="context"/>下加载时，自动加载进来 </para>
    /// </summary>
    /// <param name="context">程序集加载上下文</param>
    /// <param name="extScanDir">扩展扫描目录（存在才会扫描）；分析此目录直属dll，不会递归查找；传null则不额外扫描指定目录下的程序集</param>
    /// <returns></returns>
    private static Dictionary<Assembly, AppScanAttribute> GetScanAssemblies(AssemblyLoadContext context, in string? extScanDir = null)
    {
        /*  要确保扫描的程序集和传入context；否则会会导致 is、强制类型转换 等实现，因为不在同一个context下  */
        Dictionary<Assembly, AppScanAttribute> map = [];
        //  查看指定目录下程序集是否在AssemblyLoadContext加载；仅判断有AppScanAttribute标签的
        if (Directory.Exists(extScanDir) == true)
        {
            //  后期这里考虑使用多线程进行加载，先判断出需要Scan的程序集，再遍历处理加到map和、load2Context中
            foreach (var file in Directory.GetFiles(extScanDir, "*.dll"))
            {
                if (IsIgnoreScanAssembly(file) == true)
                {
                    continue;
                }
                //  不是忽略程序集，才扫描分析 加载程序集，若判定为无需扫描，则忽略掉
                Assembly? assembly = DelegateHelper.Run(Assembly.LoadFile, file);
                if (assembly == null || assembly.GetCustomAttribute<AppScanAttribute>() == null)
                {
                    continue;
                }
                //  是【需要加载】的程序集，判断是否已经在上下文上了；不在则补偿加载一下
                if (context.Assemblies.Any(ass => ass.FullName == assembly.FullName) == false)
                {
                    context.LoadFromAssemblyName(assembly.GetName());
                }
            }
        }
        //  遍历【context】判断哪些程序集需要加载；后期考虑多线程
        foreach (var ass in context.Assemblies)
        {
            /**  AssemblyLoadContext.Default 仅包含【显式】使用过的程序集
                 *      如项目添加了引用，未在源代码中使用过也不会加载都上下文
                 *      显式 使用是可传递的，如在A使用了B，B显式使用了C，则A作为入口程序启动时，上下文上会有C
                 * 输出示例：
                    System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e
                    testhost, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Runtime, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    Microsoft.TestPlatform.CoreUtilities, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Diagnostics.Tracing, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Diagnostics.Debug, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Runtime.Extensions, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Diagnostics.Process, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.ComponentModel.Primitives, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    Microsoft.TestPlatform.PlatformAbstractions, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Collections, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    Microsoft.TestPlatform.CrossPlatEngine, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
                    Microsoft.TestPlatform.CommunicationUtilities, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    Microsoft.VisualStudio.TestPlatform.ObjectModel, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    Microsoft.VisualStudio.TestPlatform.Common, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed
                    System.Runtime.Serialization.Formatters, Version=8.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Collections.Concurrent, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Diagnostics.TraceSource, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Threading, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.IO.FileSystem, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Runtime.InteropServices, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Memory, Version=9.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
                    Microsoft.Win32.Primitives, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Threading.ThreadPool, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Net.Primitives, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Collections.NonGeneric, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Net.Sockets, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Private.Uri, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Threading.Overlapped, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Runtime.Intrinsics, Version=9.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
                    System.Diagnostics.DiagnosticSource, Version=9.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
                    System.Linq.Expressions, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Runtime.Numerics, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Linq, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.ComponentModel.TypeConverter, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.ObjectModel, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Runtime.Serialization.Primitives, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Data.Common, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Xml.ReaderWriter, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Private.Xml, Version=9.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
                    System.ComponentModel, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Reflection.Emit.ILGeneration, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Reflection.Emit.Lightweight, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Reflection.Primitives, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    Anonymously Hosted DynamicMethods Assembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
                    System.Numerics.Vectors, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    Microsoft.VisualStudio.TestPlatform.Common.resources, Version=15.0.0.0, Culture=zh-Hans, PublicKeyToken=b03f5f7f11d50a3a
                    System.Runtime.Loader, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Reflection.Metadata, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.IO.MemoryMappedFiles, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Collections.Immutable, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Text.Encoding.Extensions, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    NUnit3.TestAdapter, Version=4.6.0.0, Culture=neutral, PublicKeyToken=4cb40d35494691ac
                    System.Resources.ResourceManager, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    nunit.engine.api, Version=3.0.0.0, Culture=neutral, PublicKeyToken=2638cd05610744eb
                    System.Runtime.InteropServices.RuntimeInformation, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Xml.XDocument, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Private.Xml.Linq, Version=9.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
                    System.Threading.Thread, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    nunit.engine, Version=3.18.1.0, Culture=neutral, PublicKeyToken=2638cd05610744eb
                    nunit.engine.core, Version=3.18.1.0, Culture=neutral, PublicKeyToken=2638cd05610744eb
                    Microsoft.VisualStudio.Debugger.Runtime.NetCoreApp, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    Microsoft.IntelliTrace.TelemetryObserver.Common, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    Microsoft.IntelliTrace.TelemetryObserver.CoreClr, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Reflection, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Reflection.Extensions, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    testcentric.engine.metadata, Version=2.0.0.0, Culture=neutral, PublicKeyToken=6fe0a02d2036aa1d
                    Snail.Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
                    nunit.framework, Version=4.2.2.0, Culture=neutral, PublicKeyToken=2638cd05610744eb
                    System.Diagnostics.TextWriterTraceListener, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    System.Console, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    Snail.Abstractions, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
                    System.Text.RegularExpressions, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    Snail, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
                 */
            var attr = IsIgnoreScanAssembly(ass.Location) ? null : ass.GetCustomAttribute<AppScanAttribute>();
            if (attr != null)
            {
                map[ass] = attr;
            }
        }

        return map;
    }
    /// <summary>
    /// 判断是否是需要忽略的扫描的程序集
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private static bool IsIgnoreScanAssembly(string file)
    {
        //  某些动态程序集，file为空字符串；此时强制忽略；否则按照文件名称排除
        if (file?.Length > 0)
        {
            file = new FileInfo(file).Name;
            //  把一些程序集强制剔除掉，没有意义，也不是自己写的，不可能包含 AppScan 特性标签
            return file.StartsWith("Microsoft.")
                || file.StartsWith("Newtonsoft.");
        }
        return true;
    }
    /// <summary>
    /// 按程序集分析类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="types">分析出来的类型，放到此集合中</param>
    private static void GetTypes(Assembly assembly, in IList<Tuple<Type, Attribute[]>> types)
    {
        //  同程序集下的程序集，做一下属性判断；Aspect标记类型，放到最后；
        List<Tuple<Type, Attribute[]>> aspectTypes = [];
        foreach (Type type in assembly.GetTypes())
        {
            bool isAspectType = false;
            Attribute[] attrs = [.. type.GetCustomAttributes()
                .Where(attr =>
                {
                    /** 剔除掉系统自带的编译属性；即时没有任何属性标记，编译后也会自带一些特性标签
                     *  1、接口自带：
                     *      {System.Runtime.CompilerServices.NullableContextAttribute}
                     *  2、类自带
                     *      {System.Runtime.CompilerServices.NullableContextAttribute}
                     *      {System.Runtime.CompilerServices.NullableAttribute}
                     */
                    Type type = attr.GetType();
                    if (type == typeof(AspectAttribute))
                    {
                        isAspectType = true;
                        return false;
                    }
                    string? typeName = type.FullName?? type.GetGenericTypeDefinition()?.FullName;
                    return typeName != null && typeName.StartsWith("System.Runtime.CompilerServices") == false;
                })
            ];
            var item = new Tuple<Type, Attribute[]>(type, attrs);
            item.AddTo(isAspectType ? aspectTypes : types);
        }
        types.TryAddRange(aspectTypes);
    }
    #endregion
}
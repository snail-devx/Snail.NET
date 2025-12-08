using Snail.Abstractions.Common.Delegates;
using Snail.Abstractions.Setting;
using Snail.Abstractions.Setting.Delegates;
using Snail.Aspect.Common.Attributes;
using Snail.Dependency;
using Snail.Setting;
using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Common.Extensions;
using Snail.Utilities.Common.Utils;
using System.Diagnostics;
using System.Runtime.Loader;

namespace Snail
{
    /// <summary>
    /// 应用程序；泛型，支持<see cref="OnBuild"/>事件
    /// </summary>
    public abstract class Application<App> : IApplication where App : class
    {
        #region 事件、属性变量
        /// <summary>
        /// 事件：配置Web应用时； <br />
        ///     1、触发时机：<see cref="OnRun"/>之前执行 <br />
        ///     2、用途说明：区别于OnRun，暴露构建完的app实例做一些专有配置，如WebApi配置app中间件等 
        /// </summary>
        public event Action<App>? OnBuild;

        /// <summary>
        /// 应用程序配置管理器
        /// </summary>
        protected ISettingManager Setting => DI.ResolveRequired<ISettingManager>();
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        public Application()
        {
            //  1、内置配置、实例 初始化
            //      依赖注入管理器 
            DI = DIManager.Empty;
            DIManager.Current = DI;
            //      【应用程序配置】管理器配置
            SettingFactory.Config(() => new SettingManager());
            //  2、内置依赖注入实例
            DI.Register<IApplication>(LifetimeType.Singleton, this);
            DI.Register<ISettingManager>(LifetimeType.Singleton, manager => SettingFactory.Create());
            //      依赖注入管理器，作为scope构建
            DI.Register<IDIManager>(LifetimeType.Scope, manager => DIManager.Current);
            //  3、强制内置服务初始化
            this.AddDIService();
            RunContext.New();
        }
        #endregion

        #region IApplication
        /// <summary>
        /// 事件：应用扫描时 <br />
        ///     1、触发时机：<see cref="IApplication.Run"/>时，首先执行程序扫描 <br />
        /// 注意事项： <br />
        ///     1、只有打上<see cref="AppScanAttribute"/>标签的<see cref="Assembly"/>才会被扫描 <br />
        ///     2、只有有<see cref="Attribute"/>标签的<see cref="Type"/>才会被扫描 <br />
        /// </summary>
        public event AppScanDelegate? OnScan;
        /// <summary>
        /// 事件：服务注册时 <br />
        ///     1、触发时机：系统内置依赖注入注册完成后 <br />
        ///     2、用于外部覆盖内置依赖注入配置完成个性化配置 <br />
        /// </summary>
        public event Action? OnRegister;
        /// <summary>
        /// 事件：程序运行时触发 <br />
        ///     1、触发时机：app配置完成，准备启动前触发 <br />
        ///     2、用于启动依赖的相关服务，如启动mq接收消息
        /// </summary>
        public event Action? OnRun;

        /// <summary>
        /// 依赖注入 管理器
        /// </summary>
        public IDIManager DI { private init; get; }
        /// <summary>
        /// 应用配置 管理器
        /// </summary>
        ISettingManager IApplication.Setting => Setting;
        /// <summary>
        /// 运行应用程序，执行顺序
        /// <para>1、内置服务注册（在app构造方法执行）</para>
        /// <para>2、扫描程序集，扫描<see cref="Type"/>完成特定<see cref="Attribute"/>分析注册，触发<see cref="IApplication.OnScan"/>事件</para>
        /// <para>3、读取应用程序配置，外部通过<see cref="ISettingManager.Use(in bool, in string, SettingUserDelegate)"/>使用配置</para>
        /// <para>4、自定义服务注册；触发<see cref="IApplication.OnRegister"/>事件，用于完成个性化di替换等</para>
        /// <para>5、应用构建；触发<see cref="Application{T}.OnBuild"/> 完成应用启动前自定义配置</para>
        /// <para>6、服务启动；触发<see cref="IApplication.OnRun"/>，运行WebApp应用</para>
        /// </summary>
        void IApplication.Run() => Run(appBuilder: null);
        #endregion

        #region 继承方法
        /// <summary>
        /// 运行程序
        /// </summary>
        /// <param name="appBuilder">应用程序构建委托</param>
        protected void Run(Func<App>? appBuilder)
        {
            /*  执行应用程序集扫描，然后触发各个层级事件    */
            //  1、自定义【依赖注入】、服务信息注册；触发服务注册事件，交给外部进行自定义服务注册
            if (OnScan != null)
            {
                StartScan(this, OnScan.Invoke);
            }
            ((IApplication)this).Setting.Run();
            OnRegister?.Invoke();
            //  2、程序启动；启动自定义服务，触发【OnRun】事件，交给外部进行自定义服务启动
            if (appBuilder != null && OnBuild != null)
            {
                var app = appBuilder.Invoke();
                ThrowIfNull(app, $"{nameof(appBuilder)}委托返回的App对象为null");
                OnBuild.Invoke(app);
            }
            OnRun?.Invoke();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 启动应用扫描
        /// 备注：  <br />
        ///     1、不会执行<see cref="Assembly.LoadFile(string)"/>加载程序集，只会读取<see cref="AssemblyLoadContext.Default"/>已经加载好的程序集  <br />
        ///     2、已加载程序集，需要和<see cref="AppContext.BaseDirectory"/>同目录；否则不会识别（如微软自带的一些依赖程序集，扫描没有意义）  <br />
        ///     3、只扫描<see cref="AppScanAttribute"/>标记的程序集，只扫描有<see cref="Attribute"/>标记的<see cref="Type"/>
        ///     4、按照<see cref="AppScanAttribute.Order"/>、<see cref="Assembly.FullName"/>升序扫描后，逐个进行type扫描，回调callback委托方法
        /// </summary>
        /// <param name="app">应用程序实例，会自动扫描对应的<see cref="IApplication.RootDirectory"/>目录下程序集做补偿</param>
        /// <param name="callback">扫描程序集后，遍历<see cref="Type"/>的回调方法</param>
        protected static void StartScan(IApplication app, in AppScanDelegate callback)
        {
            ThrowIfNull(callback);
            //  获取需要扫描的程序集
            IDictionary<Assembly, AppScanAttribute> map = GetScanAssemblies(AssemblyLoadContext.Default, extScanDir: app.RootDirectory);
            if (map?.Any() != true)
            {
                return;
            }
            //  按照扫描顺序分组升序，相同组内部，按照文件名称升序：检索程序集，整理需要进行遍历Type的程序集；后续这里可以考虑做多线程，提升匹配性能
            Dictionary<int, List<Tuple<Type, Attribute[]>>> typeMap = new Dictionary<int, List<Tuple<Type, Attribute[]>>>();
            Parallel.ForEach(map.GroupBy(kv => kv.Value.Order), group =>
            {
                //  以order做key始终不会重复，直接add到字典中，但注意多线程影响
                List<Tuple<Type, Attribute[]>> types = new List<Tuple<Type, Attribute[]>>();
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
                        ReadOnlySpan<Attribute> attrSpan = new ReadOnlySpan<Attribute>(attrs.ToArray());
                        callback.Invoke(type, attrSpan);
                    }
                }
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 获取需要扫描的程序集 <br />
        ///     1、仅返回打了<see cref="AppScanAttribute"/>标签的程序集 <br />
        ///     2、<paramref name="extScanDir"/>下需扫描程序集，未在当前<paramref name="context"/>下加载时，自动加载进来
        /// </summary>
        /// <param name="context">程序集加载上下文</param>
        /// <param name="extScanDir">扩展扫描目录（存在才会扫描）；分析此目录直属dll，不会递归查找；传null则不额外扫描指定目录下的程序集</param>
        /// <returns></returns>
        private static IDictionary<Assembly, AppScanAttribute> GetScanAssemblies(AssemblyLoadContext context, in string? extScanDir = null)
        {
            /*  要确保扫描的程序集和传入context；否则会会导致 is、强制类型转换 等实现，因为不在同一个context下  */
            IDictionary<Assembly, AppScanAttribute> map = new Dictionary<Assembly, AppScanAttribute>();
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
            List<Tuple<Type, Attribute[]>> aspectTypes = new List<Tuple<Type, Attribute[]>>();
            foreach (Type type in assembly.GetTypes())
            {
                bool isAspectType = false;
                Attribute[] attrs = type.GetCustomAttributes()
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
                        string? typeName = type.FullName
                            ?? type.GetGenericTypeDefinition()?.FullName;
                        return typeName != null && typeName.StartsWith("System.Runtime.CompilerServices") == false;
                    })
                    .ToArray();
                var item = new Tuple<Type, Attribute[]>(type, attrs);
                item.AddTo(isAspectType ? aspectTypes : types);
            }
            types.TryAddRange(aspectTypes);
        }
        #endregion
    }

    /// <summary>
    /// 应用程序
    /// </summary>
    public class Application : Application<Application>, IApplication
    {
        #region IApplication
        /// <summary>
        /// 运行应用程序，执行顺序
        /// <para>1、内置服务注册（在app构造方法执行）</para>
        /// <para>2、扫描程序集，扫描<see cref="Type"/>完成特定<see cref="Attribute"/>分析注册，触发<see cref="IApplication.OnScan"/>事件</para>
        /// <para>3、读取应用程序配置，外部通过<see cref="ISettingManager.Use(in bool, in string, SettingUserDelegate)"/>使用配置</para>
        /// <para>4、自定义服务注册；触发<see cref="IApplication.OnRegister"/>事件，用于完成个性化di替换等</para>
        /// <para>5、应用构建；触发<see cref="Application{T}.OnBuild"/> 完成应用启动前自定义配置</para>
        /// <para>6、服务启动；触发<see cref="IApplication.OnRun"/>，运行WebApp应用</para>
        /// </summary>
        public void Run() => Run(appBuilder: () => this);
        #endregion
    }
}

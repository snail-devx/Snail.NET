using Snail.Abstractions.Dependency.Interfaces;
using Snail.Dependency;
using Snail.Test.Dependency.Components;
using Snail.Test.Dependency.DataModels;
using Snail.Test.Dependency.Interfaces;

namespace Snail.Test.Dependency
{
    /// <summary>
    /// <see cref="IDIManager"/>、<see cref="DIManager"/>测试
    /// </summary>
    public sealed class DIManagerTest
    {
        /// <summary>
        /// 测试程序集扫描，同步测试方法
        ///     1、<see cref="IDIManager.Register"/>
        ///     2、<see cref="IDIManager.IsRegistered(string?, Type)"/>
        ///        <see cref="DIManagerExtensions.IsRegistered"/>
        ///     3、<see cref="IDIManager.Unregister(string?, Type)"/>
        ///        <see cref="DIManagerExtensions.Unregister"/>
        ///        <see cref="DIManagerExtensions.Unregister"/>
        /// </summary>
        [Test]
        public void TestAssembly()
        {
            //将DIManager的Root、Current排除掉，先去掉，看看效果

            //IDIManager di = DIHelper.UseDIManager(DIManager.Root);
            IApplication app = new Application();
            app.Run();

            IDIManager di = app.ScopeServices;
            //  from TestComponent
            //      Component   标记
            Assert.That(di.IsRegistered("TypeOfTransient", typeof(TestComponent)), "应该已注册了才对。key:TypeOfTransient    from:TestComponent");
            Assert.That(di.IsRegistered("TypeOfScope", typeof(TestComponent)), "应该已注册了才对。key:TypeOfTransient    from:TestComponent");
            Assert.That(di.IsRegistered("TypeOfSingleton", typeof(TestComponent)), "应该已注册了才对。key:TypeOfTransient    from:TestComponent");
            //      Component<> 标记
            Assert.That(di.IsRegistered(key: null, typeof(TestComponent)), "应该已注册了才对。key:null  from:TestComponent");
            Assert.That(di.IsRegistered(key: "TestKey", typeof(TestComponent)), "应该已注册了才对。key:TestKey  from:TestComponent");
            Assert.That(di.IsRegistered(key: "Scope", typeof(TestComponent)), "应该已注册了才对。key:Scope  from:TestComponent");
            Assert.That(di.IsRegistered(key: "Singleton", typeof(TestComponent)), "应该已注册了才对。key:Singleton  from:TestComponent");
            //  from ITestComponent
            //      Component   标记
            Assert.That(di.IsRegistered("TypeOfTransient", typeof(ITestComponent)), "应该已注册了才对。key:TypeOfTransient   from:ITestComponent");
            Assert.That(di.IsRegistered("TypeOfScope", typeof(ITestComponent)), "应该已注册了才对。key:TypeOfScope   from:ITestComponent");
            Assert.That(di.IsRegistered("TypeOfSingleton", typeof(ITestComponent)), "应该已注册了才对。key:TypeOfSingleton    from:ITestComponent");
            //      Component<> 标记
            Assert.That(di.IsRegistered(key: null, typeof(ITestComponent)), "应该已注册了才对。key:null  from:ITestComponent");
            Assert.That(di.IsRegistered(key: "TestKey", typeof(ITestComponent)), "应该已注册了才对。key:TestKey  from:ITestComponent");
            Assert.That(di.IsRegistered(key: "Scope", typeof(ITestComponent)), "应该已注册了才对。key:Scope  from:ITestComponent");
            Assert.That(di.IsRegistered(key: "Singleton", typeof(ITestComponent)), "应该已注册了才对。key:Singleton  from:ITestComponent");
            //  采用扩展方法判断
            //      TestComponent
            Assert.That(di.IsRegistered<TestComponent>(), "应该已注册了才对。key:null    from:TestComponent");
            Assert.That(di.IsRegistered<TestComponent>("TypeOfTransient"), "应该已注册了才对。key:TypeOfTransient    from:TestComponent");
            //      ITestComponent
            Assert.That(di.IsRegistered<ITestComponent>(), "应该已注册了才对。key:null    from:ITestComponent");
            Assert.That(di.IsRegistered<ITestComponent>("TypeOfTransient"), "应该已注册了才对。key:TypeOfTransient    from:ITestComponent");
            //  未注册
            Assert.That(!di.IsRegistered(key: "Singleton1", typeof(ITestComponent)), "应该未注册了才对。key:Singleton  from:ITestComponent");

            //  Unregister
            //      Unregister
            di.Unregister(key: "TypeOfTransient", typeof(TestComponent));
            di.Unregister(key: null, typeof(TestComponent));
            Assert.That(!di.IsRegistered(key: "TypeOfTransient", typeof(TestComponent)), "应该未注册了才对。key:TypeOfTransient  from:TestComponent");
            Assert.That(!di.IsRegistered(key: null, typeof(TestComponent)), "应该未注册了才对。key:TypeOfTransient  from:TestComponent");
            //      Unregister<>
            Assert.That(!di.Unregister(typeof(ITestComponent)).IsRegistered<ITestComponent>(key: null),
                 "应该未注册了才对。key:null  from:ITestComponent");
            Assert.That(!di.Unregister<ITestComponent>(key: "TypeOfTransient").IsRegistered<ITestComponent>(key: "TypeOfTransient"),
                 "应该未注册了才对。key:TypeOfTransient  from:ITestComponent");

            //  静态和抽象类型判断：没注册才对
            Assert.That(!di.IsRegistered(key: null, typeof(TestStaticComponent)), "应该未注册才对。key:null  from:TestStaticComponent");
            Assert.That(!di.IsRegistered(typeof(TestAbstractComponent)), "应该未注册才对。key:null  from:TestAbstractComponent");
        }

        /// <summary>
        /// 测试配置文件注入
        /// </summary>
        [Test]
        public void TestSettingRegister()
        {
            IApplication app = new Application();
            app.Run();
        }

        /// <summary>
        /// 测试依赖注入信息注册
        ///     1、from、to命中各种条件下的异常信息捕捉判断
        /// </summary>
        [Test]
        public void TestRegister()
        {
            /** <see cref="DIDescriptorTest.DIDescriptorTest"/> 方法中做了，这里不在冗余了 */
        }

        /// <summary>
        /// 测试依赖注入实例构建
        ///     1、测试普通的构造方法、测试属性、方法注入
        ///     2、测试泛型的构造方法、测试属性、方法注入
        ///     3、测试可枚举的构造方法、测试属性、方法注入
        ///     4、测试key隔绝实例
        /// </summary>
        [Test]
        public void TestResolve()
        {
            #region 常规测试，属性注入、方法注入、构造方法注入、key隔离，附加构造方法参数
            IDIManager di = DIManager.Empty;
            //  常规类型注册
            Assert.Catch<ArgumentNullException>(() => di.Register(null!), "注册空的依赖注入描述器");
            di.Register<IFrom1, To1>().Register<IFrom1, To1>(key: "key")
              .Register<IFrom2, To2>().Register<IFrom2, To2>(key: "key");
            Assert.That(di.Resolve<IFrom1>() != null, "IFrom1构建不应为null");

            //  To2做了属性注入和构造方法注入等逻辑；即时属性标记为 private init，也能注入进去
            IFrom2? if2 = di.Resolve<IFrom2>();
            Assert.That(if2 != null);
            Assert.That(if2 is To2);
            To2 to2 = (To2)if2!;
            Assert.That(to2.IF1_P != null);/*IF1_P                      属性注入，get、set都是public*/
            Assert.That(to2.IF1_PR != null);/*IF1_PR                    属性注入，init、get都是public*/
            Assert.That(to2.IF1_PRP != null);/*IF1_PRP                  属性注入，private init、get*/
            Assert.That(to2.IF1_PRP2 != null);/*IF1_PRP2                属性注入，private set*/
            Assert.That(to2.IF1_F != null);/*IF1_F                      字段注入，public*/
            Assert.That(to2.GetIF1_FPValue() != null);/*IF1_FP          字段注入，private*/
            Assert.That(to2.IF1_FR == null);/*IF1_FR                    字段注入，public readonly*/
            //      测试一下属性注入的key
            Assert.That(to2.IF1_KF != null);/*IF1_KF                    字段注入，Key = "key" */
            Assert.That(to2.IF1_K2F == null);/*IF1_K2F                    字段注入，Key = "key2" */
            //      简单生命周期测试
            Assert.That(to2.IF1_P == to2.IF1_PR);
            Assert.That(to2.IF1_PR == to2.IF1_PRP);
            Assert.That(to2.IF1_PRP == to2.IF1_PRP2);
            Assert.That(to2.IF1_PRP2 == to2.IF1_F);
            //      key隔离实例
            Assert.That(to2.IF1_KF != to2.IF1_PR);
            //      测一下构造方法和属性注入同时给值的情况
            Assert.That(to2.IsConstructorInject == "InjectConstruct");//    通过属性标记选举的【构造方法】
            Assert.That(to2.IsMethodInject == "InjectMethod");
            Assert.That(to2.IsMethodInject2 == "InjectMethod2");
            //     测试一个构造方法的参数注入
            to2 = (To2)di.Resolve(key: "key", typeof(IFrom2), [new StringParameter() { Value = "snail-test" }])!;
            Assert.That(to2 != null);
            Assert.That(to2!.IsConstructorParam == "snail-test");
            #endregion

            //  数据构建出来了就算通过
            #region 测试泛型：注册泛型不可构造类型，自动生成实现类
            di = DIManager.Empty;
            di.Register(typeof(IFromG2<,>), to: typeof(ToG2<,>));
            di.Register(typeof(IFromG2<Int32, Int32>), to: typeof(ToG2<Int32, Int32>));
            di.Register(typeof(IFromG2<string, string>), to: typeof(ToG2_1<string>));

            Assert.That(di.Resolve<IFromG2<string, Int32>>() is ToG2<string, Int32>);
            Assert.That(di.Resolve<IFromG2<Int32, Int32>>() is ToG2<Int32, Int32>);
            Assert.That(di.Resolve<IFromG2<string, string>>() is ToG2_1<string>);

            di.Register(typeof(IFromG2<string, string>), to: typeof(ToG2_1));
            Assert.That(di.Resolve<IFromG2<string, string>>() is ToG2_1);
            #endregion

            #region 可枚举
            di = DIManager.Empty;
            di.Register(typeof(IFromG2<,>), to: typeof(ToG2<,>));
            di.Register(typeof(IFromG2<Int32, Int32>), to: typeof(ToG2<Int32, Int32>));
            di.Register(typeof(IFromG2<string, string>), to: typeof(ToG2_1<string>));
            di.Register(key: "key", typeof(IFromG2<string, string>), to: typeof(ToG2_1<string>));
            di.Register(typeof(IFromG2<string, string>), to: typeof(ToG2_1));
            //  构建三个，且都不为null
            var ens = di.Resolve<IEnumerable<IFromG2<string, string>>>() as List<IFromG2<string, string>>;
            Assert.That(ens != null && ens.Count == 3 && ens.Any(item => item == null) == false);
            #endregion
        }

        /// <summary>
        /// 测试类型代理：超时自动回收
        /// </summary>
        [Test]
        public void TestTypeProxy()
        {
            //  需要测试看效果时，把下面代码解注释，会在【输出】中看到具体销毁了哪些类型

            IDIManager manager = DIManager.Empty;
            manager.Register<To1>();
            To1? to1 = manager.Resolve<To1>();
            Assert.That(to1 != null);

            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// 测试生命周期
        ///     1、单线程下：单例、瞬时、scope是否正常
        ///     2、多线程下：单例、瞬时、scope是否正常
        /// </summary>
        [Test]
        public void TestLifetime()
        {
            IDIManager manager = DIManager.Empty;
            manager.Register<IFrom1, To1>(lifetime: LifetimeType.Singleton);
            manager.Register<IFrom1, To1>(key: "Singleton", lifetime: LifetimeType.Singleton);
            manager.Register<IFrom1, To1>(key: "Scope", lifetime: LifetimeType.Scope);
            manager.Register<IFrom1, To1>(key: "Transient", lifetime: LifetimeType.Transient);
            //  一个manager自身测试： 首先是key隔离，不会影响相互间生命周期实例
            //      manager
            Assert.That(manager.Resolve<IFrom1>(key: null) != manager.Resolve<IFrom1>(key: "Singleton"));
            Assert.That(manager.Resolve<IFrom1>(key: "Singleton") == manager.Resolve<IFrom1>(key: "Singleton"));
            Assert.That(manager.Resolve<IFrom1>(key: "Singleton") != manager.Resolve<IFrom1>(key: "Scope"));
            Assert.That(manager.Resolve<IFrom1>(key: "Singleton") != manager.Resolve<IFrom1>(key: "Transient"));
            Assert.That(manager.Resolve<IFrom1>(key: "Scope") == manager.Resolve<IFrom1>(key: "Scope"));
            Assert.That(manager.Resolve<IFrom1>(key: "Scope") != manager.Resolve<IFrom1>(key: "Transient"));
            Assert.That(manager.Resolve<IFrom1>(key: "Transient") != manager.Resolve<IFrom1>(key: "Transient"));
            //      cManager：先构建子管理器，同步已有实例会不会继承过来
            IDIManager cManager = manager.New();
            Assert.That(cManager.IsRegistered<IFrom1>(key: "Singleton"));
            Assert.That(cManager.IsRegistered<IFrom1>(key: "Scope"));
            Assert.That(cManager.IsRegistered<IFrom1>(key: "Transient"));
            Assert.That(cManager.Resolve<IFrom1>(key: null) != cManager.Resolve<IFrom1>(key: "Singleton"));
            Assert.That(cManager.Resolve<IFrom1>(key: "Singleton") == cManager.Resolve<IFrom1>(key: "Singleton"));
            Assert.That(cManager.Resolve<IFrom1>(key: "Singleton") != cManager.Resolve<IFrom1>(key: "Scope"));
            Assert.That(cManager.Resolve<IFrom1>(key: "Singleton") != cManager.Resolve<IFrom1>(key: "Transient"));
            Assert.That(cManager.Resolve<IFrom1>(key: "Scope") == cManager.Resolve<IFrom1>(key: "Scope"));
            Assert.That(cManager.Resolve<IFrom1>(key: "Scope") != cManager.Resolve<IFrom1>(key: "Transient"));
            Assert.That(cManager.Resolve<IFrom1>(key: "Transient") != cManager.Resolve<IFrom1>(key: "Transient"));
            //  多个manager相互间测试：继承模式下，单例仍然一致
            Assert.That(manager.Resolve<IFrom1>(key: null) == cManager.Resolve<IFrom1>(key: null));
            Assert.That(manager.Resolve<IFrom1>(key: null) != cManager.Resolve<IFrom1>(key: "Singleton"));
            Assert.That(manager.Resolve<IFrom1>(key: "Singleton") == cManager.Resolve<IFrom1>(key: "Singleton"));
            Assert.That(manager.Resolve<IFrom1>(key: "Singleton") != cManager.Resolve<IFrom1>(key: "Scope"));
            Assert.That(manager.Resolve<IFrom1>(key: "Singleton") != cManager.Resolve<IFrom1>(key: "Transient"));
            Assert.That(manager.Resolve<IFrom1>(key: "Scope") != cManager.Resolve<IFrom1>(key: "Scope"));
            Assert.That(manager.Resolve<IFrom1>(key: "Scope") != cManager.Resolve<IFrom1>(key: "Transient"));
            Assert.That(manager.Resolve<IFrom1>(key: "Transient") != cManager.Resolve<IFrom1>(key: "Transient"));
        }

        /// <summary>
        /// 测试多线程模式
        ///     1、同一个Manager，注册、解注册是否正常
        ///     2、多线程下<see cref="DIManager.Current"/>、<see cref="DIManager.Empty"/>等是否正常
        /// </summary>
        [Test]
        public void TestMultiThread()
        {
            IDIManager manager = DIManager.Empty;
            manager.Register<IFrom1, To1>(lifetime: LifetimeType.Singleton);
            manager.Register<IFrom1, To1>(key: "Singleton", lifetime: LifetimeType.Singleton);
            manager.Register<IFrom1, To1>(key: "Scope", lifetime: LifetimeType.Scope);
            manager.Register<IFrom1, To1>(key: "Transient", lifetime: LifetimeType.Transient);

            //  单例模式下，多线程都是一致的；scope同一个manager和单例模式效果一致；瞬时模式，多线程下都不一样
            IFrom1[] sns = new IFrom1[100], sts = new IFrom1[100], sss = new IFrom1[100], ts = new IFrom1[100];
            Parallel.For(0, 100, index =>
            {

                TestContext.Out.WriteLine($"{index}");
                sns[index] = manager.Resolve<IFrom1>(key: null)!;
                sts[index] = manager.Resolve<IFrom1>(key: "Singleton")!;
                sss[index] = manager.Resolve<IFrom1>(key: "Scope")!;
                ts[index] = manager.Resolve<IFrom1>(key: "Transient")!;
            });
            //      无null值
            Assert.That(sns.Any(item => item == null) == false);
            Assert.That(sts.Any(item => item == null) == false);
            Assert.That(sss.Any(item => item == null) == false);
            Assert.That(ts.Any(item => item == null) == false);
            //      单例去重
            Assert.That(sns.Distinct().Count() == 1);
            Assert.That(sts.Distinct().Count() == 1);
            Assert.That(sss.Distinct().Count() == 1);
            Assert.That(ts.Distinct().Count() == 100);
        }

        /// <summary>
        /// 测试循环注入；死循环
        /// </summary>
        [Test]
        public void TestCircleResolve()
        {

            IDIManager manager = DIManager.Empty;
            manager.Register<CircleReference>().Register<CircleReference2>();
            //  循环依赖，会报错
            Assert.Catch<LockRecursionException>(() => manager.Resolve<CircleReference>(), "循环依赖报错");

        }


        #region 私有类型
        private class StringParameter : IParameter
        {
            /// <summary>
            /// 参数类型：和<see cref="Name"/>配合时用，选举要传递信息的目标参数
            /// </summary>
            Type IParameter.Type { get; } = typeof(string);

            /// <summary>
            /// 参数名称：和<see cref="Type"/>配合使用，选举要传递信息的目标参数
            /// <para>1、Name为空时，则选举第一个类型为Type的参数 </para>
            /// <para>2、Name非空时，则选举类型为Type、且参数名为Name的参数 </para>
            /// </summary>
            public string? Name { init; get; }
            /// <summary>
            /// 固定的参数值
            /// </summary>
            public string? Value { init; get; }

            /// <summary>
            /// 获取参数值；由外部自己构建
            /// </summary>
            /// <param name="manager">DI管理器实例</param>
            /// <returns></returns>
            Object? IParameter.GetParameter(in IDIManager manager) => Value;
        }
        #endregion
    }
}

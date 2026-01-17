using Microsoft.Extensions.DependencyInjection;
using Snail.Test.Dependency.DataModels;
using Snail.Test.Dependency.Interfaces;

namespace Snail.Test.Dependency
{
    /// <summary>
    /// 微软的依赖注入框架测试
    /// </summary>
    public sealed class ServiceCollectionTest
    {
        [Test]
        public void TestSC()
        {
            //  依赖注册
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IFrom1, To1>();

            //  构建服务器提供程序
            IServiceProvider sp = services.BuildServiceProvider();
            //Int32 d= sp.GetService<Int32>();
            Object? d = sp.GetService(typeof(Int32));
            IFrom1? f1 = (IFrom1?)sp.GetService(typeof(IFrom1));

        }

        /// <summary>
        /// 测试构建实例
        /// </summary>
        [Test]
        public void TestResolve()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(typeof(IFrom1), typeof(To1));
            services.AddKeyedSingleton(typeof(IFrom1), "xxx", typeof(To1_1));
            services.AddSingleton(typeof(CircleReference));
            services.AddSingleton(typeof(CircleReference2));

            //  sp执行构造方法创建实例时，所有参数都会再走一下sp.GetService创建，若未注册依赖，则会报错；即使未注册的实例为可new的class
            //      System.InvalidOperationException : Unable to resolve service for type 'Snail.Test.Dependency.Interfaces.IFrom2' while attempting to activate 'Snail.Test.Dependency.DataModels.To1_1'.
            //      System.InvalidOperationException : Unable to resolve service for type 'Snail.Test.Dependency.DataModels.To1' while attempting to activate 'Snail.Test.Dependency.DataModels.To1_1'

            IServiceProvider sp = services.BuildServiceProvider();
            IFrom1? if1 = sp.GetKeyedService<IFrom1>("xxx");
            //  循环依赖，会报错
            Assert.Catch<InvalidOperationException>(() => sp.GetService<CircleReference>(), "循环依赖报错");

            //  测试单例模式下多个from指向同一个实现类时，会创建多个实例
            services = new ServiceCollection();
            services.AddSingleton<IFrom1, To_1_2>().AddSingleton<IFrom2, To_1_2>();
            sp = services.BuildServiceProvider();
            IFrom1 f1 = sp.GetService<IFrom1>()!;
            IFrom2 f2 = sp.GetService<IFrom2>()!;
            Assert.That(ReferenceEquals(f1, f2) == false);
        }

        /// <summary>
        /// 测试泛型
        /// </summary>
        [Test]
        public void TestGeneric()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IFrom1, To1>();
            services.AddSingleton<IFrom1, To1_1>();
            services.AddSingleton<IFrom2, To2>();
            // 报错 Open generic service type 'Test.DataModels.IFrom12`2[T1,T2]' requires registering an open generic implementation type. Arg_ParamName_Name
            //     注册时全泛型，实现类也没指定实体，目的就是为了方便Resolve时动态创建类型
            //services.AddTransient(typeof(IFrom12<,>), typeof(To12<To1,To2 >));
            services.AddTransient(typeof(IFromG2<,>), typeof(ToG2<,>));

            Type type1 = typeof(IFromG2<To1, To2>),
                type2 = typeof(IFromG2<IFrom1, IFrom2>),
                type3 = typeof(IFromG2<,>);


            //  详见 \Microsoft.Extensions.DependencyInjection\src\ServiceLookup\CallSiteFactory.cs
            IServiceProvider sp = services.BuildServiceProvider();

            sp.GetService(typeof(IFromG2<To1, To2>));

            Object?
                //Test.DataModels.To12`2[C1,C2]' can't be converted to service type 'Test.DataModels.IFrom12`2[I1,I2]'
                //db = sp.GetService(typeof(IFrom12<,>)),
                // 这个会自动基于 services.AddTransient(typeof(IFrom12<,>), typeof(To12<,>)); 注册新类型；MakeGenericType 
                da = sp.GetService(typeof(IFromG2<To1, To2>)),
                // 这个会自动基于 services.AddTransient(typeof(IFrom12<,>), typeof(To12<,>)); 注册新类型；MakeGenericType 
                dc = sp.GetService(typeof(IFromG2<IFrom1, IFrom2>));
            //var obj = sp.GetService(typeof(IFrom12<,>));
            IServiceProvider childProvider = sp.CreateScope().ServiceProvider;
        }

        /// <summary>
        /// 测试集合
        /// </summary>
        [Test]
        public void TestEnumerable()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IFrom1, To1>();
            services.AddTransient(typeof(IFromG2<,>), typeof(ToG2<,>));
            services.AddSingleton(typeof(IFromG2<,>), typeof(ToG2<,>));

            IServiceProvider sp = services.BuildServiceProvider();

            //  IEnumerable 内部若涉及到动态构建泛型类型，构建出来的只是给自己用的
            IEnumerable<IFromG2<IFrom1, IFrom2>> list1 = sp.GetServices<IFromG2<IFrom1, IFrom2>>();
            IEnumerable<IFromG2<IFrom1, IFrom2>> list2 = sp.GetServices<IFromG2<IFrom1, IFrom2>>();
            //      出来的list会基于现有依赖信息生命周期做策略：以最小的为准：瞬时<容器<单例

            //      到具体实例数据，还是走依赖注册信息的
            Assert.That(list1.First() != list2.First());
            Assert.That(list1.Last() == list2.Last());

            //  此处调用时，会重新再 基于 IFrom12<,> 构建 IFrom12<IFrom1,IFrom2>
            IFromG2<IFrom1, IFrom2>? f12_1 = sp.GetService<IFromG2<IFrom1, IFrom2>>();
            //  此处调用，不会再重新构建
            IFromG2<IFrom1, IFrom2>? f12_2 = sp.GetService<IFromG2<IFrom1, IFrom2>>();
            //  泛型动态构建数据，生命周期和源注册信息保持一致
            Assert.That(f12_1 == f12_2);

            //  动态构建的泛型，不会影响到List获取
            IEnumerable<IFromG2<IFrom1, IFrom2>> list3 = sp.GetServices<IFromG2<IFrom1, IFrom2>>();
            Assert.That(list1.Count() == list3.Count());
            Assert.That(list1.First() != list3.First());
            Assert.That(list1.Last() == list3.Last());

            //  构建子容器，查看Single模式的动态泛型，是否继承下去了
            IServiceProvider sp1 = sp.CreateScope().ServiceProvider;
            IFromG2<IFrom1, IFrom2>? f12_3 = sp1.GetService<IFromG2<IFrom1, IFrom2>>();
            Assert.That(f12_1 == f12_3);
        }

    }
}

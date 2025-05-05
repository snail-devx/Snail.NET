using System.Diagnostics;
using Snail.Abstractions.Dependency.DataModels;
using Snail.Dependency;
using Snail.Utilities.Collections;
using Snail.Utilities.Common.Extensions;

namespace Snail.WebApp.Components
{
    /// <summary>
    /// 【依赖注入】服务提供程序；用于和<see cref="IDIManager"/>做对接
    /// </summary>
    public sealed class ServiceProvider : Disposable, IServiceProvider, IKeyedServiceProvider, ISupportRequiredService, IServiceScopeFactory, IServiceScope, IDisposable
    {
        #region 属性变量
        /// <summary>
        /// 微软自带的Keyed服务映射，将keyed值映射成字符串值
        /// </summary>
        private static readonly LockMap<object, string> _keyedMap = new();
        /// <summary>
        /// 依赖注入管理器
        /// </summary>
        private readonly IDIManager _manager;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="services">已有的依赖注入实例</param>
        /// <param name="manager">依赖注入管理器</param>
        public ServiceProvider(in IServiceCollection services, in IDIManager manager)
        {
            _manager = manager;
            EnsureInCurrentThread();
            //  将内置服务添加到_manager中
            Registers(services);
            //  注册一些强制服务
            ForceRegisterService();
        }
        /// <summary>
        /// 基于父级构造；继承父级的一些数据
        /// </summary>
        /// <param name="parent"></param>
        private ServiceProvider(in ServiceProvider parent)
        {
            _manager = parent._manager.New();
            EnsureInCurrentThread();
            ForceRegisterService();
        }
        #endregion

        #region 实例构建：IServiceProvider、ISupportRequiredService、IKeyedServiceProvider
        /// <summary>
        /// 获取依赖注入实例
        /// </summary>
        /// <param name="from">依赖注入源类型</param>
        /// <returns></returns>
        object? IServiceProvider.GetService(Type from)
            => _manager.Resolve(key: null, from);
        /// <summary>
        /// 获取依赖注入实例；为null报错
        /// </summary>
        /// <param name="from">依赖注入源类型</param>
        /// <returns></returns>
        object ISupportRequiredService.GetRequiredService(Type from)
            => _manager.ResolveRequired(from);
        /// <summary>
        /// 获取依赖注入实例
        /// </summary>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="keyed">依赖注入Key值</param>
        /// <returns></returns>
        object? IKeyedServiceProvider.GetKeyedService(Type from, object? keyed)
        {
            string? key = BuildKeyByKeyd(keyed);
            object? value = _manager.Resolve(key, from);
            return value;
        }
        /// <summary>
        /// 获取依赖注入实例；为null报错
        /// </summary>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="keyed">依赖注入Key值</param>
        /// <returns></returns>
        object IKeyedServiceProvider.GetRequiredKeyedService(Type from, object? keyed)
        {
            string? key = BuildKeyByKeyd(keyed);
            object value = _manager.ResolveRequired(key, from);
            return value!;
        }
        #endregion

        #region 依赖注入Scope管理： IServiceScopeFactory、IServiceScope
        /// <summary>
        /// 创建新的scope作用域实例；基于当前实例创建新作用域实例
        /// </summary>
        /// <returns></returns>
        IServiceScope IServiceScopeFactory.CreateScope() => new ServiceProvider(this);
        /// <summary>
        /// 构建scope级别的ServiceProvider实例；返回自身实例即可
        /// </summary>
        IServiceProvider IServiceScope.ServiceProvider => this;
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
                    _manager.Dispose();
                    _keyedMap.TryDispose();
                }
                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
            }
            //  执行基类回收
            base.Dispose(disposing);
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 当前线程下的一些确保补偿工作
        /// </summary>
        private void EnsureInCurrentThread()
        {
            //  确保api运行期间DIManager.Current不为null；特别是dotnet在api运行时，大量多线程操作
            DIManager.Current = _manager;
        }
        /// <summary>
        /// 强制注册必备信息；完成scope、Provider之间的串联
        /// </summary>
        private void ForceRegisterService()
        {
            //  避免多次创建，构建相同注册信息，先清空一下
            _manager.Unregister<IServiceProvider>()
                    .Unregister<IKeyedServiceProvider>()
                    .Unregister<IServiceScopeFactory>()
                    .Unregister<IServiceScope>();
            //  强制逻辑：把当前对象继承的接口都注册一下，容器单例
            _manager.Register<IServiceProvider>(LifetimeType.Scope, this)
                    .Register<IKeyedServiceProvider>(LifetimeType.Scope, this)
                    .Register<IServiceScopeFactory>(LifetimeType.Scope, this)
                    .Register<IServiceScope>(LifetimeType.Scope, this);
        }

        /// <summary>
        /// 将内置的sc中注册的依赖注入服务，注册到<see cref="IDIManager"/>中
        /// </summary>
        /// <param name="services"></param>
        private void Registers(in IServiceCollection services)
        {
            //  遍历转换成【依赖注入】描述器；注意Keyed值
            DIDescriptor[] descriptors = new DIDescriptor[services.Count];
            for (var index = 0; index < services.Count; index++)
            {
                ServiceDescriptor sd = services[index];
#if DEBUG
                Debug.WriteLine("-----------内置服务：" + sd.ToString());
#endif
                //  分析构建依赖注入信息：区分区分Keyed
                string? key = null;
                LifetimeType lifetime = ConverLifetime(sd);
                Func<IDIManager, object?>? toFunc = null;
                Type? to = null;
                //      键值依赖注入服务
                if (sd.IsKeyedService == true)
                {
                    key = BuildKeyByKeyd(sd.ServiceKey);
                    if (sd.KeyedImplementationFactory != null)
                    {
                        Func<IServiceProvider, object?, object?> factory = sd.KeyedImplementationFactory;
                        object? keyed = sd.ServiceKey;
                        toFunc = manager =>
                        {
                            IServiceProvider sp = manager.ResolveRequired<IServiceProvider>();
                            return factory.Invoke(sp, keyed);
                        };
                    }
                    else if (sd.KeyedImplementationInstance != null)
                    {
                        object value = sd.KeyedImplementationInstance;
                        toFunc = manager => value;
                    }
                    else
                    {
                        to = sd.KeyedImplementationType;
                    }
                }
                //      常规无键值依赖注入服务
                else
                {
                    if (sd.ImplementationFactory != null)
                    {
                        Func<IServiceProvider, object> factory = sd.ImplementationFactory;
                        toFunc = manager =>
                        {
                            IServiceProvider sp = manager.ResolveRequired<IServiceProvider>();
                            return factory.Invoke(sp);
                        };
                    }
                    else if (sd.ImplementationInstance != null)
                    {
                        object value = sd.ImplementationInstance;
                        toFunc = manager => value;
                    }
                    else
                    {
                        to = sd.ImplementationType;
                    }
                }
                //  构建依赖注入描述器；from-to类型时，不检查类型，接入时发现检查类型，则微软内置服务会报错，先兼容一下
                descriptors[index] = toFunc == null
                    ? new DIDescriptor(key, sd.ServiceType, lifetime, to!)
                    : new DIDescriptor(key, sd.ServiceType, lifetime, toFunc);
            }
            _manager.Register(descriptors);
        }
        /// <summary>
        /// 基于<see cref="IServiceCollection"/>的Keyed服务构建对应的Key值字符串
        /// </summary>
        /// <param name="keyed"></param>
        /// <returns></returns>
        private static string? BuildKeyByKeyd(object? keyed)
        {
            //  值类型直接先ToString返回；引用类型，使用字典做key值生成guid值
            string? key = keyed == null
                ? null
                : (keyed.GetType().IsValueType
                    ? keyed.ToString()
                    : _keyedMap.GetOrAdd(keyed, _ => Guid.NewGuid().ToString())
                );
            return key;
        }
        /// <summary>
        /// 转换生命周期
        /// </summary>
        /// <param name="sd"></param>
        /// <returns></returns>
        private static LifetimeType ConverLifetime(in ServiceDescriptor sd)
        {
            switch (sd.Lifetime)
            {
                case ServiceLifetime.Singleton: return LifetimeType.Singleton;
                case ServiceLifetime.Scoped: return LifetimeType.Scope;
                case ServiceLifetime.Transient: return LifetimeType.Transient;
                default: throw new NotSupportedException($"不支持的ServiceLifetime生命周期值：{sd.Lifetime.ToString()}");
            }
        }
        #endregion
    }
}

using System.Diagnostics;

namespace Snail.WebApp.Components
{
    /// <summary>
    /// 工厂类：服务提供程序；用于构建依赖注入服务容器和实例构建提供程序
    /// </summary>
    public sealed class ServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        #region 属性变量
        /// <summary>
        /// 依赖注入管理器
        /// </summary>
        private readonly IDIManager _manager;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="manager"></param>
        public ServiceProviderFactory(IDIManager manager)
        {
            _manager = manager;
        }
        #endregion

        #region IServiceProviderFactory
        /// <summary>
        /// 构建【依赖注入】信息容器
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        IServiceCollection IServiceProviderFactory<IServiceCollection>.CreateBuilder(IServiceCollection services)
            => services;
        /// <summary>
        /// 构建【依赖注入】提供程序；和<see cref="IDIManager"/>桥接入口
        /// </summary>
        /// <param name="services">已有的依赖注入服务集合</param>
        /// <returns></returns>
        IServiceProvider IServiceProviderFactory<IServiceCollection>.CreateServiceProvider(IServiceCollection services)
        {
#if DEBUG //    调试时，测试自身是否有keyed依赖注入配置
            foreach (var sc in services)
            {
                if (sc.IsKeyedService == true)
                {
                    Debug.WriteLine(sc);
                }
            }
#endif
            //  构建全新的依赖注入管理器
            IServiceProvider provider = new ServiceProvider(services, _manager);
            return provider;
        }
        #endregion
    }
}

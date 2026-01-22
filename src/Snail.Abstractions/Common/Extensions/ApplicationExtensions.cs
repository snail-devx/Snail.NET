using Snail.Abstractions.Common.Interfaces;

namespace Snail.Abstractions.Common.Extensions;

/// <summary>
/// <see cref="IApplication"/>的通用扩展方法
/// </summary>
public static class ApplicationExtensions
{
    #region IBootstrapper 扩展
    /// <summary>
    /// 添加【引导程序】服务
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplication AddBootstrapperService(this IApplication app)
    {
        //  服务注册完成后，加载所有的 引导程序 实例，执行应用程序引导
        app.OnRegistered += services =>
        {
            IEnumerable<IBootstrapper>? bootstrappers = services.Resolve<IEnumerable<IBootstrapper>>();
            if (bootstrappers != null)
            {
                foreach (var item in bootstrappers)
                {
                    item.Bootstrap();
                }
            }
        };
        //  应用启动时，将 引导程序 从DI中干掉，后续不会再用了
        app.OnRun += services =>
        {
            services.Unregister<IEnumerable<IBootstrapper>>().Unregister<IBootstrapper>();
        };

        return app;
    }
    #endregion
}

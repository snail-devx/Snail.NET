using Snail.Abstractions.Setting.Delegates;
using Snail.Utilities.Common.Extensions;

namespace Snail.Abstractions.Setting.Extensions
{
    /// <summary>
    /// <see cref="IApplication"/>扩展方法：将setting的一些入口挂载上去，方便外部使用
    /// </summary>
    public static class ApplicationExtensions
    {
        #region 公共方法
        /// <summary>
        /// 判断是否是【生产环境】；从环境变量“RunType”值中分析
        /// </summary>
        /// <param name="app">应用程序自身</param>
        /// <returns>是返回true，否则返回false</returns>
        public static bool IsProduction(this IApplication app)
            => "Production".IsEqual(app.Setting.GetEnv("RunType"), ignoreCase: true);
        /// <summary>
        /// 获取应用程序配置的环境变量值 <br />
        ///     1、内置使用<see cref="ISettingManager.GetEnv(in string)"/>方法完成取值 <br />
        ///     2、请在<see cref="IApplication.Run"/>之后执行此方法
        /// </summary>
        /// <param name="app">应用程序自身</param>
        /// <param name="name">环境变量名称</param>
        /// <returns>配置值；若不存在返回null</returns>
        public static string? GetEnv(this IApplication app, string name)
            => app.Setting.GetEnv(name);

        /// <summary>
        /// 使用指定的应用程序配置 <br />
        ///     1、内置使用<see cref="ISettingManager.Use(in bool, in string, SettingUserDelegate)" /> 方法 <br />
        /// </summary>
        /// <param name="app">应用程序自身</param>
        /// <param name="isProject">false读取工作空间下配置；true读取工作空间-项目下的配置</param>
        /// <param name="rsCode">配置资源的编码</param>
        /// <param name="user">配置使用者</param>
        /// <returns>自身，链式调用</returns>
        public static IApplication UseSetting(this IApplication app, in bool isProject, in string rsCode, SettingUserDelegate user)
        {
            app.Setting.Use(isProject, rsCode, user);
            return app;
        }
        #endregion
    }
}

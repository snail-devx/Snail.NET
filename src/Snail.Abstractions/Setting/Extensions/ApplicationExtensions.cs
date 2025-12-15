using Snail.Abstractions.Setting.Delegates;
using Snail.Utilities.Common.Extensions;

namespace Snail.Abstractions.Setting.Extensions;

/// <summary>
/// <see cref="Setting"/>针对<see cref="IApplication"/>的扩展方法
/// <para>1、将setting的一些入口挂载上去，方便外部使用</para>
/// </summary>
public static class ApplicationExtensions
{
    extension(IApplication app)
    {
        #region 环境变量相关
        /// <summary>
        /// 应用名称
        /// <para>1、从环境变量<see cref="ENV_AppName"/>中分析</para>
        /// <para>2、若为空则使用<see cref="Assembly.GetEntryAssembly()"/></para>
        /// </summary>
        public string AppName => Default(app.GetEnv(ENV_AppName), Assembly.GetEntryAssembly()!.GetName().Name)!;
        /// <summary>
        /// 是否是【生产环境】
        /// <para>1、从环境变量<see cref="ENV_RunType"/>中分析</para>
        /// <para>2、值为 Production 时，则为生产环境</para>
        /// </summary>
        public bool IsProduction => "Production".IsEqual(app.Setting.GetEnv(ENV_RunType), ignoreCase: true);
        /// <summary>
        /// 数据中心Id
        /// <para>1、从环境变量<see cref="Env_DatacenterId"/>中分析</para>
        /// <para>2、用于进行分布式部署时使用，生成唯一主键id时使用</para>
        /// </summary>
        public string? DatacenterId => app.Setting.GetEnv(Env_DatacenterId);
        /// <summary>
        /// 工作节点Id
        /// <para>1、从环境变量<see cref="Env_WorkerId"/>中分析</para>
        /// <para>2、和 DatacenterId 配合，生成唯一主键id时使用</para>
        /// </summary>
        public string? WorkerId => app.Setting.GetEnv(Env_WorkerId);
        #endregion

        #region 扩展方法
        /// <summary>
        /// 使用指定的应用程序配置
        /// <para>1、内置使用<see cref="ISettingManager.Use(in bool, in string, SettingUserDelegate)" /> 方法</para>
        /// </summary>
        /// <param name="isProject">false读取工作空间下配置；true读取工作空间-项目下的配置</param>
        /// <param name="rsCode">配置资源的编码</param>
        /// <param name="user">配置使用者</param>
        /// <returns>自身，链式调用</returns>
        public IApplication UseSetting(in bool isProject, in string rsCode, SettingUserDelegate user)
        {
            app.Setting.Use(isProject, rsCode, user);
            return app;
        }
        /// <summary>
        /// 获取应用程序配置的环境变量值
        /// <para>1、内置使用<see cref="ISettingManager.GetEnv(in string)"/>方法完成取值 </para>
        /// <para>2、请在<see cref="IApplication.Run"/>之后执行此方法 </para>
        /// </summary>
        /// <param name="name">环境变量名称</param>
        /// <returns>配置值；若不存在返回null</returns>
        public string? GetEnv(string name) => app.Setting.GetEnv(name);
        #endregion
    }
}

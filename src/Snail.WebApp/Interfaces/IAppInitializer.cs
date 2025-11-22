namespace Snail.WebApp.Interfaces;

/// <summary>
/// 接口约束：应用程序初始化器 <br />
///     1、用于在<see cref="IApplication"/>实例构建时做一些默认初始化，确保在外部自定义初始化之前
/// </summary>
public interface IWebAppInitializer
{
    /// <summary>
    /// 初始化App；app对象由
    /// </summary>
    /// <param name="app">app对象</param>
    void InitApp(WebApplication app);
}

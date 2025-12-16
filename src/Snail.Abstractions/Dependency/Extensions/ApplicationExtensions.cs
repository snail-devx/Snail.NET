using Snail.Abstractions.Dependency.DataModels;
using Snail.Abstractions.Dependency.Interfaces;
using System.Diagnostics;

namespace Snail.Abstractions.Dependency.Extensions;

/// <summary>
/// <see cref="Dependency"/>针对<see cref="IApplication"/>的扩展方法
/// </summary>
public static class ApplicationExtensions
{
    extension(IApplication app)
    {
        #region 扩展方法
        /// <summary>
        /// 为应用添加【依赖注入】服务
        /// </summary>
        /// <returns></returns>
        internal IApplication AddDIService()
        {
            //  1、监听应用的程序集扫描事件，扫描实现【IComponent】接口的组件标签，动态注册依赖注入相关信息
            app.OnScan += (services, type, attrs) =>
            {
                //  无效组件，忽略掉
                if (type.CanAsToType(out string? error) == false)
                {
#if DEBUG
                    Debug.WriteLine($"不能为做组件使用，{error}");
#endif
                    return;
                }
                //  分析特性标签，进行依赖注入信息分析
                List<DIDescriptor> descriptors = [];
                DIDescriptor di;
                for (int index = 0; index < attrs.Length; index++)
                {
                    Attribute attr = attrs[index];
                    if (attr is IComponent component)
                    {
                        di = new DIDescriptor(component.Key, component.From ?? type, component.Lifetime, type);
                        descriptors.Add(di);
#if DEBUG
                        Debug.WriteLine($"注册组件：key={di.Key ?? STR_Null},from={di.From.FullName},lifetime={di.Lifetime},to={di.To!.FullName}");
#endif
                    }
                }
                //  添加注入信息
                services.Register([.. descriptors]);
            };
            //  2、监听setting的扫描配置，接收ioc配置文件，解析配置内容生成【依赖注入】信息；考虑先不对外提供【配置文件】注入方式，看看有没有问题
            /* 考虑先不对外提供【配置文件】注入方式，看看有没有问题；推荐先使用程序集扫描实现type自动注入方式
            app.Setting.Use(isProject: true, "dependency", (string workspace, string? project, string code, SettingType type, string content) =>
            {
                ThrowIfFalse(type == SettingType.File, $"【依赖注入】暂时仅支持File类型配置扫描。type:{type.ToString()}");
                //  分析【依赖注入】信息，Key值前缀使用工作空间、项目Code拼接起来
                string keyPrefix = string.Join(STR_SEPARATOR, workspace, project);
                IList<DIDescriptor>? descriptors = DIHelper.BuildFromXmlFile(content, keyPrefix: $"{workspace}:{project}", defaultLifetime: LifetimeType.Singleton);
                //  注入【依赖注入】信息，调试模式下输出解析出来的信息
                if (descriptors?.Count > 0)
                {
                    app.DI.Register(descriptors.ToArray());
    #if DEBUG
                    foreach (var di in descriptors)
                    {
                        Debug.WriteLine($"注册组件：key={di.Key},from={di.From.FullName},lifetime={di.Lifetime},to={di.To!.FullName}");
                    }
    #endif
                }
            });
            */

            return app;
        }

        /// <summary>
        /// 使用【依赖注入服务】构建泛型实例
        /// <para>1、通过<see cref="IApplication.ScopeServices"/>进行泛型实例构建</para>
        /// </summary>
        /// <typeparam name="T">依赖注入源类型</typeparam>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <returns></returns>
        public T? Resolve<T>(string? key = null) => app.ScopeServices.Resolve<T>(key);
        /// <summary>
        /// 使用【依赖注入服务】构建有效泛型实例，返回null报错
        /// <para>1、通过<see cref="IApplication.ScopeServices"/>进行泛型实例构建</para>
        /// </summary>
        /// <typeparam name="T">依赖注入源类型</typeparam>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <returns></returns>
        public T ResolveRequired<T>(string? key = null) => app.ScopeServices.ResolveRequired<T>(key);

        /// <summary>
        /// 使用【依赖注入根服务】构建泛型实例
        /// <para>1、通过<see cref="IApplication.RootServices"/>进行泛型实例构建</para>
        /// </summary>
        /// <typeparam name="T">依赖注入源类型</typeparam>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <returns></returns>
        public T? ResolveInRoot<T>(string? key = null) => app.RootServices.Resolve<T>(key);
        /// <summary>
        /// 使用【依赖注入根服务】构建有效泛型实例，返回null报错
        /// <para>1、通过<see cref="IApplication.RootServices"/>进行泛型实例构建</para>
        /// </summary>
        /// <typeparam name="T">依赖注入源类型</typeparam>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <returns></returns>
        public T ResolveRequiredInRoot<T>(string? key = null) => app.RootServices.ResolveRequired<T>(key);
        #endregion
    }
}
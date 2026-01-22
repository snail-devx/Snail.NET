namespace Snail.Abstractions.Common.Interfaces;

/// <summary>
/// 接口约束：应用引导程序
/// <para>1、应用启动时，自动加载所有实现此接口的组件，进行应用程序初始化</para>
/// <para>2、初步需求场景：在使用mongo数据库时，需要在程序启动时，自动注入一些自定义序列化器，用于进行接口、抽象类的数据推断</para>
/// <para>3、需要<see cref="IApplication"/>实现类集成作为内置功能</para>
/// </summary>
public interface IBootstrapper
{
    /// <summary>
    /// 执行引导
    /// <para>1、执行时机：在<see cref="IApplication.OnRegistered"/>事件中执行此方法</para>
    /// </summary>
    void Bootstrap();
}

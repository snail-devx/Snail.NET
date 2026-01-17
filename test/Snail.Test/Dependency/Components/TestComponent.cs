namespace Snail.Test.Dependency.Components
{
    /// <summary>
    /// 测试组件
    /// </summary>
    [Component(From = typeof(TestComponent), Key = "TypeOfTransient", Lifetime = LifetimeType.Transient)]
    [Component(From = typeof(TestComponent), Key = "TypeOfScope", Lifetime = LifetimeType.Scope)]
    [Component(From = typeof(TestComponent), Key = "TypeOfSingleton", Lifetime = LifetimeType.Singleton)]
    [Component<TestComponent>]
    [Component<TestComponent>(Key = "TestKey")]
    [Component<TestComponent>(Key = "Scope", Lifetime = LifetimeType.Scope)]
    [Component<TestComponent>(Key = "Singleton", Lifetime = LifetimeType.Singleton)]

    [Component(From = typeof(ITestComponent), Key = "TypeOfTransient", Lifetime = LifetimeType.Transient)]
    [Component(From = typeof(ITestComponent), Key = "TypeOfScope", Lifetime = LifetimeType.Scope)]
    [Component(From = typeof(ITestComponent), Key = "TypeOfSingleton", Lifetime = LifetimeType.Singleton)]
    [Component<ITestComponent>]
    [Component<ITestComponent>(Key = "TestKey")]
    [Component<ITestComponent>(Key = "Scope", Lifetime = LifetimeType.Scope)]
    [Component<ITestComponent>(Key = "Singleton", Lifetime = LifetimeType.Singleton)]
    public sealed class TestComponent : ITestComponent
    {

    }
    /// <summary>
    /// 测试组件的接口
    /// </summary>
    public interface ITestComponent
    {

    }

    /// <summary>
    /// 静态class，不能作为组件，测试使用
    /// </summary>
    [Component]
    public static class TestStaticComponent
    {

    }
    /// <summary>
    /// 抽象class，不能作为组件，测试使用
    /// </summary>
    [Component]
    public abstract class TestAbstractComponent
    {

    }
}

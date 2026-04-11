namespace Snail.Test.Dependency.Components;

/// <summary>
/// 测试私有注入
/// </summary>
[Component<TestPrivateInject>]
internal class TestPrivateInject
{
    /// <summary>
    /// 应用程序实例
    /// </summary>
    /// <remarks>子类注入实现时，无法取到父类的private属性，会注入失效</remarks>
    [Inject]
    private IApplication App { init; get; } = null!;
    [Inject]
    protected IApplication App1 { init; get; } = null!;

    [Inject]
    public required IApplication App2 { init; get; }

    public required string X;

    /// <summary>
    /// </summary>
    [Inject]
    private void InjectMethod()
    {

    }
}

[Component<TestPrivateInject>(Key = "Child")]
internal class TestPrivateInjectChild : TestPrivateInject
{

}

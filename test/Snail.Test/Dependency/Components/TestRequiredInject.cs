using Snail.Utilities.Common.Interfaces;

namespace Snail.Test.Dependency.Components;

/// <summary>
/// 测试 Required 注入
/// </summary>
internal class TestRequiredInject
{
    [Inject(Required = true)]
    protected IApplication App { init; get; } = null!;

    [Inject(Required = true)]
    protected IIdentifiable Identifiable { init; get; } = null!;

    [Inject(Key = "11111111")]
    protected IIdentifiable Identifiable2 { init; get; } = null!;
}

/// <summary>
/// 
/// </summary>
internal class IdentifiableInject : IIdentifiable
{
    public string Id { set; get; } = null!;
}
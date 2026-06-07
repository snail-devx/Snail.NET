using Snail.Abstractions.Common;

namespace Snail.Common;

/// <summary>
/// 验证器默认实现
/// </summary>
[Component<IValidator>(Lifetime = LifetimeType.Singleton)]
public class Validator : IValidator
{
}

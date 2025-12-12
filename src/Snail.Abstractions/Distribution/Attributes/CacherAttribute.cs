using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Distribution.Interfaces;

namespace Snail.Abstractions.Distribution.Attributes;

/// <summary>
/// 特性标签：分布式缓存 注入器
/// <para>1、用于依赖注入自动构建<see cref="ICacher"/>实 </para>
/// <para>2、配置<see cref="ICacheProvider"/>参数注入Key，动态构建实例作为<see cref="ICacher"/>构造方法参数值传入 </para>
/// <para>3、暂时不集成服务器地址配置选项参数注入 </para>
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class CacherAttribute : AccessorAttribute<ICacher, ICacheProvider>
{
}

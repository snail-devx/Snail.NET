using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Logging.Interfaces;

namespace Snail.Abstractions.Logging.Attributes;

/// <summary>
/// 特性标签：日志记录器 注入器
/// <para>1、用于依赖注入自动构建<see cref="ILogger"/>实例</para>
/// <para>2、配置<see cref="ILogProvider"/>参数注入Key，动态构建实例作为<see cref="ILogger"/>构造方法参数值传入 </para>
/// <para>3、暂时不集成服务器地址配置选项参数注入</para>
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class LoggerAttribute : AccessorAttribute<ILogger, ILogProvider>
{
}

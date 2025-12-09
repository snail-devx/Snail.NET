using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Identity.Interfaces;

/// <summary>
/// 接口约束：主键Id提供程序
/// </summary>
public interface IIdProvider
{
    /// <summary>
    /// 生成新的主键Id
    /// </summary>
    /// <param name="codeType">>编码类型；默认Default；Provider中可根据此做id区段区分；具体得看实现类是否支持</param>
    /// <param name="server">服务器配置选项；为null提供程序自身做默认值处理，或者报错</param>
    /// <returns></returns>
    string NewId(string? codeType = null, IServerOptions? server = null);
}

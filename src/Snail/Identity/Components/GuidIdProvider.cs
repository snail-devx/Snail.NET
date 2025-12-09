using Snail.Abstractions.Identity.Interfaces;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Identity.Components;

/// <summary>
/// Guid主键Id管理器
/// </summary>
/// <remarks>作为依赖注入组件，Key，单例对外提供</remarks>
[Component<IIdProvider>(Key = DIKEY_Guid)]
public sealed class GuidIdProvider : IIdProvider
{
    #region 属性变量
    /// <summary>
    /// 云Id
    /// </summary>
    private string _cloudID = "10000";
    #endregion

    #region IIdProvider
    /// <summary>
    /// 生成新的主键Id
    /// </summary>
    /// <param name="codeType">>编码类型；默认Default；Provider中可根据此做id区段区分；具体得看实现类是否支持</param>
    /// <param name="server">服务器配置选项；为null提供程序自身做默认值处理，或者报错</param>
    /// <returns></returns>
    string IIdProvider.NewId(string? codeType, IServerOptions? server)
    {
        /*代码参照自工作中代码：LeadingCloud.Framework.Manager.DefaultIdentifier*/
        byte[] buffer = Guid.NewGuid().ToByteArray();
        return _cloudID + BitConverter.ToInt64(buffer, 0).ToString().Substring(5);
    }
    #endregion
}

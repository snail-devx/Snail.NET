namespace Snail.WebApp.Enumerations;

/// <summary>
/// 权限令牌来源类型
/// </summary>
public enum TokenFromType
{
    /// <summary>
    /// 来自路由参数，默认最后一个参数
    /// </summary>
    Route = 0,

    /// <summary>
    /// 来自QueryString查询参数
    /// </summary>
    Query = 10,

    /// <summary>
    /// 来自请求头
    /// </summary>
    Header = 20,

    /// <summary>
    /// 来此Cookie
    /// </summary>
    Cookie = 30,
}
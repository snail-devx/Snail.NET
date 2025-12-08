namespace Snail.Utilities.Web.Extensions;
/// <summary>
/// <see cref="Uri"/>相关扩展方法
/// </summary>
public static class UriExtensions
{
    #region 公共方法

    #region UserInfo
    /// <summary>
    /// 尝试从Uri中取出用户信息：用户名和密码
    ///     类似这种才能分析出来：mongodb://xxx:ZQx7rTwpc@192.168.13.21:30000
    /// </summary>
    /// <param name="uri">uri</param>
    /// <param name="user">out 参数；分析出来的用户名</param>
    /// <param name="pwd">out 参数；分析出来的密码</param>
    /// <returns>uri自身，方便做链式调用</returns>
    public static Uri TryGetUserInfo(this Uri uri, out string? user, out string? pwd)
    {
        user = null;
        pwd = null;
        //  对特殊字符做一下转义
        if (uri.UserInfo?.Length > 0)
        {
            var arr = uri.UserInfo.Split(':');
            if (arr.Length == 2)
            {
                user = Uri.UnescapeDataString(arr[0]);
                pwd = Uri.UnescapeDataString(arr[1]);
            }
        }
        return uri;
    }

    /// <summary>
    /// 尝试清理Uri中的UserInfo信息；无userInfo则忽略
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="userInfo">out 参数，被移除的userInfi信息字符串</param>
    /// <returns>移除userInfo后新的Uri</returns>
    public static Uri TryClearUserInfo(this Uri uri, out string userInfo)
    {
        userInfo = uri.UserInfo;
        if (userInfo?.Length > 0)
        {
            string newUrl = $"{uri.Scheme}://{uri.Authority}{uri.PathAndQuery}{uri.Fragment}";
            uri = new Uri(newUrl);
        }
        return uri;
    }
    #endregion

    #endregion
}

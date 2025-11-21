using System;

namespace Snail.WebApp.Extensions;

/// <summary>
/// HttpContext 扩展
/// </summary>
public static class HttpContextExtensions
{
    #region 公共方法
    /// <summary>
    /// 为当前网络上下文设置数据，方便网络请求操作中共享。
    /// </summary>
    /// <param name="context"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public static void SetData(this HttpContext context, string key, object? value)
    {
        ThrowIfNullOrEmpty(key);
        context.Items[key] = value;
    }
    /// <summary>
    /// 获取上下文设置的数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static T GetData<T>(this HttpContext context, string key)
    {
        ThrowIfNullOrEmpty(key);
        context.Items.TryGetValue(key, out object? value);
        return value == null ? default! : (T)value;
    }
    #endregion
}

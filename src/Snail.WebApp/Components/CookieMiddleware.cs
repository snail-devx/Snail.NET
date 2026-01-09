using Microsoft.Extensions.Primitives;
using Snail.Utilities.Common.Extensions;
using System.Text;

namespace Snail.WebApp.Components;

/// <summary>
/// Cookie中间件 
/// <para>1、解决外部传入cookie值包含“{”、“}”等关键字时，无法识别的问题 </para>
/// <para>2、确保此中间件在第一位执行，否则可能导致前面取到的cookie有问题 </para>
/// </summary>
[Component<CookieMiddleware>]
public sealed class CookieMiddleware : IMiddleware
{
    #region 属性变量
    /// <summary>
    /// Cookie特殊字符编码映射
    /// </summary>
    private static readonly IReadOnlyDictionary<char, string> _cookieSpecialCharEncodeMap = new Dictionary<char, string>()
        {
            { '"',"\"".AsUrlEncode()},
            { ',',",".AsUrlEncode()},
            //{';',";".AsUrlEncode() },/*这个先不做处理，后续看情况，不然多个cookie值的分隔符也会被处理掉*/
            {'\\',"\\".AsUrlEncode() },
        };
    #endregion

    #region IMiddleware
    /// <summary>
    /// Request handling method.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    /// <returns>A <see cref="Task"/> that represents the execution of this middleware.</returns>
    Task IMiddleware.InvokeAsync(HttpContext context, RequestDelegate next)
    {
        /*  未识别的Cookie值举例 _SHARE_KEY_CHAIN_ key 值不会被识别
         *      _SHARE_KEY_CHAIN_=  {"key1":"123","key3":"value2"}
         *  实现思路，将特定的关键字，进行url编码；然后再赋值给header
         *      如下为微软CookieHeaderParserShared中对cookie值的有效性做的判断 
                     if (c < 0x21 || c > 0x7E)
                     {
                         return false;
                     }
                     return !(c == '"' || c == ',' || c == ';' || c == '\\');
         *      先仅针对关键字做适配，这类【c < 0x21 || c > 0x7E】的暂时不管，实在不行，要求外部做url编码
         *          HttpUtility.UrlEncode(dd);
         */
        //  这个逻辑，不是特别好，仅是针对net46做兼容，真的要100%不出问题，还得外部做编码
        StringValues cookie = context.Request.Headers.Cookie;
        //  cookie有值，则遍历看是否有这些关键字
        if (cookie.Count > 0)
        {
            StringBuilder cSB = new StringBuilder();
            string tmpStr;
            foreach (char ch in cookie.ToString())
            {
                _cookieSpecialCharEncodeMap.TryGetValue(ch, out tmpStr!);
                if (tmpStr == null) cSB.Append(ch);
                else cSB.Append(tmpStr);
            }
            context.Request.Headers.Cookie = cSB.ToString();
            //  整理完之后，取一下
            var _ = context.Request.Cookies;
        }
        //  继续下一步执行逻辑
        return next.Invoke(context);
    }
    #endregion
}
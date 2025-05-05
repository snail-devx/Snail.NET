using Microsoft.AspNetCore.Mvc.Filters;
using Snail.Utilities.Common.Extensions;
using Snail.WebApp.Attributes;
using Snail.WebApp.Enumerations;
using Snail.WebApp.Extensions;

namespace Snail.WebApp.Components
{
    /// <summary>
    /// API请求动作ContentType过滤器 <br />
    ///     1、验证特定API仅支持指定格式数据提交；如仅支持json提交post数据
    /// </summary>
    public sealed class ActionContentFilter : IActionFilter
    {
        #region 属性变量
        /// <summary>
        /// mimetype映射字典
        /// </summary>
        private static Dictionary<string, ContentType> _contentTypeMap = new Dictionary<string, ContentType>
        {
            //  JSON提交
            ["application/json"] = ContentType.Json,
            //  Form-URL提交
            ["application/x-www-form-urlencoded"] = ContentType.FormUrl,
        };
        #endregion

        #region IActionFilter
        /// <summary>
        /// 请求时
        /// </summary>
        /// <param name="context"></param>
        void IActionFilter.OnActionExecuting(ActionExecutingContext context)
        {
            ContentAttribute attr = context.GetCustomAttribute<ContentAttribute>(context.Controller);
            // 忽略
            if ((attr.Allow & ContentType.Ignore) == ContentType.Ignore)
            {
                return;
            }
            //  遍历content-type值
            ContentType ct;
            if (context.HttpContext.Request.ContentType?.Length > 0)
            {
                KeyValuePair<string, ContentType> kv = default;
                foreach (var str in context.HttpContext.Request.ContentType.Split(';'))
                {
                    kv = _contentTypeMap.FirstOrDefault(kv => kv.Key.IsEqual(str, ignoreCase: true));
                    if (kv.Key != null) break;
                }
                ct = kv.Key == null ? ContentType.Ignore : kv.Value;
            }
            else
            {
                ct = ContentType.Ignore;
            }
            //  不合法，抛出错误中断
            if ((attr.Allow & ct) != ct)
            {
                string msg = $"不支持的Content-Type值：{context.HttpContext.Request.ContentType}";
                throw new NotSupportedException(msg);
            }
        }

        /// <summary>
        /// 操作执行完成后
        /// </summary>
        /// <param name="context"></param>
        void IActionFilter.OnActionExecuted(ActionExecutedContext context)
        {
            //  执行结束后，什么都不用作
        }
        #endregion

        #region 继承方法
        #endregion

        #region 内部方法
        #endregion

        #region 私有方法
        #endregion
    }
}

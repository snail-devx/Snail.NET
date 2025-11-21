using Snail.Common.Extensions;

namespace Snail.WebApp.Components
{
    /// <summary>
    /// 上下文中间件
    /// <para>1、为每个请求构建全新的运行时上下文，互不干扰</para>
    /// <para>2、从请求信息中提取信息，构建共享钥匙串信息</para>
    /// </summary>
    [Component<RunContextMiddleware>(Lifetime = LifetimeType.Singleton)]
    public class RunContextMiddleware : IMiddleware
    {
        #region 属性变量
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
            //  构建全新的运行时上下文：先直接从Cookie中读取；后续考虑做一些兼容性工作，如从Header中读取、、、。以适配不同情况的需求
            {
                RunContext rt = RunContext.New();
                //  分析共享钥匙串数据
                rt.InitShareKeyChain(context.Request.Cookies[CONTEXT_ShareKeyChain]);
                //  分析父级操作Id，完成追踪逻辑
                string? tmpString = context.Request.Cookies[CONTEXT_ParentActionId];
                if (string.IsNullOrEmpty(tmpString) == false)
                {
                    rt.Add(CONTEXT_ParentActionId, tmpString);
                }
            }
            //  进入下一个操作
            return next.Invoke(context);
        }
        #endregion

        #region 继承方法
        #endregion

        #region 私有方法
        #endregion
    }
}

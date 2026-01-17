using Microsoft.AspNetCore.Mvc.Filters;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Dependency.Enumerations;
using Snail.WebApp.Attributes;
using Snail.WebApp.Components;

namespace Snail.WebApiTest.Components
{
    /// <summary>
    /// 
    /// </summary>
    [Component<ActionBaseFilter>(Lifetime = LifetimeType.Singleton)]
    public sealed class CustomActionFilter : ActionBaseFilter
    {
        #region 属性变量
        #endregion

        #region 构造方法

        #endregion

        #region 公共方法
        #endregion

        #region 继承方法
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        protected override Task? CaptureAuth(ActionExecutingContext request, AuthAttribute attr)
        {
            return null;
        }
        #endregion

        #region 内部方法
        #endregion

        #region 私有方法
        #endregion
    }
}

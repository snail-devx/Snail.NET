namespace Snail.Abstractions.Web.Extensions;
/// <summary>
/// <see cref="IApplication"/>针对<see cref="Web"/>下的相关扩展
/// </summary>
public static class ApplicationExtensions
{
    extension(IApplication app)
    {
        /// <summary>
        /// 添加HTTP请求服务
        /// </summary>
        /// <returns></returns>
        public IApplication AddHttpService()
        {
            //  服务注册完成后，对Http请求相关对象做一下预热操作
            app.OnRegister += () =>
            {
                app.ResolveRequired<IHttpManager>();
            };
            return app;
        }
    }
}

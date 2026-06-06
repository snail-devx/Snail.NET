using Snail.Abstractions.Identity.Interfaces;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Identity.Extensions;
/// <summary>
/// <see cref="Identity"/>针对<see cref="IApplication"/>扩展方法
/// </summary>
public static class ApplicationExtensions
{
    extension(IApplication app)
    {
        /// <summary>
        /// 生成新的主键Id
        /// <para>1、采用默认的<see cref="IIdProvider"/>实现类进行生成，且<see cref="IServerOptions"/>传null</para>
        /// <para>2、构建<see cref="IIdProvider"/>时使用<see cref="IApplication.Services" />根服务实例</para>
        /// </summary>
        /// <param name="codeType"></param>
        /// <returns></returns>
        public string NewId(string codeType)
        {
            IIdProvider provider = app.ResolveRequired<IIdProvider>();
            return provider.NewId(codeType);
        }
    }

}
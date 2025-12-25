using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.Interfaces;
using Snail.Utilities.Collections.Extensions;

namespace Snail.Abstractions.Database.Extensions;

/// <summary>
/// <see cref="IDbModelProvider{DbModel}"/>扩展方法
/// </summary>
public static class DbModelProviderExtensions
{
    #region 属性变量
    #endregion

    #region 公共方法
    /// <summary>
    /// 基于主键Id获取一条数据
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <typeparam name="IdType">主键数据类型</typeparam>
    /// <param name="provider">数据库提供程序</param>
    /// <param name="id">主键Id值</param>
    /// <returns>存在返回实体，否则返回null</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbModelProvider{DbModel}.AsQueryable(string)"/>方法</remarks>
    public static async Task<DbModel?> Load<DbModel, IdType>(this IDbModelProvider<DbModel> provider, IdType id)
        where DbModel : class where IdType : notnull
    {
        IList<DbModel> rt = await provider.Load([id]);
        return rt.FirstOrDefault();
    }
    #endregion
}

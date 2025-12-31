using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.Interfaces;
using Snail.Utilities.Collections.Extensions;

namespace Snail.Abstractions.Database.Extensions;

/// <summary>
/// <see cref="IDbProvider"/>扩展方法
/// </summary>
public static class DbProviderExtensions
{
    #region 扩展方法
    extension(IDbProvider provider)
    {
        /// <summary>
        /// 基于主键id值加载数据
        /// </summary>
        /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
        /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
        /// <param name="id">主键Id</param>
        /// <returns></returns>
        public async Task<DbModel?> Load<DbModel, IdType>(IdType id) where DbModel : class where IdType : notnull
        {
            IList<DbModel> models = await provider.Load<DbModel, IdType>([id]);
            return models?.FirstOrDefault();
        }
        /// <summary>
        /// 基于主键id值加载数据
        /// </summary>
        /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
        /// <param name="ids">主键Id集合</param>
        /// <returns></returns>
        public Task<IList<DbModel>> Load<DbModel>(List<string> ids) where DbModel : class
            => provider.Load<DbModel, string>(ids);
        /// <summary>
        /// 基于主键id值加载数据
        /// </summary>
        /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
        /// <param name="id">主键Id</param>
        /// <returns></returns>
        public async Task<DbModel?> Load<DbModel>(string id) where DbModel : class
        {
            IList<DbModel> models = await provider.Load<DbModel, string>([id]);
            return models?.FirstOrDefault();
        }

        /// <summary>
        /// 基于主键id值更新数据
        /// </summary>
        /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
        /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
        /// <param name="id">要更新的数据主键id值</param>
        /// <param name="updates">要更新的数据；key为DbModel的属性名称，Value为具体值</param>
        /// <returns>更新的数据条数</returns>
        /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsUpdatable(string)"/>方法</remarks>
        public Task<long> Update<DbModel, IdType>(IdType id, IDictionary<string, object?> updates) where DbModel : class where IdType : notnull
             => provider.Update<DbModel, IdType>([id], updates);
        /// <summary>
        /// 基于主键id值更新数据
        /// </summary>
        /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
        /// <param name="ids">要更新的数据主键id值集合</param>
        /// <param name="updates">要更新的数据；key为DbModel的属性名称，Value为具体值</param>
        /// <returns>更新的数据条数</returns>
        /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsUpdatable(string)"/>方法</remarks>
        public Task<long> Update<DbModel>(IList<string> ids, IDictionary<string, object?> updates) where DbModel : class
            => provider.Update<DbModel, string>(ids, updates);
        /// <summary>
        /// 基于主键id值更新数据
        /// </summary>
        /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
        /// <param name="id">要更新的数据主键id值</param>
        /// <param name="updates">要更新的数据；key为DbModel的属性名称，Value为具体值</param>
        /// <returns>更新的数据条数</returns>
        /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsUpdatable(string)"/>方法</remarks>
        public Task<long> Update<DbModel>(string id, IDictionary<string, object?> updates) where DbModel : class
            => provider.Update<DbModel, string>([id], updates);


        /// <summary>
        /// 删除数据
        /// <para>1、主键id为字符串</para>
        /// </summary>
        /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
        /// <param name="ids"></param>
        /// <returns></returns>
        public Task<long> Delete<DbModel>(params IList<string> ids) where DbModel : class
            => provider.Delete<DbModel, string>(ids);
    }
    #endregion
}

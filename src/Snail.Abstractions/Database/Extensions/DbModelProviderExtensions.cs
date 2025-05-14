﻿using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.Interfaces;
using Snail.Utilities.Collections.Extensions;

namespace Snail.Abstractions.Database.Extensions
{
    /// <summary>
    /// <see cref="IDbModelProvider{DbModel}"/>扩展方法
    /// </summary>
    public static class DbModelProviderExtensions
    {
        #region 属性变量
        #endregion

        #region 公共方法
        /// <summary>
        /// 插入数据
        /// </summary>
        /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
        /// <param name="provider">数据库提供程序</param>
        /// <param name="model">数据对象</param>
        /// <returns>插入成功返回true；否则返回false</returns>
        public static Task<bool> InsertAsync<DbModel>(this IDbModelProvider<DbModel> provider, DbModel model) where DbModel : class
        {
            ThrowIfNull(model);
            return provider.Insert([model]);
        }
        /// <summary>
        /// 保存数据：存在覆盖，不存在插入
        /// </summary>
        /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
        /// <param name="provider">数据库提供程序</param>
        /// <param name="model">数据对象</param>
        /// <returns>插入成功返回true；否则返回false</returns>
        public static Task<bool> SaveAsync<DbModel>(this IDbModelProvider<DbModel> provider, DbModel model) where DbModel : class
        {
            ThrowIfNull(model);
            return provider.Save([model]);
        }
        /// <summary>
        /// 基于主键Id获取一条数据
        /// </summary>
        /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
        /// <typeparam name="IdType">主键数据类型</typeparam>
        /// <param name="provider">数据库提供程序</param>
        /// <param name="id">主键Id值</param>
        /// <returns>存在返回实体，否则返回null</returns>
        /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbModelProvider{DbModel}.AsQueryable(string)"/>方法</remarks>
        public static async Task<DbModel?> LoadAsync<DbModel, IdType>(this IDbModelProvider<DbModel> provider, IdType id)
            where DbModel : class where IdType : notnull
        {
            IList<DbModel> rt = await provider.Load([id]);
            return rt.FirstOrDefault();
        }
        /// <summary>
        /// 基于主键id更新一条数据
        /// </summary>
        /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
        /// <typeparam name="IdType">主键数据类型</typeparam>
        /// <param name="provider">数据库提供程序</param>
        /// <param name="id">主键Id值</param>
        /// <param name="updates">要更新的数据；key为DbModel的属性名称，Value为具体值</param>
        /// <returns>更新成功返回true；否则返回false</returns>
        /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbModelProvider{DbModel}.AsUpdatable(string)"/>方法</remarks>
        public static async Task<bool> UpdateAsync<DbModel, IdType>(this IDbModelProvider<DbModel> provider, IdType id, IDictionary<string, object?> updates)
            where DbModel : class where IdType : notnull
        {
            long rt = await provider.Update(updates, [id]);
            return rt == 1;
        }
        /// <summary>
        /// 基于主键id删除一条数据
        /// </summary>
        /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
        /// <typeparam name="IdType">主键数据类型</typeparam>
        /// <param name="provider">数据库提供程序</param>
        /// <param name="id">主键Id值</param>
        /// <returns>删除成功返回true；否则返回false</returns>
        /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbModelProvider{DbModel}.AsDeletable(string)"/>方法</remarks>
        public static async Task<bool> DeleteAsync<DbModel, IdType>(this IDbModelProvider<DbModel> provider, IdType id)
            where DbModel : class where IdType : notnull
        {
            long rt = await provider.Delete(id);
            return rt == 1;
        }
        #endregion
    }
}

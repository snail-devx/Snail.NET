﻿using System.Linq.Expressions;
using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.Interfaces;
using Snail.Utilities.Linq.Extensions;

namespace Snail.Database.Components
{
    /// <summary>
    /// <see cref="IDbUpdatable{DbModel}"/>接口实现
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    public abstract class DbUpdatable<DbModel> : IDbUpdatable<DbModel> where DbModel : class
    {
        #region 属性变量
        /// <summary>
        /// 路由分片
        /// </summary>
        protected readonly string? Routing;
        /// <summary>
        /// 过滤条件
        /// </summary>
        protected readonly List<Expression<Func<DbModel, bool>>> Filters = new();
        /// <summary>
        /// 更新时更新相关；key为属性名称，value为更新值
        /// </summary>
        protected readonly Dictionary<string, object?> Updates = new();
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="routing"></param>
        public DbUpdatable(string? routing)
        {
            Routing = Default(routing, defaultStr: null);
        }
        #endregion

        #region IDbUpdatable

        #region 当前类直接实现的
        /// <summary>
        /// 查询条件<br />
        ///     1、多次调用时内部进行and合并
        /// </summary>
        /// <param name="predicate">where条件lambda表达式。lambda表达式目前不支持子文档、子表查询。示例:item=>item.Name=="Test"</param>
        /// <returns>数据库查询对象，方便链式调用</returns>
        /// <remarks>Where条件生效规则和<see cref="IDbQueryable{DbModel}.Where(Expression{Func{DbModel, bool}})"/>保持一致</remarks>
        IDbUpdatable<DbModel> IDbUpdatable<DbModel>.Where(Expression<Func<DbModel, bool>> predicate)
        {
            ThrowIfNull(predicate);
            Filters.Add(predicate);
            return this;
        }

        /// <summary>
        /// 设置字段值<br />
        ///     1、多次调用按顺序合并<br />
        ///     2、仅针对更新操作生效<br />
        /// </summary>
        /// <typeparam name="TField">返回字段类型</typeparam>
        /// <param name="fieldLambda">字段lambda表达式。示例:item=>item.Name。不支持非成员字段</param>
        /// <param name="value">字段值</param>
        /// <returns>数据库查询对象，方便链式调用</returns>
        IDbUpdatable<DbModel> IDbUpdatable<DbModel>.Set<TField>(Expression<Func<DbModel, TField>> fieldLambda, TField value)
        {
            string name = ThrowIfNull(fieldLambda).GetMember().Name;
            Updates[name] = value;
            return this;
        }
        /// <summary>
        /// 批量设置字段值<br />
        ///     1、多次调用按顺序合并<br />
        ///     2、仅针对更新操作生效<br />
        /// </summary>
        /// <param name="data">字段值字典。key为DbModel属性名，vlaue为字段值</param>
        /// <returns>数据库查询对象，方便链式调用</returns>
        IDbUpdatable<DbModel> IDbUpdatable<DbModel>.Set(IDictionary<string, object?> data)
        {
            ThrowIfNull(data);
            foreach (var (key, value) in data)
            {
                Updates[key] = value;
            }
            return this;
        }
        #endregion

        #region 需要子类重写的
        /// <summary>
        /// 执行更新操作
        /// </summary>
        /// <remarks>禁止无条件更新、禁止无更新字段</remarks>
        /// <returns>更新数据条数</returns>
        public abstract Task<long> Update();
        #endregion

        #endregion
    }
}

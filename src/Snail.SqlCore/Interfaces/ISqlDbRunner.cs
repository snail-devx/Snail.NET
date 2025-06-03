using System.Data.Common;
using Snail.SqlCore.Enumerations;

namespace Snail.SqlCore.Interfaces
{
    /// <summary>
    /// 接口约束：sql数据库操作执行者
    /// </summary>
    public interface ISqlDbRunner
    {
        /// <summary>
        /// 基于属性获取对应的数据库字段名称
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <param name="title">用途标题，报错使用</param>
        /// <returns></returns>
        string GetDbFieldName(string propertyName, string title);

        /// <summary>
        /// 构建Select查询操作语句
        /// </summary>
        /// <param name="usageType">select的用户，字段数据选择、any数据判断，数据量、、、</param>
        /// <param name="filterSql">数据过滤条件sql；必填；不带Where条件；注意sql注入</param>
        /// <param name="selectFields">需要返回的数据字段集合，值为DbModel属性名；为null、空则返回所有字段</param>
        /// <param name="sorts">排序配置，key为DbModel的属性名，Value为true升序/false降序</param>
        /// <param name="skip">分页跳过多少页</param>
        /// <param name="take">分页取多少页数据</param>
        /// <returns>完整可执行的sql查询语句</returns>
        string BuildQuerySql(SelectUsageType usageType, string filterSql, IList<string>? selectFields = null, IList<KeyValuePair<string, bool>>? sorts = null, int? skip = null, int? take = null);
        /// <summary>
        /// 构建Update更新操作sql
        /// </summary>
        /// <param name="updates">要更新的数据；key为DbModel的属性名称，Value为具体值</param>
        /// <param name="filterSql">数据过滤条件sql；必填；不带Where条件；注意sql注入</param>
        /// <param name="filterParam">数据过滤条件参数字典；key为DbModel的参数名称，value为参数值</param>
        /// <param name="param">更新语句的参数化字典，自动合并过滤条件参数；key为参数名称，value为参数值。防止sql注入</param>
        /// <returns>完整可执行的sql语句</returns>
        string BuildUpdateSql(IDictionary<string, object?> updates, string filterSql, IDictionary<string, object> filterParam, out IDictionary<string, object> param);
        /// <summary>
        /// 构建Delete删除操作sql
        /// </summary>
        /// <param name="filterSql">数据过滤条件sql；必填；不带Where条件；注意sql注入</param>
        /// <returns>完整可执行的sql语句</returns>
        string BuildDeleteSql(string filterSql);

        /// <summary>
        /// 执行数据库操作
        /// </summary>
        /// <typeparam name="T">执行数据库操作返回值类型</typeparam>
        /// <param name="dbAction">要执行的数据库操作</param>
        /// <param name="isReadAction">操作数据库时，是读操作还是写操作；默认读操作</param>
        /// <param name="needTransaction">是否需要事务，默认不需要</param>
        /// <returns>数据库操作返回值</returns>
        T RunDbAction<T>(Func<DbConnection, T> dbAction, bool isReadAction = true, bool needTransaction = false);
        /// <summary>
        /// 执行异步数据库操作
        /// </summary>
        /// <typeparam name="T">执行数据库操作返回值类型</typeparam>
        /// <param name="dbAction">要执行的数据库操作</param>
        /// <param name="isReadAction">操作数据库时，是读操作还是写操作；默认读操作</param>
        /// <param name="needTransaction">是否需要事务，默认不需要</param>
        /// <returns>数据库操作返回值</returns>
        Task<T> RunDbActionAsync<T>(Func<DbConnection, Task<T>> dbAction, bool isReadAction = true, bool needTransaction = false);
    }
}

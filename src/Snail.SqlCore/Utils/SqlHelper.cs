using Snail.Abstractions.Database.DataModels;
using Snail.Utilities.Collections;
using Snail.Utilities.Collections.Extensions;
using System.Reflection;

namespace Snail.SqlCore.Utils;
/// <summary>
/// 关系型数据库操作助手了
/// </summary>
public static class SqlHelper
{
    #region 属性变量
    /// <summary>
    /// 字段值代理字典
    /// <para>1、key为字段的type值，value为字段值构建代理委托</para>
    /// <para>2、用于把传入数据转成对应集合数据，满足pgsql等不能直接给参数赋值object，得是具体类型值的需求</para>
    /// </summary>
    private static readonly LockMap<Type, Delegate> _fieldValueProxyMap = new();
    #endregion

    #region 内部方法
    /// <summary>
    /// 提取数据库字段值
    /// <para>1、从传入的数据实体集合中提取指定字段值</para>
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="field">要提取值的数据字段，基于<see cref="DbModelField.Type"/>转换数据类型</param>
    /// <param name="models"></param>
    /// <returns>提取的数据字段值集合，不知道具体数据类型，这里用object代替，实际上为 <see cref="List{Type}"/></returns>
    internal static object ExtractDbFieldValues<DbModel>(DbModelField field, IList<DbModel> models)
    {
        object[] values = models.Select(item => field.Property.GetValue(item)).ToArray()!;
        return ConvertDbFieldValues(field, values);
    }
    /// <summary>
    /// 转换数据库字段值
    /// </summary>
    /// <param name="field">要提取值的数据字段，基于<see cref="DbModelField.Type"/>转换数据类型</param>
    /// <param name="values"></param>
    /// <returns>提取的数据字段值集合，不知道具体数据类型，这里用object代替，实际上为 <see cref="List{Type}"/></returns>
    internal static object ConvertDbFieldValues(DbModelField field, object[] values)
    {
        //  基于type进行代理缓存，提升性能
        Delegate func = _fieldValueProxyMap.GetOrAdd(field.Type, type =>
        {
            Type newType = typeof(FieldValueProxy<>).MakeGenericType(type);
            var buildMethod = newType.GetField("Build", BindingFlags.Static | BindingFlags.Public)!.GetValue(null);
            return (Delegate)buildMethod!;
        });
        return func.DynamicInvoke([values])!;
    }
    #endregion

    #region 内部类型
    /// <summary>
    /// 字段值代理类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class FieldValueProxy<T>
    {
        /// <summary>
        /// 字段值构建委托
        /// </summary>
        public static readonly Func<object[], List<T>> Build;
        /// <summary>
        /// 静态构造方法
        /// </summary>
        static FieldValueProxy()
        {
            Type type = typeof(T);
            Build = values =>
            {
                var list = new List<T>(values.Length);
                foreach (var item in values)
                {
                    // 使用高效转换（避免 Convert.ChangeType）
                    ThrowIfNull(item);
                    T value = item.GetType() == type
                        ? (T)item
                        : (T)Convert.ChangeType(item, type);
                    list.Add(value);
                }
                return list;
            };
        }
    }
    #endregion
}
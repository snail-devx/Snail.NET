using Snail.Utilities.Common.Extensions;
using System.Runtime.Serialization;

/*  排序相关实体合并放到此处统一管理    */
namespace Snail.Elastic.DataModels;

/// <summary>
/// 排序实体
/// </summary>
[Serializable]
public class ElasticSortModel : ISerializable
{
    /// <summary>
    /// 排序的字段名
    /// </summary>
    public string FieldName { private init; get; }
    /// <summary>
    /// 排序的模式，升序还是降序<br />
    ///     asc：升序<br />
    ///     desc：降序<br />
    /// </summary>
    public string Order { private init; get; }

    /// <summary>
    /// 排序字段格式化<br />
    ///     在日期字段排序时可指定，其他情况无效果<br />
    /// </summary>
    public string? Format { init; get; }
    /// <summary>
    /// 排序模式<br />
    ///     1、在排序字段为array或者多值时生效；无特殊需求null即可；避免传错导致不可用<br />
    ///     2、可选值：<br />
    ///         min（最小值）<br />
    ///         max（最大值）<br />
    ///         sum（最小值，仅数值字段生效）<br />
    ///         avg（平均值，仅数值字段）<br />
    ///         median（中位数，仅数值字段）<br />
    /// </summary>
    public string? Mode { init; get; }

    //  后续再增加其他逻辑，如nested筛选排序

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="fieldName">排序字段名</param>
    /// <param name="isAsc">是否升序</param>
    public ElasticSortModel(string fieldName, bool isAsc)
    {
        ThrowIfNullOrEmpty(fieldName);
        FieldName = fieldName;
        Order = isAsc == true ? "asc" : "desc";
    }
    #endregion

    #region ISerializable：只实现序列化，反序列化先不管
    /// <summary>
    /// JSON序列化
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(FieldName, new
        {
            order = Order,
            format = Format,
            mode = Mode,
            //  固定配置
            missing = Order == "asc" ? "_first" : "_last",
        });
    }
    #endregion
}
/// <summary>
/// Nested字段排序
///     暂时只支持一级，后续看情况做多级支撑
/// </summary>
[Serializable]
public sealed class ElasticNestedSortModel : ElasticSortModel, ISerializable
{
    /// <summary>
    /// Nested字段排序配置
    /// </summary>
    public ElasticNestedSortFieldModel Nested { set; get; }

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="fieldName">排序字段名；需要的是Nested字段全路径</param>
    /// <param name="isAsc">是否升序</param>
    /// <param name="nested">nested字段信息；不能为null</param>
    public ElasticNestedSortModel(string fieldName, bool isAsc, ElasticNestedSortFieldModel nested)
        : base(fieldName, isAsc)
    {
        ThrowIfNull(Nested = nested);
    }
    #endregion

    #region ISerializable
    /// <summary>
    /// JSON序列化
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        //  需要把父级字段信息带过来
        info.AddValue(FieldName, new
        {
            order = Order,
            format = Format,
            mode = Mode,
            nested = Nested,
            //  固定配置
            missing = Order == "asc" ? "_first" : "_last",
        });
    }
    #endregion
}
/// <summary>
/// Nested字段排序排序的Nested字段实体
/// </summary>
[Serializable]
public sealed class ElasticNestedSortFieldModel : ISerializable
{
    /// <summary>
    /// nested字段路径
    /// </summary>
    public required string Path { init; get; }
    /// <summary>
    /// 最大用作计算排序的子文档数量。默认不限制；若子文档数量过多，对性能有影响<br />
    ///     1、对应ES的“max_children”
    /// </summary>
    public int? MaxChildren { init; get; }
    /// <summary>
    /// 子文档字段排序过滤条件
    /// </summary>
    public required ElasticQueryModel Filter { init; get; }
    /// <summary>
    /// 下级内嵌子文档做排序时的配置，也是配置 path、filter、max_children、nested（可无限往下循环）
    /// </summary>
    public ElasticNestedSortFieldModel? Nested { init; get; }

    #region ISerializable
    /// <summary>
    /// JSON序列化
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        ThrowIfNullOrEmpty(Path);
        ThrowIfNull(Filter);
        info.TryAddValue("path", Path)
            .TryAddValue("max_children", MaxChildren)
            .TryAddValue("filter", Filter)
            .TryAddValue("nested", Nested);
    }
    #endregion
}

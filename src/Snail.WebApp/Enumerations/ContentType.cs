namespace Snail.WebApp.Enumerations;

/// <summary>
/// 提交内容类型枚举：配合完成content-type筛选过滤
/// </summary>
public enum ContentType
{
    /// <summary>
    /// 允许所有的类型
    /// </summary>
    All = 1,

    /// <summary>
    /// application/json
    /// </summary>
    Json = 2,

    /// <summary>
    /// application/x-www-form-urlencoded
    /// </summary>
    FormUrl = 4,

    //  后续再支持其他的；按照2的n次方赋值
}
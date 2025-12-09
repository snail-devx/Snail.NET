namespace Snail.Aspect.Distribution.Enumerations;

/// <summary>
/// 枚举：缓存操作类型
/// </summary>
public enum CacheActionType
{
    /// <summary>
    /// 基于传入key加载缓存<br />
    ///     1、若key在缓存中不存在，则执行方法得到的新数据<br />
    ///     2、最后将缓存数据+新数据合并返回<br />
    /// </summary>
    Load = 10,
    /// <summary>
    /// 基于传入key加载缓存，取到的新数据自动save保存到缓存中<br />
    ///     1、若key在缓存中不存在，则将执行方法得到的新数据save到缓存中<br />
    ///     2、最后将缓存数据+新数据合并返回<br />
    /// </summary>
    LoadSave = 11,

    /// <summary>
    /// 保存缓存；将执行方法得到的数据save的缓存中
    /// </summary>
    Save = 20,

    /// <summary>
    /// 删除缓存；方法执行完成后，从缓存中删除key对应数据
    /// </summary>
    Delete = 30,
}

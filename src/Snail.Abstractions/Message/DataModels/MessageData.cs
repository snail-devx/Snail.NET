using Snail.Utilities.Common.Extensions;

namespace Snail.Abstractions.Message.DataModels;

/// <summary>
/// 消息数据
/// </summary>
public sealed class MessageData
{
    /// <summary>
    /// 消息名称
    /// 需要和注册时消息名称对应上
    /// </summary>
    public required string Name { init; get; }

    /// <summary>
    /// JSON序列化后消息数据
    /// </summary>
    public string? Data { set; get; }

    /// <summary>
    /// 消息上下文附加的上下文数据
    /// </summary>
    public IDictionary<string, string>? Context { set; get; }

    #region 公共方法
    /// <summary>
    /// JSON反序列化消息数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? As<T>()
    {
        return string.IsNullOrEmpty(Data)
            ? default
            : Data.As<T>();
    }
    #endregion

    #region 构造方法
    /// <summary>
    /// 默认无参构造方法
    /// </summary>
    public MessageData() { }
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="name">消息名</param>
    /// <param name="data">消息数据；内部会自动序列化成string</param>
    /// <param name="context">消息上下文附加数据</param>

    public MessageData(string name, object? data, IDictionary<string, string>? context = null)
    {
        Name = ThrowIfNullOrEmpty(name);
        Data = data?.AsJson();
        Context = context;
    }
    #endregion
}

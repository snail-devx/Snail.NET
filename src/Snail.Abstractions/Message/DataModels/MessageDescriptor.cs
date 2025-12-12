using Snail.Utilities.Common.Extensions;

namespace Snail.Abstractions.Message.DataModels;

/// <summary>
/// 消息描述器
/// <para>1、描述发送和接收到的消息数据信息</para>
/// </summary>
public class MessageDescriptor
{
    /// <summary>
    /// 消息名称
    /// 需要和注册时消息名称对应上
    /// </summary>
    public required string Name { init; get; }

    /// <summary>
    /// JSON序列化后消息数据
    /// </summary>
    public string? Data { init; get; }

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
    public MessageDescriptor() { }
    #endregion
}

namespace Snail.RabbitMQ.Exceptions;

/// <summary>
/// 强制应答异常
/// <para>1、消息接收次数超过最大次数 </para>
/// <para>2、消息接收客户端出错等情况 </para>
/// </summary>
public class ForceAskException : Exception
{
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="reason">强制应答的原因</param>
    /// <param name="innerEx"></param>
    public ForceAskException(string reason, Exception? innerEx = null) : base(reason, innerEx)
    {
    }
}

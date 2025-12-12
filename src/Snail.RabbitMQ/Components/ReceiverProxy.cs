using Snail.RabbitMQ.Exceptions;
using Snail.Utilities.Common.Extensions;

namespace Snail.RabbitMQ.Components;

/// <summary>
/// 消息接收器代理
/// </summary>
public sealed class ReceiverProxy<T>
{
    #region 属性变量
    /// <summary>
    /// 重视次数
    /// </summary>
    private readonly int _attempt;
    /// <summary>
    /// 接收器
    /// </summary>
    private readonly Func<T, Task<bool>> _receiver;
    /// <summary>
    /// 计数器
    /// </summary>
    private int _counter = 0;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="attempt">接收消息的尝试次数
    /// <para>1、接收方发生异常后，尝试多少次后仍失败，则强制确认，避免消息堆积 </para>
    /// <para>2、&lt;= 0 不自动确认；直到处理成功 </para>
    /// </param>
    /// <param name="receiver"></param>
    public ReceiverProxy(int attempt, Func<T, Task<bool>> receiver)
    {
        _attempt = attempt;
        _receiver = ThrowIfNull(receiver);
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 接收消息
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<bool> OnReceive(T message)
    {
        Exception? tmpEx = null;
        bool isSuccess = false;
        //  接收消息处理，拦截异常
        try
        {
            isSuccess = await _receiver.Invoke(message);
        }
        catch (Exception ex)
        {
            tmpEx = ex.Optimize();
        }
        //  进行消息接收结果处理：成功时，重置计数器；失败时，进行尝试次数判断（ _attempt <= 0 表示一直尝试）
        if (isSuccess == false)
        {
            if (_attempt > 0)
            {
                _counter += 1;
                //  超过重视次数，强制清除
                if (_counter >= _attempt)
                {
                    tmpEx = tmpEx != null
                        ? new ForceAskException($"消息已经连续{_counter}次处理失败，发生异常", tmpEx)
                        : new ForceAskException($"消息已经连续{_counter}次处理失败，接收方返回false；");
                    _counter = 0;
                }
            }
            if (tmpEx != null)
            {
                throw tmpEx;
            }
        }
        else
        {
            _counter = 0;
        }
        return isSuccess;
    }
    #endregion
}

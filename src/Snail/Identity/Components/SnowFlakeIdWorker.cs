namespace Snail.Identity.Components;

/// <summary>
/// Id生成器
/// <para>1、参照自工作代码：LeadingCloud.Framework.Id.IdWorker </para>
/// <para>2、后期支持外部指定<see cref="_twEpoch"/>值 </para>
/// </summary>
public sealed class SnowFlakeIdWorker
{
    #region 属性变量
    /// <summary>
    /// 开始时间 
    /// <para>2016,1,1 </para>
    /// <para>2016,1,1 Ticks 635872032000000000/10000=63587203200000 </para>
    /// </summary>
    private readonly long _twEpoch = 635872032000000000L;

    /// <summary>
    /// 数据中心标识位数
    /// </summary>
    private readonly int _datacenterIdBits = 5;
    /// <summary>
    /// 机器标识位数
    /// </summary>
    private readonly int _workerIdBits = 5;

    /// <summary>
    /// 数据中心ID最大值
    /// <para>-1L ^ (-1L &lt;&lt; <see cref="_datacenterIdBits"/>) </para>
    /// </summary>
    private readonly long _maxDatacenterId = 0; //-1L ^ (-1L << 5);
    /// <summary>
    /// 机器ID最大值
    /// <para>-1L ^ (-1L &lt;&lt; <see cref="_workerIdBits"/> ) </para>
    /// </summary>
    private readonly long _maxWorkerId = 0;// -1L ^ (-1L << 5);

    /// <summary>
    /// 毫秒内自增位
    /// </summary>
    private readonly int _sequenceBits = 12;
    /// <summary>
    /// 机器ID偏左移12位
    /// <para>workerIdShift = _sequenceBits </para>
    /// </summary>
    private readonly int _workerIdShift = 0;
    /// <summary>
    /// 数据中心ID左移17位
    /// <para>_datacenterIdShift = _sequenceBits + _workerIdShift </para>
    /// <para>12 + 5 </para>
    /// </summary>
    private readonly int _datacenterIdShift = 0;

    /// <summary>
    /// 时间毫秒左移22位
    /// <para>_timestampLeftShift = _sequenceBits + _workerIdShift + _datacenterIdShift </para>
    /// <para>12 + 5 + 5 </para>
    /// </summary>
    private readonly int _timestampLeftShift = 0;

    /// <summary>
    /// sequenceMask
    /// <para>sequenceMask = -1L ^ (-1L &lt;&lt; sequenceBits) </para>
    /// </summary>
    private readonly long _sequenceMask = 0;//-1L ^ (-1L << 12);

    /// <summary>
    /// 数据中心Id
    /// </summary>
    private readonly long _datacenterId = 0L;
    /// <summary>
    /// 应用程序Id
    /// </summary>
    private readonly long _workerId = 0L;

    //lastTimestamp = -1L
    //check workerId 
    //if (workerId > maxWorkerId || workerId < 0)
    //if (datacenterId > maxDatacenterId || datacenterId< 0)
    private long _lastTimestamp = 0L;
    private long _sequence = 0L;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造
    /// </summary>
    /// <param name="datacenterId">机器</param>
    /// <param name="workerId">应用</param>
    public SnowFlakeIdWorker(long datacenterId, long workerId)
    {
        //机器ID
        _datacenterId = datacenterId;
        //进程ID
        _workerId = workerId;
        //最大进程ID
        _maxWorkerId = -1L ^ -1L << _workerIdBits;
        //最大机器ID
        _maxDatacenterId = -1L ^ -1L << _datacenterIdBits;
        if (datacenterId > _maxDatacenterId)
        {
            throw new Exception(string.Format("IdWork datacenterId:{0}大于最大值:{1}", datacenterId, _maxDatacenterId));
        }
        else if (workerId > _maxWorkerId)
        {
            throw new Exception(string.Format("IdWork workerId:{0}大于最大值:{1}", workerId, _maxWorkerId));
        }

        //进程号左移位数
        _workerIdShift = _sequenceBits;
        //机器号左移位数
        _datacenterIdShift = _workerIdBits + _sequenceBits;
        //时间戳左移位数
        _timestampLeftShift = _workerIdBits + _datacenterIdBits + _sequenceBits;
        //时间戳最大值
        _sequenceMask = -1L ^ -1L << _sequenceBits;
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 下一个id
    /// </summary>
    /// <returns></returns>
    public long NextId()
    {
        //var timestamp = DateTime.UtcNow; //timeGen()
        long timestamp = timeGen();
        if (timestamp < _lastTimestamp)
        {
            //时间错误，时间往回调整了
            //两种处理方式，一种是报错，另一种是等待时间一直到大于等于当前时间
            throw new Exception(string.Format("IdWorker 系统时间{0}不能小于上次修改时间{1}，否则会造成ID重复，请修改后重试", timestamp, _lastTimestamp));
        }
        else if (timestamp == _lastTimestamp)
        {
            //当前毫秒内，则+1
            _sequence = _sequence + 1 & _sequenceMask;
            if (_sequence == 0)
            {
                //等待下一个毫秒的到来 
                timestamp = tilNextMillis(_lastTimestamp);
            }
        }
        else
        {
            _sequence = 0L;
        }
        _lastTimestamp = timestamp;
        //ID偏移组合生成最终的ID，并返回ID
        long newid = timestamp << _timestampLeftShift | _datacenterId << _datacenterIdShift | _workerId << _workerIdShift | _sequence;
        return newid;
    }

    /// <summary>
    /// 生成指定时间的ID，用于判断日期
    /// </summary>
    /// <returns></returns>
    public long GenerateId(DateTime datetime)
    {
        //var timestamp = DateTime.UtcNow; //timeGen()
        long timestamp = timeGen(datetime);
        long tmpsequence = 0L;
        //ID偏移组合生成最终的ID，并返回ID
        long newid = timestamp << _timestampLeftShift | _datacenterId << _datacenterIdShift | _workerId << _workerIdShift | tmpsequence;
        return newid;
    }
    #endregion

    #region 私有方法
    ///// <summary>
    ///// 暂不使用，用于理解算法
    ///// </summary>
    ///// <returns></returns>
    //private static Int64 timeGen_bak()
    //{
    //    TimeSpan ts = DateTime.UtcNow - new DateTime(2016, 1, 1);
    //    double tsDouble = ts.TotalMilliseconds;
    //    if (tsDouble > Int64.MaxValue)
    //    {
    //        //超出使用年限
    //        throw new Exception("IdWorker 超出使用年限");
    //    }
    //    //时间戳
    //    Int64 timestamp = (Int64)tsDouble;
    //    return timestamp;
    //}
    /*
       1纳秒 =1000皮秒
       1纳秒 =0.001 微秒
       1纳秒 =0.000 001毫秒 　
       1纳秒 =0.000 000 001秒
       Ticks:每个计时周期表示一百纳秒，即一千万分之一秒。
       此属性的值表示自 0001 年 1 月 1 日午夜 12:00:00（表示 DateTime..::.MinValue）
       以来经过的以 100 纳秒为间隔的间隔数 1s=1000000/100 Ticks
       2016,1,1 Ticks 635872032000000000/10000=63587203200000
       */
    private long timeGen(DateTime? datetime = null)
    {
        /*
        TimeSpan ts = DateTime.UtcNow - new DateTime(2016, 1, 1, 0, 0, 0, 0);
        double tsDouble = ts.TotalMilliseconds;
        if (tsDouble > long.MaxValue)
        {
            //超出使用年限
            throw new Exception("IdWorker 超出使用年限");
        }
        //时间戳
        long timestamp = (long)tsDouble;
        */
        if (datetime == null)
        {
            datetime = DateTime.UtcNow;
        }
        return (datetime.Value.Ticks - _twEpoch) / 10000;
    }

    private long tilNextMillis(long last)
    {
        long timestamp = timeGen();
        //等待下一个毫秒的到来 循环等待
        while (timestamp <= last)
        {
            timestamp = timeGen();
        }
        return timestamp;
    }
    #endregion
}

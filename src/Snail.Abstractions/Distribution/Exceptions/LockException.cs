namespace Snail.Abstractions.Distribution.Exceptions
{
    /// <summary>
    /// 锁操作异常
    /// </summary>
    public sealed class LockException : Exception
    {
        #region 属性变量
        /// <summary>
        /// 加锁Key值
        /// </summary>
        public string Key { private init; get; }
        /// <summary>
        /// 加锁值
        /// </summary>
        public string Value { private init; get; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="innerException"></param>
        public LockException(string key, string value, Exception? innerException = null)
            : base($"锁操作失败：key={key};value={value}", innerException)
        {
            Key = key;
            Value = value;
        }
        #endregion
    }
}

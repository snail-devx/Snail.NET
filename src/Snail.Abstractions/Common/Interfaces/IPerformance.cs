namespace Snail.Abstractions.Common.Interfaces
{
    /// <summary>
    /// 接口约束：性能指标 <br />
    ///     1|约束必须有耗时指标 <br />
    /// </summary>
    public interface IPerformance
    {
        /// <summary>
        /// 请求耗时毫秒
        /// </summary>
        public long? Performance { get; }
    }
}

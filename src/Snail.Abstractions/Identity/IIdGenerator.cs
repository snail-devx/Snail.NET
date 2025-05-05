namespace Snail.Abstractions.Identity
{
    /// <summary>
    /// 接口约束：主键Id生成器；支持服务器地址等网络分布式id策略
    /// </summary>
    public interface IIdGenerator
    {
        /// <summary>
        /// 生成新的主键Id
        /// </summary>
        /// <param name="codeType">>编码类型；默认Default；Provider中可根据此做id区段区分；具体得看实现类是否支持</param>
        /// <returns>新的主键Id值</returns>
        string NewId(string? codeType = null);
    }
}

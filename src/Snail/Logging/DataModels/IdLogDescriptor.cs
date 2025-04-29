using Snail.Abstractions.Identity.Interfaces;
using Snail.Abstractions.Logging.DataModels;

namespace Snail.Logging.DataModels
{
    /// <summary>
    /// 唯一主键日志描述器，多了日志Id约束
    /// </summary>
    public class IdLogDescriptor : LogDescriptor, IIdentity
    {
        #region INetLogDescriptor
        /// <summary>
        /// 日志Id；用于唯一标记本条日志
        /// </summary>
        public required string Id { get; init; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="forceLog">此日志是否为强制日志；强制日志，不受系统配置日志层级控制，始终记录</param>
        public IdLogDescriptor(bool forceLog = false) : base(forceLog) { }
        #endregion
    }
}

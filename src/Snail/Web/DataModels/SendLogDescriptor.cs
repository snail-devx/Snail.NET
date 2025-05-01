using Snail.Logging.DataModels;

namespace Snail.Web.DataModels
{
    /// <summary>
    /// HTTP请求发送日志描述器 <br /> 
    ///     在启用网络追踪时；强制给日志加上日志Id值
    /// </summary>
    public sealed class SendLogDescriptor : IdLogDescriptor
    {
        #region 属性变量
        /// <summary>
        /// 请求服务器地址配置选项
        /// </summary>
        public required string? ServerOptions { init; get; }

        /// <summary>
        /// 请求方法
        /// </summary>
        public required string HttpMethod { init; get; }

        /// <summary>
        /// 请求Headers
        /// </summary>
        public Dictionary<string, string?>? Headers { init; get; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="isForce"></param>
        public SendLogDescriptor(bool isForce) : base(isForce) { }
        #endregion
    }
}

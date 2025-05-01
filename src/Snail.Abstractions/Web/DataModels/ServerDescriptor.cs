using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Web.DataModels
{
    /// <summary>
    /// 服务器描述器
    /// </summary>
    public sealed class ServerDescriptor : ServerOptions, IServerOptions
    {
        #region 属性变量
        /** 从父类<see cref="ServerOptions"/>继承属性
         *      <see cref="ServerOptions.Workspace"/>                       服务器所在工作空间Key值
         *      <see cref="ServerOptions.Type"/>                            服务器类型；用于对多个服务器做分组用
         *      <see cref="ServerOptions.Code"/>                            服务器编码
         */

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string Server { private init; get; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法：<see cref="IServerOptions.Workspace"/>、<see cref="IServerOptions.Type"/>强制为null
        /// </summary>
        /// <param name="code">服务器编码</param>
        /// <param name="server">服务器地址</param>
        public ServerDescriptor(string code, string server)
            : this(workspace: null, type: null, code, server)
        {
        }
        /// <summary>
        /// 构造方法；<see cref="IServerOptions.Type"/>强制为null
        /// </summary>
        /// <param name="workspace">服务器所在工作空间Key值</param>
        /// <param name="code">服务器编码</param>
        /// <param name="server">服务器地址</param>
        public ServerDescriptor(string? workspace, string code, string server)
            : this(workspace, type: null, code, server)
        {
        }
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="workspace">服务器所在工作空间Key值</param>
        /// <param name="type">服务器节配置，如http、https、sdk、grpc等标记区分</param>
        /// <param name="code">服务器编码</param>
        /// <param name="server">服务器地址</param>
        public ServerDescriptor(string? workspace, string? type, string code, string server)
            : base(workspace, type, code)
        {
            Server = ThrowIfNullOrEmpty(server)!;
        }
        #endregion
    }
}

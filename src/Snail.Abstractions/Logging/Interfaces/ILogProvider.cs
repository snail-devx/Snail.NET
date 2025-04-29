﻿using Snail.Abstractions.Logging.DataModels;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Logging.Interfaces
{
    /// <summary>
    /// 接口约束：日志提供程序；负责进行具体日志记录
    /// </summary>
    public interface ILogProvider
    {
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="descriptor">要记录的日志信息</param>
        /// <param name="scope">日志作用域描述器；用于区分日志组等情况</param>
        /// <param name="server">服务器配置选项；为null提供程序自身做默认值处理，或者报错 <br />
        ///     1、记录器为网络日志时，日志要记录到哪个服务器下，如哪个数据库服务器 <br />
        ///     2、记录器为本地日志时，采用哪个工作组下的配置，如log4net配置；此时仅<see cref="IServerOptions.Workspace"/>生效 <br />
        /// </param>
        /// <returns>记录成功；返回true</returns>
        Boolean Log(LogDescriptor descriptor, ScopeDescriptor? scope, IServerOptions? server);
    }
}

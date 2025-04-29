using Snail.Abstractions.Logging.DataModels;
using Snail.Abstractions.Logging.Enumerations;

namespace Snail.Abstractions.Logging
{
    /// <summary>
    /// 接口约束：日志记录器
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 日志级别是否可用；不可用将不记录此级别日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="forceLog">是否是【强制】日志</param>
        /// <returns>可用返回true；否则返回false</returns>
        bool IsEnable(LogLevel level, bool forceLog = false);

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="descriptor">要记录的日志信息</param>
        /// <returns>记录成功；返回true</returns>
        bool Log(LogDescriptor descriptor);

        /// <summary>
        /// 创建新作用域日志记录器 <br />
        ///     1、基于<paramref name="title"/>创建一条唯一主键Id标记日志，后续日志将归属于此Id组，和其他日志区分开<br />
        ///     2、一般在进行多线程操作时，子线程之间日志做归组使用，方便查看日志层级<br />
        /// </summary>
        /// <param name="title">日志标题；将作为后续日志组的组名</param>
        /// <param name="content">日志内容；日志组日志的内容信息</param>
        /// <returns>新的日志管理，此管理器下的日志合并到同一组中</returns>
        ILogger Scope(string title, string? content = null);
    }
}

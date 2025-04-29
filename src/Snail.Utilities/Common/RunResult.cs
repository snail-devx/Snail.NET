namespace Snail.Utilities.Common
{
    /// <summary>
    /// 运行结果信息
    /// </summary>
    public class RunResult
    {
        #region 属性变量
        /// <summary>
        /// 运行成功
        /// </summary>·
        public static readonly RunResult SUCCESS = new RunResult(true);
        /// <summary>
        /// 运行失败
        /// </summary>
        public static readonly RunResult FAILED = new RunResult(false);

        /// <summary>
        /// 运行是否成功
        /// </summary>
        public bool Success { private init; get; }

        /// <summary>
        /// 运行过程中的异常信息对象，一般在运行失败时传入
        /// </summary>
        public Exception? Exception { private init; get; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="success">运行是否成功</param>
        /// <param name="ex">运行过程中的异常信息对象</param>
        public RunResult(in bool success, in Exception? ex = null)
        {
            Success = success;
            Exception = ex;
        }
        #endregion

        #region 公共方法
        #endregion

        #region 转换
        /// <summary>
        /// 布尔转换成运行结果
        /// </summary>
        /// <param name="success">是否成功</param>
        public static implicit operator RunResult(bool success)
            => success ? SUCCESS : FAILED;
        /// <summary>
        /// 异常转换成运行结果：运行失败，并记录异常对象
        /// </summary>
        /// <param name="ex">运行时的异常信息对象</param>
        public static implicit operator RunResult(Exception ex)
            => new RunResult(false, ex);

        /// <summary>
        /// RunResult转成Boolean，获取运行是否成功
        /// </summary>
        /// <param name="rt"></param>
        public static implicit operator bool(RunResult rt)
            => rt.Success;
        /// <summary>
        /// RunResult转成Exception，获取运行异常信息
        /// </summary>
        /// <param name="rt"></param>
        public static implicit operator Exception?(RunResult rt)
            => rt.Exception;
        #endregion
    }

    /// <summary>
    /// 运行结果；支持泛型运行结果数据
    /// </summary>
    /// <typeparam name="T">运行结果数据</typeparam>
    public sealed class RunResult<T> : RunResult
    {
        #region 属性变量
        /// <summary>
        /// 运行成功
        /// </summary>
        public static new readonly RunResult<T> SUCCESS = new RunResult<T>(true);
        /// <summary>
        /// 运行失败
        /// </summary>
        public static new readonly RunResult<T> FAILED = new RunResult<T>(false);

        /// <summary>
        /// 结果结果数据
        /// </summary>
        public T? Data { private init; get; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="success">运行是否成功</param>
        /// <param name="ex">运行过程中的异常信息对象</param>
        /// <param name="data">结果结果数据</param>
        public RunResult(in bool success, T? data = default, Exception? ex = null) : base(success, ex)
        {
            Data = data;
        }
        #endregion

        #region 转换
        /// <summary>
        /// 布尔转换成运行结果
        /// </summary>
        /// <param name="success">是否成功</param>
        public static implicit operator RunResult<T>(bool success)
            => success ? SUCCESS : FAILED;
        /// <summary>
        /// 异常转换成运行结果：运行失败，并记录异常对象
        /// </summary>
        /// <param name="ex">运行时的异常信息对象</param>
        public static implicit operator RunResult<T>(Exception ex)
            => new RunResult<T>(false, ex: ex);

        /// <summary>
        /// RunResult转成Boolean，获取运行是否成功
        /// </summary>
        /// <param name="rt"></param>
        public static implicit operator bool(RunResult<T> rt)
            => rt.Success;
        /// <summary>
        /// RunResult转成Exception，获取运行异常信息
        /// </summary>
        /// <param name="rt"></param>
        public static implicit operator Exception?(RunResult<T> rt)
            => rt.Exception;

        /// <summary>
        /// RunResult{T}转成运行返回结果
        /// </summary>
        /// <param name="rt"></param>
        public static implicit operator T?(RunResult<T> rt)
            => rt.Data;
        /// <summary>
        /// 运行返回值转成RunResult{T}
        /// </summary>
        /// <param name="data">运行结果数据</param>
        public static implicit operator RunResult<T>(T? data)
            => new RunResult<T>(true, data, ex: null);
        #endregion
    }
}

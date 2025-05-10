using System.Collections.Generic;
using Snail.Aspect.Common.Interfaces;

namespace Snail.Aspect.Common.Components
{
    /// <summary>
    /// 方法执行时的上下文；实现方法执行拦截，切面注入逻辑<br />
    ///     1、配合<see cref="IMethodRunHandle"/>使用<br />
    ///     2、记录方法名称、参数等信息<br />
    ///     3、存储、修改方法返回值数据
    /// </summary>
    public sealed class MethodRunContext
    {
        #region 属性变量
        /// <summary>
        /// 执行的方法名称
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// 方法传入的参数
        /// </summary>
        public IReadOnlyDictionary<string, object> Parameters { get; }

        /// <summary>
        /// 执行方法的返回值；若方法为void或者Task，则无返回值
        /// </summary>
        public object ReturnValue { private set; get; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        public MethodRunContext(string method, Dictionary<string, object> parameters)
        {
            Method = method;
            Parameters = parameters;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 设置方法返回值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public T SetReturnValue<T>(T data)
        {
            ReturnValue = data;
            return data;
        }
        #endregion
    }
}

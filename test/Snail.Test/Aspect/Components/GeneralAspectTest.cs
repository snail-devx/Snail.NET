using Snail.Aspect.General.Components;
using Snail.Aspect.General.Interfaces;

namespace Snail.Test.Aspect.Components
{
    /// <summary>
    /// 通用的 切面编程 方法测试；自己实现方法拦截接口
    /// </summary>
    [MethodAspect(Interceptor = "GeneralAspectTest")]
    abstract class GeneralAspectTest
    {
        #region 公共方法
        //public abstract void Testabstract();

        public virtual void TesVoid()
        { }
        public virtual async Task TestTask(string x)
        {
            await Task.Yield();
        }
        public virtual string TestString()
        {
            return "TestString";
        }

        public virtual async Task<string> TestTaskString()
        {
            await Task.Yield();
            return "TestTaskString";
        }
        #endregion
    }


    /// <summary>
    /// 接口实现：方法运行句柄
    /// </summary>
    [Component<IMethodInterceptor>(Key = "GeneralAspectTest")]
    public class MethodRunHandleTest : IMethodInterceptor
    {
        #region IMethodHandler
#pragma warning disable Snail_Warning
        /// <summary>
        /// 异步方法运行时
        /// <para>1、方法执行时若需要返回值，则通过<paramref name="context"/>属性<see cref="MethodRunContext.ReturnValue"/>属性值 </para>
        /// </summary>
        /// <param name="next">下一个动作代码委托</param>
        /// <param name="context">方法运行的上下文参数</param>
        /// <returns></returns>
        async Task IMethodInterceptor.InterceptAsync(Func<Task> next, MethodRunContext context)
        {
            await next.Invoke().ConfigureAwait(false);
            if (context.Method == "TestTaskString")
            {
                context.ReturnValue = "修改返回值";
            }
        }

        /// <summary>
        /// 同步方法运行时
        /// <para>1、方法执行时若需要返回值，则通过<paramref name="context"/>属性<see cref="MethodRunContext.ReturnValue"/>属性值 </para>
        /// </summary>
        /// <param name="next">下一个动作代码委托</param>
        /// <param name="context">方法运行的上下文参数</param>
        void IMethodInterceptor.Intercept(Action next, MethodRunContext context)
        {
            next.Invoke();
        }
#pragma warning restore Snail_Warning
        #endregion
    }
}

using System.Diagnostics;
using System.Reflection;

namespace Snail.Utilities.Common.Utils
{
    /// <summary>
    /// 诊断助手类
    /// </summary>
    public static class DiagnosticsHelper
    {
        #region 属性变量
        /// <summary>
        /// 当前类型，做排除
        /// </summary>
        private static readonly Type _type = typeof(DiagnosticsHelper);
        #endregion

        #region 公共方法
        /// <summary>
        /// 获取执行入口方法；从<see cref="StackFrame"/> 执行堆栈中查找
        /// </summary>
        /// <param name="excludeType">排除的类型；即执行堆栈是在此类型时，跳过，继续往上游找</param>
        /// <returns></returns>
        public static MethodBase? GetEntryMethod(Type? excludeType)
        {
            //foreach (StackFrame sf in new StackTrace().GetFrames())
            //{
            //    entryMethod = sf.GetMethod();
            //    if (entryMethod?.DeclaringType != _type)
            //    {
            //        break;
            //    }
            //}

            MethodBase? entry = null;
            foreach (StackFrame sf in new StackTrace().GetFrames())
            {
                entry = sf.GetMethod();
                //  如果是有编译器生成的代码，这里直接继续往下找
                if (entry?.DeclaringType != null && entry.DeclaringType != _type && entry.DeclaringType != excludeType)
                {
                    break;
                }
                entry = null;
            }
            return entry;
        }

        /// <summary>
        /// 获取执行堆栈中有指定特性标记的方法
        /// </summary>
        /// <typeparam name="Attr"></typeparam>
        /// <param name="excludeType">排除的类型；即执行堆栈是在此类型时，跳过，继续往上游找</param>
        /// <param name="attr"></param>
        /// <returns></returns>
        public static MethodBase? GetEntryMethod<Attr>(Type? excludeType, out Attr? attr) where Attr : Attribute
        {
            MethodBase? entry = null;
            attr = null;
            foreach (StackFrame sf in new StackTrace().GetFrames())
            {
                entry = sf.GetMethod();
                if (entry?.DeclaringType != null && entry.DeclaringType != _type && entry.DeclaringType != excludeType)
                {
                    attr = entry.GetCustomAttribute<Attr>();
                    if (attr != null)
                    {
                        break;
                    }
                }
            }
            return attr == null ? null : entry;
        }
        #endregion
    }
}

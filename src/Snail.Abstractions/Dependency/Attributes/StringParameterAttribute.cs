using Snail.Abstractions.Dependency.Interfaces;

namespace Snail.Abstractions.Dependency.Attributes
{
    /// <summary>
    /// 特性标签：【依赖注入】构建实例时的构造方法注入的<see cref="string"/>类型参数值 <br />
    ///     1、仅在【属性、字段、方法参数】的标签上生效
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
    public sealed class StringParameterAttribute : Attribute, IParameter<string>
    {
        #region 属性变量
        /// <summary>
        /// 字符串参数值
        /// </summary>
        public string? Value { init; get; }
        #endregion

        #region IParameter
        /// <summary>
        /// 参数名称：和<see cref="Type"/>配合使用，选举要传递信息的目标参数 <br />
        /// 1、Name为空时，则选举第一个类型为Type的参数
        /// 2、Name非空时，则选举类型为Type、且参数名为Name的参数
        /// </summary>
        public string? Name { init; get; }

        /// <summary>
        /// 获取参数值；由外部自己构建
        /// </summary>
        /// <param name="manager">DI管理器实例</param>
        /// <returns></returns>
        string? IParameter<string>.GetParameter(in IDIManager manager) => Value;
        #endregion
    }
}

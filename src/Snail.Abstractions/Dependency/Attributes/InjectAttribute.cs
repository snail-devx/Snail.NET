using Snail.Abstractions.Dependency.Interfaces;

namespace Snail.Abstractions.Dependency.Attributes
{
    /// <summary>
    /// 特性标签：注入配置，在不同对象作用不一样 <br />
    ///     1、在构造方法上：DI选举构造方法时，优先选择有此特性的第一个构造方法；无此特性则选举参数最长的构造方法 <br />
    ///     2、在属性、字段上：DI构建完实例后，再通过DI自动构建属性、字段值；前提是属性可写 <br />
    ///     3、在其他方法时：DI构建完实例后，执行此方法进行个性化初始化配置；前提是此方法为【实例方法】 <br />
    ///     4、在方法参数上：DI执行构造方法、注入方法前，基于DI构建此参数值（无此特性的参数，走参数类型默认值） <br />
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter,
        AllowMultiple = false,
        Inherited = false
    )]
    public class InjectAttribute : Attribute, IInject
    {
        #region 属性变量
        /// <summary>
        /// 依赖注入Key值，用于DI动态构建实例 <br />
        /// 1、用于区分同一个源（From）多个实现（to）的情况 <br />
        /// 2、默认行为：值为null；在【Constructor】和【Method】使用时忽略此属性
        /// </summary>
        public string? Key { init; get; }
        #endregion

        #region IInject
        /// <summary>
        /// 依赖注入Key值，用于DI动态构建实例 <br />
        ///     1、用于区分同一个源（From）多个实现（to）的情况 <br />
        ///     2、默认值为null
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        string? IInject.GetKey(IDIManager manager) => Key;
        #endregion
    }
}

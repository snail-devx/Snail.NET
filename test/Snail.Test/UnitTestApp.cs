namespace Snail.Test
{
    /// <summary>
    /// 单元测试应用；
    ///     1、集成<see cref="IApplication"/>实例
    /// </summary>
    public class UnitTestApp
    {
        #region 属性变量
        /// <summary>
        /// 应用实例
        /// </summary>
        public readonly IApplication App;
        #endregion

        #region 测试方法
        /// <summary>
        /// 构造方法
        /// </summary>
        public UnitTestApp()
        {
            App = new Application();
            App.Run();
        }
        #endregion
    }
}

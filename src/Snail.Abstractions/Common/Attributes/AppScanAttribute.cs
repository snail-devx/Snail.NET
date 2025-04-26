namespace Snail.Abstractions.Common.Attributes
{
    /// <summary>
    /// 特性标签：应用程序扫描
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class AppScanAttribute : Attribute
    {
        /// <summary>
        /// 扫描顺序，越大越后扫描
        /// </summary>
        public int Order { init; get; }
    }
}

namespace Snail.Abstractions.Common.Delegates
{
    /// <summary>
    /// 委托：应用扫描器
    /// </summary>
    /// <param name="type">扫描到的类型</param>
    /// <param name="attributes">类型的自定义特性标签</param>
    public delegate void AppScanDelegate(Type type, ReadOnlySpan<Attribute> attributes);
}

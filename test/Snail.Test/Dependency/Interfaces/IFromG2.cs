namespace Snail.Test.Dependency.Interfaces
{
    /// <summary>
    /// 接口：IF2，泛型接口，两个泛型参数
    /// </summary>
    public interface IFromG2<I1, I2>
    {
        I1? T_1 { get; set; }
        I2? T_2 { get; set; }
    }
}

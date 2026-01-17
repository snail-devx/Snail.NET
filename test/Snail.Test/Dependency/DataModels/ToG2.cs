using Snail.Test.Dependency.Interfaces;

namespace Snail.Test.Dependency.DataModels
{
    /// <summary>
    /// 实体，实现<see cref="IFromG2{I1, I2}"/>
    /// </summary>
    /// <typeparam name="C1"></typeparam>
    /// <typeparam name="C2"></typeparam>
    public class ToG2<C1, C2> : IFromG2<C1, C2>
    {
        public C1? T_1 { get; set; }
        public C2? T_2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ToG2()
        {

        }
    }

    public class ToG2_1<T> : ToG2<T, T>, IFromG2<T, T>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class ToG2_1 : ToG2<String, String>, IFromG2<String, String>
    {
    }


}

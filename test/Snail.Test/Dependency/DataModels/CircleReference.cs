using Snail.Test.Dependency.Interfaces;

namespace Snail.Test.Dependency.DataModels
{
    /// <summary>
    /// 循环引用
    /// </summary>
    public sealed class CircleReference
    {
        public CircleReference(CircleReference2 cr2, IFrom1 if1)
        {

        }
    }

    /// <summary>
    /// 循环引用
    /// </summary>
    public sealed class CircleReference2
    {
        public CircleReference2(CircleReference cr)
        {

        }
    }
}

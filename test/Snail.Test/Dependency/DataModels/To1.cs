using Snail.Test.Dependency.Interfaces;

namespace Snail.Test.Dependency.DataModels
{
    /// <summary>
    /// 实体：实现<see cref="IFrom1"/>
    /// </summary>
    public class To1 : IFrom1
    {

    }

    public abstract class To1_1_Abs : To1, IFrom1
    {

    }
    public class To1_1_ : To1_1_Abs
    {

    }

    /// <summary>
    /// 实体：实现<see cref="IFrom1"/>
    /// </summary>
    public class To1_1 : To1, IFrom1
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        public To1_1()
        {

        }
    }

    /// <summary>
    /// 文件注入时的类型
    /// </summary>
    public class To1_File : To1_1, IFrom1
    {

    }
}

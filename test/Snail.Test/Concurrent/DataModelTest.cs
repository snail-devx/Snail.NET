using Snail.Utilities.Collections;

namespace Snail.Test.Concurrent
{
    /// <summary>
    /// 【Concurrent】定义的实体测试
    /// </summary>
    public sealed class DataModelTest
    {
        #region LockList<T>测试
        [Test]
        public void LockTest()
        {
            //  List自身，在多线程操作时，add不一定能加到100个数据进去
            List<string> l = new List<string>();
            Parallel.For(0, 100, index => l.Add(index.ToString()));

            //  测试添加
            LockList<string> list = new LockList<string>();
            Parallel.For(0, 100, index => list.Add(index.ToString()));
            Assert.That(list.Count == 100, "LockList长度不对");
            list.Add("xxxxxxxxxx");
        }
        #endregion
    }
}

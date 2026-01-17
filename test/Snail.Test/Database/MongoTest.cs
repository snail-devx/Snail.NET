using MongoDB.Driver;
using Snail.Test.Database.DataModels;

namespace Snail.Test.Database
{
    /// <summary>
    /// Mongo数据库测试
    /// </summary>
    public sealed class MongoTest
    {
        #region 属性变量
        #endregion

        #region 公共方法
        /// <summary>
        /// 测试过滤条件表达式
        /// </summary>
        [Test]
        public void FilterExpressTest()
        {
            FilterDefinitionBuilder<TestDbModel> filter = Builders<TestDbModel>.Filter;

            var filterx = filter.Eq(m => m.Bool, true) | filter.Ne(m => m.BoolNull, null);

            string? x = filter.ToString();
        }
        #endregion
    }
}

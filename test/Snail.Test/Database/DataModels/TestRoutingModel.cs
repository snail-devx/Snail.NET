using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.Interfaces;

namespace Snail.Test.Database.DataModels
{
    /// <summary>
    /// 测试路由
    /// </summary>
    [DbTable(Name = "Snail_TestRoutingModel", Routing = true)]
    public sealed class TestRoutingModel : IDbRouting
    {
        /** ElasticSearch
        DELETE snail_testroutingmodel
        PUT snail_testroutingmodel
        {
            "mappings": {
                "dynamic": false,
                "properties": {
                "Id":{"type": "keyword"},
                "Routing":{"type": "keyword"},
                "Name":{"type": "keyword"}
                }
            }
        }
        */


        /// <summary>
        /// 主键Id
        /// </summary>
        [DbField(PK = true)]
        public string? Id { set; get; }

        /// <summary>
        /// 路由值；不保存数据库
        /// </summary>
        public required string Routing { set; get; }

        /// <summary>
        /// 字段名称
        /// </summary>
        public string? Name { set; get; }

        #region IDbModelRouting
        /// <summary>
        /// 获取实例的路由值
        /// </summary>
        /// <returns></returns>
        string IDbRouting.GetRouting() => Routing;
        #endregion
    }
}

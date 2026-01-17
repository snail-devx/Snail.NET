using Snail.Abstractions.Database.Attributes;

namespace Snail.Test.Database.DataModels
{
    /// <summary>
    /// 测试子文档，仅mongo和Elastic支持
    /// </summary>
    public class TestChildDocument
    {
        public string? ChildName { set; get; }

        [DbField(Ignored = true)]
        public string? ChildIgnore { set; get; }
    }
}

using Snail.Abstractions.Database.Attributes;

namespace Snail.Test.Database.DataModels
{
    /// <summary>
    /// 测试父级继承属性
    /// </summary>
    public class TestParentModel
    {
        /// <summary>
        /// 主键字段，配合MongoDB主键字段强制【_id】做测试，测试父级主键Id值
        /// </summary>
        public string? IdValue { set; get; }


        public string? ParentName { set; get; }

        /// <summary>
        /// 测试重写字段
        /// </summary>
        public string? Override { set; get; }

        [DbField(Ignored = true)]
        public string? ParentIgnore { set; get; }
    }
}

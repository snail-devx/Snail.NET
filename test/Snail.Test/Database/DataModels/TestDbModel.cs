using Snail.Abstractions.Database.Attributes;
using System.Linq.Expressions;

namespace Snail.Test.Database.DataModels
{
    /// <summary>
    /// 数据库测试实体
    /// </summary>
    [DbTable(Name = "Snail_TestModel"), DbCache]
    public sealed class TestDbModel : TestParentModel
    {
        /// <summary>
        /// 主键
        /// </summary>
        [DbField(Name = "Id", PK = true)]
        public new string? IdValue { set; get; }
        public string? String { set; get; }

        /// <summary>
        /// 重写父类字段；构造出 父、子有相同字段名的场景
        /// </summary>
        public new string? ParentName { set; get; }

        public string? Special { set; get; }


        [DbField(Name = "IntValue")]
        public int Int { set; get; }
        public int? IntNull { set; get; }

        public bool Bool { set; get; }
        public bool? BoolNull { set; get; }

        public char Char { set; get; }
        public char? CharNull { set; get; }

        public DateTime DateTime { set; get; }
        public DateTime? DateTimeNull { set; get; }

        public ExpressionType NodeType { set; get; }
        public ExpressionType? NodeTypeNull { set; get; }


        [DbField(Name = "MyOverride")]
        public new string? Override { set; get; }
    }
}

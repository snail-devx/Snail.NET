using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Snail.Test.Database.DataModels
{
    /// 数据过滤表达式描述器
    /// 1、进行表达式过滤条件测试时标注此表达式是否支持
    /// </summary>
    /// <param name="IsSupport">此表达式是否支持；不支持会报错</param>
    /// <param name="IsTextFilter">是否是文本搜索，做一下标记，关系型数据库始终不区分大小写</param>
    /// <param name="Lambda">要测试的表达式</param>
    internal record DbFilterExpressDescriptor(bool IsSupport, bool IsTextFilter, [NotNull] Expression<Func<TestDbModel, bool>> Lambda);
}

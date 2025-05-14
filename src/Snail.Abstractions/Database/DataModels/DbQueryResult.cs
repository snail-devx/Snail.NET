using Snail.Abstractions.Database.Attributes;

namespace Snail.Abstractions.Database.DataModels
{
    /// <summary>
    /// 数据库查询结果
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    public sealed class DbQueryResult<DbModel> where DbModel : class
    {
        /// <summary>
        /// 当前页数据
        /// </summary>
        public int? Page { set; get; }

        /// <summary>
        /// 最新的排序Key值 <br />
        ///     1、滚动获取下一页时，把上一页的最后一条数据排序Key回传回来 <br />
        ///     2、解决from+size分页效率问题，且ES中from+size默认不能大于1w <br />
        /// </summary>
        public string? LastSortKey { set; get; }

        /// <summary>
        /// 查询出来的数据集合
        /// </summary>
        public DbModel[] Datas { set; get; }

        #region 构造方法
        /// <summary>
        /// 无参构造方法
        /// </summary>
        public DbQueryResult()
            : this(datas: null)
        {
        }
        /// <summary>
        /// 构造方法 <br />
        ///     1、<paramref name="datas"/>赋值给<see cref="Datas"/> <br />
        ///     2、<see cref="Page"/>自动基于<see cref="Datas"/>赋值 <br />
        /// </summary>
        /// <param name="datas">结果数据值</param>
        public DbQueryResult(DbModel[]? datas)
        {
            //  无数据，给默认空数组
            Datas = datas ?? Array.Empty<DbModel>();
            Page = Datas.Length;
        }
        #endregion
    }
}

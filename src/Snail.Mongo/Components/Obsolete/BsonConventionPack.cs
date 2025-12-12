using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using Snail.Abstractions.Database.DataModels;
using Snail.Database.Utils;
using Snail.Mongo.Utils;
using System.Reflection;

namespace Snail.Mongo.Components.Obsolete;

/// <summary>
/// 自定义的Bson处理配置 
/// <para>1、主要对实体字段的映射做处理； </para>
/// <para>2、具体处理逻辑还是交给【IConvention】接口实现类，这里只做注册 </para>
/// </summary>
internal sealed class BsonConventionPack : IConventionPack
{
    #region 属性变量
    /// <summary>
    /// 转换配置集合
    /// </summary>
    private readonly List<IConvention> _Conventions;
    #endregion

    #region 构造方法
    /// <summary>
    /// 私有构造方法：暂时只给内部开放
    /// </summary>
    internal BsonConventionPack()
    {
        //  默认配置
        _Conventions = new List<IConvention>()
        {
            //  DbModel 处理：成员（字段、属性）处理
            new BsonClassMapConvention(),
            //  成员属性处理
            new BsonMemberMapConvention(),
        };
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 是否是有效类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsValidFilter(Type type)
    {
        if (type == null) return false;
        //  先判断简单一点，不能是值类型和基元类型。后期看情况再补充
        return type.IsValueType == false
            && type.IsPrimitive == false;
    }
    #endregion

    #region IConventionPack
    IEnumerable<IConvention> IConventionPack.Conventions => _Conventions;
    #endregion

    #region 私有类型
    /// <summary>
    /// Bson &lt;-> Class 转换处理
    /// </summary>
    private sealed class BsonClassMapConvention : IClassMapConvention
    {
        /// <summary>
        /// 接口名
        /// </summary>
        public string Name => "Bson <--> Class Mapper";

        /// <summary>
        /// 进行classmap处理
        /// </summary>
        /// <param name="classMap"></param>
        public void Apply(BsonClassMap classMap)
        {
            if (classMap == null) return;
            //  忽略扩展属性：解决反序列化时，class不存在对应字段/属性时报错的情况
            classMap.SetIgnoreExtraElements(true);
            classMap.SetIgnoreExtraElementsIsInherited(true);
            //  遍历现有的成员信息，进行DbFieldAttribute标记处理：字段名、主键、忽略属性逻辑。需要对DbModel做特定逻辑
            DbModelTable? descriptor = DbModelHelper.IsDbModel(classMap.ClassType) == true
                    ? DbModelHelper.GetTable(classMap.ClassType)
                    : null;
            foreach (string memberName in classMap.DeclaredMemberMaps.Select(item => item.MemberName).ToList())
            {
                BsonMemberMap member = classMap.DeclaredMemberMaps.First(item => item.MemberName == memberName);
                //  1、找到此成员对应的数据字段特性标签：区分DbModel和非DbModel《DbModel继承父类时》
                bool needDelete = false, isPK = false;
                string? dbFieldName = null;
                //      DbModel实例：则查询配置判断存在性；若数据字段找不到，则需要干掉
                if (descriptor != null)
                {
                    DbModelField? dbField = descriptor.Fields.FirstOrDefault(df => df.Property.Name == member.MemberName);
                    if (dbField != null)
                    {
                        dbFieldName = dbField.Name;
                        isPK = dbField.PK == true;
                        continue;
                    }
                    needDelete = true;
                }
                //      非数据库字段，直接看是否进行了DbField约束，有则进行重命名；否则忽略
                else
                {
                    DbFieldAttribute? attr = member.MemberInfo.GetCustomAttribute<DbFieldAttribute>();
                    if (attr != null)
                    {
                        needDelete = attr.Ignored;
                        dbFieldName = attr.Name;
                        isPK = attr.PK;
                    }
                }
                //  2、统一做字段处理：删除字段、主键字段处理、字段别名约束
                if (needDelete == true)
                {
                    classMap.UnmapMember(member.MemberInfo);
                }
                else if (isPK == true)
                {
                    classMap.SetIdMember(member);
                }
                else if (string.IsNullOrEmpty(dbFieldName) == false)
                {
                    member.SetElementName(dbFieldName);
                }
            }
        }
    }

    /// <summary>
    /// Class中属性名称映射转换映射
    ///     确保<see cref="DbFieldAttribute"/>时，始终以此为准,而不是以<see cref="BsonElementAttribute"/>
    /// </summary>
    private sealed class BsonMemberMapConvention : IMemberMapConvention
    {
        /// <summary>
        /// 接口名
        /// </summary>
        public string Name => "BsonMember <--> DbFieldAttribute";

        /// <summary>
        /// 进行memebermap处理
        /// </summary>
        /// <param name="memberMap"></param>
        public void Apply(BsonMemberMap memberMap)
        {
            /** 
             *  1、字段名称+主键字段处理：
             *      1、确保有<see cref="DbFieldAttribute"/>时，强制已此此段名作为ElementName，即使有<see cref="BsonElementAttribute"/>约束也不行
             *      2、这里兜底：BsonClassMap构建时，IMemberMapConvention类型处理，会放到最后，避免mongo自带的【IMemberMapConvention】影响
             *  2、注册MemberMap的序列化判断逻辑：
             *      解决“子类重写（new）父级属性后，父级属性仍然被序列化保存入库”的问题
             */
            if (memberMap == null) return;
            //  查找DbFieldAttribute特性
            DbFieldAttribute? dbField = memberMap.MemberInfo.GetCustomAttribute<DbFieldAttribute>();
            if (dbField != null)
            {
                string? elementName = null;
                if (dbField.PK == true)
                {
                    elementName = MongoHelper.PK_FIELDNAME;
                }
                else
                {
                    elementName = dbField.Name ?? memberMap.MemberName;
                }
                memberMap.SetElementName(elementName);
            }
            //  设置序列化判断委托：解决序列化重写属性问题
            memberMap.SetShouldSerializeMethod(new BsonMemberMapSerializeJudger(memberMap).Judge);
        }
    }

    /// <summary>
    /// DbModel的属性序列化判断器
    ///     
    /// </summary>
    private sealed class BsonMemberMapSerializeJudger
    {
        #region 属性变量
        /// <summary>
        /// 当前的Member对象
        /// </summary>
        private readonly BsonMemberMap _MemberMap;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="memberMap"></param>
        public BsonMemberMapSerializeJudger(BsonMemberMap memberMap) => _MemberMap = memberMap;
        #endregion

        #region 公共方法
        /// <summary>
        /// 判断是否需要进行序列化
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public bool Judge(object? instance)
        {
            //  解决“子类重写（new）父级属性后，父级属性仍然被序列化保存入库”的问题
            if (instance == null)
            {
                return false;
            }
            Type type = instance.GetType();
            return MongoHelper.InferBsonMemberMap(type, _MemberMap.MemberName) == _MemberMap;
        }
        #endregion
    }
    #endregion
}
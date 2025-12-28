using Snail.Abstractions.Database.DataModels;
using Snail.Database.Utils;
using Snail.Mongo.Components;
using System.Runtime.CompilerServices;

namespace Snail.Mongo.Utils;

/// <summary>
/// MongoDB数据库访问时相关助手类
/// </summary>
public static class MongoHelper
{
    #region 属性变量
    /// <summary>
    /// 主键Id字段名
    /// </summary>
    public const string PK_FIELDNAME = "_id";
    #endregion

    #region 构造方法
    /// <summary>
    /// 静态构造方法
    /// </summary>
    static MongoHelper()
    {
        //  DbModel采用新的ClassMap注册方式，不用在这里注册自定义序列化器去解决序列化和反序列化的问题
        /**  详细参照 <see cref="RegisterClassMap(Type)"/>*/
        //      对BsonMemberMap的扩展：强制固定注册名为“__attributes__”
        /**     主要是ConventionRegistry进行Lookup时，会把此名配置始终放到最后**/
        //ConventionRegistry.Register("__attributes__", new BsonConventionPack(), BsonConventionPack.IsValidFilter);

        //  mongo序列化自定义配置：对特定类型的序列化做适配
        BsonSerializer.RegisterSerializationProvider(new BsonSerializationProvider());
    }
    #endregion

    #region 公共方法

    #region 序列化和反序列化
    /// <summary>
    /// 【Bson序列化】构建DbModel的BsonDocument对象
    ///     内部会自动验证主键Id非空
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="model"></param>
    public static BsonDocument BuildDocument<DbModel>(DbModel model) where DbModel : class
    {
        //  先留口，后续考虑Child子文档和子表形式的处理。此时构建时，需要把子表数据单独拎出来
        ThrowIfNull(model);
        BsonDocument document = model.ToBsonDocument();
        //  验证主键id
        BsonValue pkValue = document.GetValue(PK_FIELDNAME);
        if (pkValue == null || pkValue == BsonNull.Value)
        {
            throw new ApplicationException($"主键Id值为null，无法执行Save操作");
        }
        return document;
    }

    /// <summary>
    /// 【Bson反序列化】构建BsonDocument的数据实体对象
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="document"></param>
    /// <returns></returns>
    public static DbModel? BuildDbModel<DbModel>(BsonDocument? document) where DbModel : class
    {
        //  之前是想着在这里进行属性字段反射构建合理数据；但最终搞到了BsonClassMap中。这里代码就简化了

        //  先留口，方便后去对文档做处理
        return document == null
            ? null
            : BsonSerializer.Deserialize<DbModel>(document);
    }
    #endregion

    #region 数据库对象构建
    /// <summary>
    /// 获取MongoDB数据对象
    /// </summary>
    /// <param name="dbServer">数据库服务器配置</param>
    /// <returns></returns>
    public static IMongoDatabase GetDatabase(DbServerDescriptor dbServer)
    {
        ThrowIfNull(dbServer);
        if (dbServer.DbType != DbType.MongoDB)
        {
            throw new ApplicationException($"非MongoDB数据库类型.{dbServer.DbType}");
        }
        MongoClient client = new MongoClient(dbServer.Connection);
        return client.GetDatabase(dbServer.DbName);
    }
    /// <summary>
    /// 构建数据库连接
    /// </summary>
    /// <typeparam name="T">连接对象类型</typeparam>
    /// <param name="dbServer">数据库服务器配置</param>
    /// <param name="tableName">数据库名</param>
    /// <returns></returns>
    public static IMongoCollection<T> CreateCollection<T>(DbServerDescriptor dbServer, String tableName)
    {
        ThrowIfNullOrEmpty(tableName);
        return GetDatabase(dbServer).GetCollection<T>(tableName);
    }
    /// <summary>
    /// 基于数据库的数据实体对象构建数据库连接
    ///     1、分析出DbModel对应的特性标签，从而分析出数据表名称
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="dbServer">数据库服务器配置</param>
    /// <returns></returns>
    public static IMongoCollection<DbModel> CreateCollection<DbModel>(DbServerDescriptor dbServer) where DbModel : class
    {
        DbModelTable table = DbModelHelper.GetTable<DbModel>();
        return CreateCollection<DbModel>(dbServer, table.Name);
    }
    /// <summary>
    /// 基于数据库的数据实体对象构建基于BsonDocument对象的数据库连接
    ///     1、分析出DbModel对应的特性标签，从而分析出数据表名称
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="dbServer">数据库服务器配置</param>
    /// <returns></returns>
    public static IMongoCollection<BsonDocument> CreateBsonCollection<DbModel>(DbServerDescriptor dbServer) where DbModel : class
    {
        DbModelTable table = DbModelHelper.GetTable<DbModel>();
        return CreateCollection<BsonDocument>(dbServer, table.Name);
    }
    #endregion

    #region BsonClassMap、BsonMemberMap相关
    /// <summary>
    /// 注册类型的ClassMap
    ///     1、确保在使用之前先注册，并只注册一次；否则可能导致重复注册
    ///     2、提前进行bson相关注册；并进行字段名称、主键、new重写属性等处理
    ///     3、若在<see cref="MongoProvider{DbModel,IdType}"/>子类中使用，则会自动注册；不用重复调用
    /// </summary>
    /// <param name="type">必须得是<see cref="DbTableAttribute"/>标记的数据库实体类型；否则会报错</param>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static DbModelTable RegisterClassMap(Type type)
    {
        /*  MongoDB默认BsonClassMap.LookupClassMap无法对new重写属性做排除
         *  解决方式：自定义构建ClassMap然后做注册；构建时自动把无效字段（忽略、new重写）排除掉
         */

        //  1、判断类型是否合法：必须得是DbModel有效类型
        DbModelHelper.IsDbModel(type, true);
        DbModelTable table = DbModelHelper.GetTable(type);
        //  2、构建ClassMap进行注册：验证是否注册了，若已经注册了则直接报错
        if (BsonClassMap.IsClassMapRegistered(type) == true)
        {
            /*
             *  先不报错也不判断。能进入此判断条件，仅限于
             *      1、同一个DbModel被多个 DbProvider注册，如先使用默认Provider，再使用自定义Provider
             *          这种情况，不用管，直接返回即可。
             *      2、同一个DbModel先使用MongDB自带驱动操作数据，再使用DbProvider操作数据
             *          这种情况则可能出现错误
             *              1、类型有new操作进行属性重写
             *              2、数据库中字段在C#类中不存在
             *      整体先不报错，出现概率较小，出错了再排查即可
             */
            return table;
            //throw new ApplicationException($"MongoHelper.RegisterClassMap：重复注册了。type：{type.FullName}");
        }
        BsonClassMap classMap = BuildClassMap(type, table)!;
        //      忽略扩展属性：解决反序列化时，class不存在对应字段/属性时报错的情况
        classMap.SetIgnoreExtraElements(true);
        classMap.SetIgnoreExtraElementsIsInherited(true);
        //      注册
        BsonClassMap.RegisterClassMap(classMap);
        //  3、返回数据库描述器对象
        return table;
    }

    /// <summary>
    /// 基于C#成员名称，推断类型中的最符合条件的成员信息
    /// </summary>
    /// <param name="type"></param>
    /// <param name="memberName"></param>
    /// <returns></returns>
    public static BsonMemberMap? InferBsonMemberMap(Type type, string memberName)
    {
        /***
         *  解决问题：
         *      1、解决“子类重写（new）父级属性后，父级属性仍然被序列化保存入库”的问题
         *      2、解决“linq表达式推断成员序列化信息时，默认升序直接找到父级成员属性”的问题
         *  整体原则：优先从子级匹配成员属性信息；尽可能做到子类重写后，属性推断则忽略父级成员属性
         *  处理思路：
         *      1、取出当前类型的ClassMap，基于MemberName找出符合条件的MemberMap，可能存在多个
         *      2、基于type逐级向上筛选，最靠近当前instance类型的member
         *      3、若member==当前MemberMap，则返回true；否则返回false
         *  注意事项：
         *      1、这种处理思路，在如下情况时有bug：
         *          子类重写（new）父类属性A，但子类中A标记了忽略，父类中A未标记；此时仍然推断返回父级成员信息
         *          因为子类的ClassMap.AllMember中，属性A标记到了父类中
         *          PS：这种情况先不考虑，业务上这么用的话，是有明显bug的。又想重写且忽略？什么毛病需求
         */
        ThrowIfNull(type);
        ThrowIfNull(memberName);
        BsonClassMap classMap = BsonClassMap.LookupClassMap(type);
        IEnumerable<BsonMemberMap> maps = classMap.AllMemberMaps.Where(item => item.MemberName == memberName);
        int mapsCount = maps.Count();
        if (mapsCount == 0)
        {
            return null;
        }
        else if (mapsCount == 1)
        {
            return maps.FirstOrDefault();
        }
        //  匹配出多个时，找到最靠近传入type类型中定义的成员信息
        else
        {
            BsonMemberMap? member = null;
            while (type != null)
            {
                member = maps.FirstOrDefault(item => item.MemberInfo.DeclaringType == type);
                if (member != null)
                {
                    break;
                }
                type = type.BaseType!;
            }
            return member;
        }
    }
    #endregion

    #region 数据操作相关
    /// <summary>
    /// 构建指定字段的过滤条件；
    /// </summary>
    /// <typeparam name="FieldType"></typeparam>
    /// <param name="dbField">数据库字段</param>
    /// <param name="fieldValues">字段值；多个则用in查询，单个用==查询</param>
    /// <returns>{fieldName:{$in:[fieldValue,fieldValue2......]}}</returns>
    public static BsonDocument BuildFieldFilter<FieldType>(DbModelField dbField, IList<FieldType> fieldValues)
    {
        ThrowIfNull(dbField);
        ThrowIfNullOrEmpty(fieldValues, "fieldValues为null或者空数组");
        BsonValue[] values = fieldValues
            .Select(value => BsonValue.Create(DbModelHelper.BuildFieldValue(value!, dbField)))
            .ToArray();
        string fieldName = dbField.PK == true ? PK_FIELDNAME : dbField.Name;
        //  单个和多个的区别
        return values.Length == 1
            ? new BsonDocument(fieldName, values.First())
            : new BsonDocument(fieldName, new BsonDocument("$in", BsonArray.Create(values)));
    }

    /// <summary>
    /// 构建更新时的数据
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static UpdateDefinition<DbModel> BuildUpdate<DbModel>(IDictionary<string, object?> data)
    {
        ThrowIfNull(data);
        List<UpdateDefinition<DbModel>> updates = [];
        //  利用mongo序列化逻辑进行更新构建：DbModel已经把序列化逻辑，构建到了Bosn的BsonClassMap中
        foreach (var (fieldName, fieldValue) in data)
        {
            UpdateDefinition<DbModel> update = Builders<DbModel>.Update.Set(fieldName, fieldValue);
            updates.Add(update);
        }
        if (updates.Count == 0)
        {
            string msg = "无更新值，无法构建Mongo更新文档";
            throw new ApplicationException(msg);
        }
        return Builders<DbModel>.Update.Combine(updates);
    }
    #endregion

    #region 聚合管道相关

    #endregion

    #endregion

    #region 私有方法
    /// <summary>
    /// 构建指定类型的ClassMap对象
    ///     1、内部自动处理new重写属性
    ///     2、自动处理字段名称映射
    ///     3、自动处理主键等
    /// </summary>
    /// <param name="type"></param>
    /// <param name="table"></param>
    /// <returns></returns>
    private static BsonClassMap? BuildClassMap(Type? type, DbModelTable table)
    {
        /*自动往父级查找，和descriptor给定表字段进行比对，最终确定要那些classmember*/
        //  1、先构建父类的
        if (type == null)
        {
            return null;
        }
        BsonClassMap? pMap = BuildClassMap(type.BaseType, table);
        //  2、基于父类构建自身
        BsonClassMap map = new(type, pMap);
        map.AutoMap();
        //  3、处理字段信息：排除无效字段；映射DbField配置
        List<BsonMemberMap> dels = [];
        foreach (var member in map.DeclaredMemberMaps)
        {
            //  成员是否在descriptor中：不在则删除掉；若在，判断定义类型是否是当前type，解决new重写时忽略父级属性需求
            DbModelField? field = table.Fields.FirstOrDefault(df => df.Property.Name == member.MemberInfo.Name);
            if (field == null || field.Property.DeclaringType != member.MemberInfo.DeclaringType)
            {
                dels.Add(member);
            }
            //  成员有效，梳理字段名和主键字段，主键字段强制_id字段名
            else if (field.PK == true)
            {
                member.SetElementName(PK_FIELDNAME);
                map.SetIdMember(member);
            }
            else
            {
                member.SetElementName(field.Name);
            }
        }
        dels.ForEach(del => map.UnmapMember(del.MemberInfo));
        //  返回
        return map;
    }
    #endregion
}

using MongoDB.Bson.Serialization.Serializers;
using Snail.Mongo.Utils;
using static Snail.Database.Components.DbModelProxy;

namespace Snail.Mongo.Components;

/// <summary>
/// Bson序列化提供程序
/// </summary>
internal sealed class BsonSerializationProvider : BsonSerializationProviderBase
{
    #region 属性变量
    #endregion

    #region 重写父类方法
    /// <summary>
    /// 获取序列化器
    /// </summary>
    /// <param name="type"></param>
    /// <param name="serializerRegistry"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public override IBsonSerializer? GetSerializer(Type type, IBsonSerializerRegistry serializerRegistry)
    {
        if (type == null)
        {
            return null;
        }
        //  1、日期类型，强制local序列化，解决默认始终utc时区的问题
        if (type == typeof(DateTime))
        {
            return new DateTimeSerializer(DateTimeKind.Local);
        }

        //  这里判断一下，如果是DbModel，则看看是否注册了BsonClassMap，没注册则做一下兜底
        //      解决问题：部分apiModel返回值中用到了DbModel，此时dbModel若没注册，则会走mongo自带序列化逻辑，可能导致new、dbfield特性失效
        //      这里进做兜底注册，不管序列化器示例构建，交给mongo自己处理
        if (IsDbModel(type) == true)
        {
            MongoHelper.TryRegisterClassMap(type);
        }

        //  DbModel采用新的ClassMap注册方式，不用在这里注册自定义序列化器去解决序列化和反序列化的问题
        /**  详细参照 <see cref="MongoHelper.RegisterClassMap(Type)"/>*/
        return null;

        //  兜底：针对DbTableAttribute标记的class，走个性化序列化器；解决子类重写父类属性时，linq表达式会匹配的却是父级属性的问题
        /*      若是针对所有类型做此判断，会导致mongo驱动报错： 
         *          System.TimeoutException : A timeout occurred after 30000ms selecting a server using CompositeServerSelector........
         *          MongoDB.Driver.MongoConnectionException: An exception occurred while opening a connection to the server.
         *          MongoDB.Driver.MongoCommandException: Command isMaster failed: no such cmd: AllowDuplicateNames.
         *      具体原因未查：
         *          比较诡异的是：即使是把mongo自带的构建BsonClassProvider的逻辑拿过来，一行代码不改，都会报错
         *          猜测是自定义的BsonClassSerializer影响到了系统特定逻辑，暂时做一下容错
        if (DbModelHelper.IsDbModel(type) == true)
        {
            var classMap = BsonClassMap.LookupClassMap(type);
            type = typeof(BsonClassMapSerializer<>).MakeGenericType(type);
            return (IBsonSerializer)Activator.CreateInstance(type, classMap);
        }
        //      不符合条件时，返回null
        return null;
        */
    }
    #endregion

    #region 私有类型
    /// <summary>
    /// Bson&lt;->Class之间的序列化和反序列化
    ///     1、解决linq对override属性的错误性判断处理
    /// </summary>
    private sealed class BsonClassMapSerializer<TClass> : MongoDB.Bson.Serialization.BsonClassMapSerializer<TClass>, IBsonDocumentSerializer
    {
        #region 属性变量
        /// <summary>
        /// 当前class的类型
        /// </summary>
        private static readonly Type _Type = typeof(TClass);
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="classMap"></param>
        public BsonClassMapSerializer(BsonClassMap classMap) : base(classMap)
        {
        }
        #endregion

        #region IBsonDocumentSerializer
        /// <summary>
        /// 尝试获取class中成员的序列化信息
        /// </summary>
        /// <param name="memberName"></param>
        /// <param name="serializationInfo"></param>
        /// <returns></returns>
        bool IBsonDocumentSerializer.TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo? serializationInfo)
        {
            //  解决子类重写父级属性时，linq查询匹配时，默认匹配到父级属性的情况。实际上应该是优先匹配子级配置
            BsonMemberMap? member = MongoHelper.InferBsonMemberMap(_Type, memberName);
            if (member == null)
            {
                serializationInfo = null;
            }
            else
            {
                IBsonSerializer serializer = member.GetSerializer();
                serializationInfo = new BsonSerializationInfo(member.ElementName, member.GetSerializer(), serializer.ValueType);
            }
            return serializationInfo != null;
        }
        #endregion
    }
    #endregion
}
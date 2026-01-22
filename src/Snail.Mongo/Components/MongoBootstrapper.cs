using MongoDB.Bson.Serialization.Serializers;
using Snail.Abstractions;
using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Dependency.Extensions;

namespace Snail.Mongo.Components;
/// <summary>
/// 应用下使用Mongo数据库时的引导程序
/// <para>1、进行自定义类型推导器注册</para>
/// </summary>
[Component<IBootstrapper>]
internal class MongoBootstrapper : IBootstrapper
{
    #region 属性变量
    /// <summary>
    /// 类型推断器
    /// </summary>
    private readonly ITypeInferrer[]? _inferrers;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app"></param>
    public MongoBootstrapper(IApplication app)
    {
        ThrowIfNull(app);
        _inferrers = app.ResolveInRoot<IEnumerable<ITypeInferrer>>()?.ToArray();
    }
    #endregion

    #region IBootstrapper
    /// <summary>
    /// 执行引导
    /// <para>1、执行时机：在<see cref="IApplication.OnRegistered"/>事件后执行此方法</para>
    /// </summary>
    void IBootstrapper.Bootstrap()
    {
        if (IsNullOrEmpty(_inferrers) == true)
        {
            return;
        }
        foreach (ITypeInferrer inferrer in _inferrers)
        {
            Type[] types = inferrer.SupportTypes;
            if (IsNullOrEmpty(types) == true)
            {
                continue;
            }
            foreach (Type type in types)
            {
                CustomBsonSerializer serializer = new CustomBsonSerializer(type, inferrer);
                BsonSerializer.RegisterSerializer(type, serializer);
            }
        }
    }
    #endregion

    #region 内部类型
    /// <summary>
    /// 自定义Bson序列化器
    /// </summary>
    private sealed class CustomBsonSerializer : IBsonSerializer
    {
        #region 属性变量
        /// <summary>
        /// 推断器
        /// </summary>
        private readonly ITypeInferrer _inferrer;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="inferrer"></param>
        public CustomBsonSerializer(Type type, ITypeInferrer inferrer)
        {
            ValueType = type;
            _inferrer = inferrer;
        }
        #endregion

        #region IBsonSerializer
        /// <summary>
        /// 序列化类型
        /// </summary>
        public Type ValueType { private init; get; }
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="context"></param>
        /// <param name="args"></param>
        /// <param name="value"></param>
        void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            //  后期可考虑针对type做一些合法性验证；是不是内置支持的type
            if (value != null)
            {
                BsonSerializer.Serialize(context.Writer, value.GetType(), value);
            }
        }
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="context"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        object? IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            BsonDocument doc = BsonDocumentSerializer.Instance.Deserialize(context);
            /* doc.GetValue取到的值，进行toString，会转成具体的数值字符串，如11转成“11”*/
            Type? type = doc == null
                ? null
                : _inferrer.InferType(doc.Contains, doc.GetValue);
            return type == null ? default! : BsonSerializer.Deserialize(doc, type);
        }
        #endregion
    }
    #endregion
}

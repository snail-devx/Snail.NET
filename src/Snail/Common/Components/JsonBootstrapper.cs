using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Snail.Abstractions.Common.Interfaces;

namespace Snail.Common.Components;
/// <summary>
/// 应用下使用Json数据时的引导程序
/// <para>1、进行自定义类型推导器注册</para>
/// </summary>
[Component<IBootstrapper>]
public class JsonBootstrapper : IBootstrapper
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
    public JsonBootstrapper(IApplication app)
    {
        ThrowIfNull(app);
        _inferrers = app.ResolveInRoot<IEnumerable<ITypeInferrer>>()?.ToArray();
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 配置自定义的JSON转换器
    /// </summary>
    /// <param name="settings">已有的JSON序列化器配置</param>
    public void UseCustomJsonConverter(JsonSerializerSettings settings)
    {
        if (IsNullOrEmpty(_inferrers) == true)
        {
            return;
        }
        //  为每个类型构建一个自定义转换器
        List<JsonConverter> converters = [];
        foreach (ITypeInferrer inferrer in _inferrers)
        {
            Type[] types = inferrer.SupportTypes;
            if (IsNullOrEmpty(types) == true)
            {
                continue;
            }
            foreach (Type type in types)
            {
                CustomJsonConverter serializer = new CustomJsonConverter(type, inferrer);
                converters.Add(serializer);
            }
        }
        //  将自定义序列化器，加到最前面
        if (converters.Count > 0)
        {
            if (IsNullOrEmpty(settings.Converters) == false)
            {
                converters.AddRange(settings.Converters);
            }
            settings.Converters = converters;
        }
    }
    #endregion

    #region IBootstrapper
    /// <summary>
    /// 执行引导
    /// </summary>
    void IBootstrapper.Bootstrap()
    {
        //  由推断器时，才操作JsonConvert.DefaultSettings；否则忽略
        if (IsNullOrEmpty(_inferrers) == false)
        {
            Func<JsonSerializerSettings>? origin = JsonConvert.DefaultSettings;
            JsonConvert.DefaultSettings = () =>
            {
                //  取默认配置，然后强制插入ILMDataFilter序列化器
                JsonSerializerSettings settings = origin?.Invoke() ?? new JsonSerializerSettings();
                UseCustomJsonConverter(settings);
                return settings;
            };
        }
    }
    #endregion

    #region 内部类型
    /// <summary>
    /// 自定义Json转换器；进行类型推导
    /// </summary>
    private sealed class CustomJsonConverter : JsonConverter
    {
        #region 属性变量
        /// <summary>
        /// 自定义转换器的类型
        /// </summary>
        private readonly Type _type;
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
        public CustomJsonConverter(Type type, ITypeInferrer inferrer)
        {
            _type = type;
            _inferrer = inferrer;
        }
        #endregion

        #region JsonConverter
        /// <summary>
        /// 是否能够转换此类型
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType) => _type == objectType;
        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            /// 序列化时走不到此Converter中；都是走实际类型的序列化
            throw new ApplicationException($"Json序列化不应该能走到此序列化方法中，请排查。converter:{GetType().FullName}；value:{value?.GetType()?.FullName}");
        }
        /// <summary>
        /// 反序列化对象
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            //  若_inferrers则不会注册自定义JSONConverter，也就不会执行此方法，执行到这里则默认_inferrers有值
            if (reader.TokenType == JsonToken.StartObject)
            {
                JObject jObj = JObject.Load(reader);
                Type? targetType = _inferrer.InferType(jObj.ContainsKey, jObj.GetValue);
                return targetType == null ? null : jObj.ToObject(targetType);
            }
            return null;
        }
        #endregion
    }
    #endregion
}

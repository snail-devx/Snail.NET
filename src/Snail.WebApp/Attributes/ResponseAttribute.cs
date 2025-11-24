using Newtonsoft.Json.Serialization;
using Snail.WebApp.Enumerations;
using Snail.WebApp.Interfaces;

namespace Snail.WebApp.Attributes;

/// <summary>
/// 特性标签：API响应结果处理
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ResponseAttribute : Attribute, IActionAttribute
{
    /// <summary>
    /// 是否禁用响应结果处理
    /// <para>1、为false时，拦截api响应结果，进行json序列化等操作</para>
    /// <para>2、为true时，忽略响应结果处理，保持原样返回</para>
    /// <para>响应发生异常时，忽略响应结果处理</para>
    /// </summary>
    public bool Disabled { init; get; }

    /// <summary>
    /// JSON序列化解析类型
    /// <para>1、如将返回结果进行驼峰序列化</para>
    /// <para>2、若返回结果不是json类型不会进行格式化</para>
    /// <para>3、若传入<see cref="JsonResolverType.Custom"/>，则确保<see cref="JsonCustomResolver"/>能够依赖注入，否则无效</para>
    /// </summary>
    public JsonResolverType JsonResolver { init; get; }
    /// <summary>
    /// JSON序列化时，忽略Null值
    /// <para>1、为true时，如{key:null,value:1}序列化后则为{value:1}</para>
    /// <para>2、为false时，保持原样输出</para>
    /// </summary>
    public bool JsonIgnoreNullValue { init; get; }
    /// <summary>
    /// JSON序列化自定义解析
    /// <para>1、传入<see cref="IContractResolver"/>的依赖注入Key值</para>
    /// <para>2、<see cref="JsonResolverType.Custom"/>时生效</para>
    /// </summary>
    public string? JsonCustomResolver { init; get; }
}

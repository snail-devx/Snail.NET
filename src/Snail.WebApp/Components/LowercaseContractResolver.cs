using Newtonsoft.Json.Serialization;

namespace Snail.WebApp.Components;

/// <summary>
/// JSON序列化时，将属性名全小写
/// </summary>
public sealed class LowercaseContractResolver : DefaultContractResolver
{
    /// <summary>
    /// 整理属性名
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    protected override string ResolvePropertyName(string propertyName)
    {
        return propertyName.ToLower();
    }
}
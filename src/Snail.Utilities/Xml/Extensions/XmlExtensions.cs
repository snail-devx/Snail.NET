using System.Xml;

namespace Snail.Utilities.Xml.Extensions;
/// <summary>
/// Xml相关扩展方法
/// </summary>
public static class XmlExtensions
{
    #region XmlNode：XmlDocument、XmlAttribute等的基类
    /// <summary>
    /// 获取节点属性值；属性值为null返回String.Empty
    /// </summary>
    /// <param name="node"></param>
    /// <param name="attrName">要获取的属性名称</param>
    /// <returns></returns>
    public static string GetAttribute(this XmlNode node, in string attrName)
    {
        XmlAttribute? attr = node.Attributes?[attrName];
        return attr?.Value?.Length > 0
            ? attr.Value
            : string.Empty;
    }
    #endregion

    #region XmlAttributeCollection
    /// <summary>
    /// 属性集合转成字典对象；属性值会自动去前后空格
    /// </summary>
    /// <param name="attrs"></param>
    /// <returns>属性字典；强制非null</returns>
    public static Dictionary<string, string> ToDictionary(this XmlAttributeCollection attrs)
    {
        Dictionary<string, string> dict = new();
        for (int index = 0; index < attrs.Count; index++)
        {
            XmlAttribute attr = attrs[index];
            dict[attr.Name] = attr.Value;
        }
        return dict;
    }
    #endregion

    #region XmlNodeList
    /// <summary>
    /// 是否存在符合条件的数据
    /// </summary>
    /// <param name="nodes"></param>
    /// <returns></returns>
    public static bool Any(this XmlNodeList nodes)
        => nodes.Count > 0;
    /// <summary>
    /// 是否存在符合条件的数据
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="predicate">断言条件</param>
    /// <returns></returns>
    public static bool Any(this XmlNodeList nodes, Predicate<XmlNode> predicate)
    {
        ThrowIfNull(predicate);
        //  根据断言条件，匹配补上返回false
        for (int index = 0; index < nodes.Count; index++)
        {
            if (predicate.Invoke(nodes[index]!) == true)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 将XmlNodeList转成新的可枚举对象
    /// </summary>
    /// <typeparam name="T">集合数据对象</typeparam>
    /// <param name="nodes">节点集合</param>
    /// <param name="func">新的对象构建委托</param>
    /// <returns></returns>
    public static IEnumerable<T> Select<T>(this XmlNodeList nodes, Func<XmlNode, T> func)
    {
        ThrowIfNull(func);
        for (int index = 0; index < nodes.Count; index++)
        {
            yield return func.Invoke(nodes[index]!);
        }
    }

    /// <summary>
    /// 遍历xml节点
    /// </summary>
    /// <param name="nodes">要遍历的xml节点集合</param>
    /// <param name="action">xml节点遍历回调；为null的节点，不会调用此回调</param>
    public static void ForEach(this XmlNodeList nodes, in Action<XmlNode> action)
    {
        ThrowIfNull(action);
        for (int index = 0; index < nodes.Count; index++)
        {
            action.Invoke(nodes[index]!);
        }
    }

    /// <summary>
    /// 将XmlNodeList转成字典对象
    /// </summary>
    /// <typeparam name="TKey">字典Key类型</typeparam>
    /// <typeparam name="TValue">字典Value类型</typeparam>
    /// <param name="nodes">节点集合</param>
    /// <param name="keyFunc">key值获取委托</param>
    /// <param name="valueFunc">value值获取委托</param>
    /// <returns>字典对象</returns>
    /// <remarks>具有局限性，不开放给外部使用</remarks>
    public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this XmlNodeList nodes, Func<XmlNode, TKey> keyFunc, Func<XmlNode, TValue> valueFunc)
        where TKey : notnull
    {
        ThrowIfNull(keyFunc);
        ThrowIfNull(valueFunc);
        Dictionary<TKey, TValue> dict = new();
        for (int index = 0; index < nodes.Count; index++)
        {
            XmlNode node = nodes[index]!;
            dict[keyFunc(node)] = valueFunc(node);

        }
        return dict;
    }
    /// <summary>
    /// 将XmlNodeList转成String的字典对象 
    /// <para>1、xmlnode节点的key属性作为字典key；value属性作为字典value值 </para>
    /// <para>2、确保xml节点的key、value属性存在，且值不为空，为空会报错 </para>
    /// </summary>
    /// <param name="nodes">xml节点</param>
    /// <param name="title">操作失败时的提示语</param>
    /// <returns></returns>
    /// <remarks>具有局限性，不开放给外部使用</remarks>
    public static IDictionary<string, string> ToDictionary(this XmlNodeList nodes, string title)
    {
        Dictionary<string, string> dict = new();
        for (int index = 0; index < nodes.Count; index++)
        {
            XmlNode node = nodes[index]!;
            string key = node.GetAttribute("key")
               ?? throw new ApplicationException($"xml节点key属性值无效，无法分析{title}：{node.OuterXml}");
            //  value属性不存在或者值为空，做强制Empty处理
            dict[key] = node.GetAttribute("value") ?? string.Empty;
        }
        return dict;
    }
    #endregion
}

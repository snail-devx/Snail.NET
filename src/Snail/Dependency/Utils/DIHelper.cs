using Snail.Abstractions.Dependency.DataModels;
using Snail.Abstractions.Dependency.Interfaces;
using Snail.Utilities.Common.Utils;
using Snail.Utilities.Xml.Extensions;
using Snail.Utilities.Xml.Utils;
using System.Xml;

namespace Snail.Dependency.Utils;

/// <summary>
/// 依赖注入助手类，做一下公共逻辑抽取
/// </summary>
public static class DIHelper
{
    #region 公共方法

    #region 依赖注入信息管理
    /// <summary>
    /// 从XML文件构建【依赖注入】信息
    /// <para>1、读取xml的[/configuration/container/register] 节点作为依赖注册信息描述 </para>
    /// <para>2、从register、container分析name属性，拼接为Key值；若传入了<paramref name="keyPrefix"/>，一并拼接，连接符为<see cref="STR_Separator"/> </para>
    /// <para>3、从register的lifetime、from、to分析依赖注入的其他信息，其中from、to可以在[/configuration/aliases/add]下配置别名，方便重用 </para>
    /// <para>4、xml配置[/configuration/aliases/add]示例：&lt;add key="别名" value="Type.FullName值" /&gt; </para>
    /// </summary>
    /// <param name="file">xml文件全路径</param>
    /// <param name="keyPrefix">依赖注入信息的<see cref="DIDescriptor.Key"/>前缀；无则不传入</param>
    /// <param name="defaultLifetime">默认的生命周期类型</param>
    /// <remarks>不太建议通过配置文件注册【依赖注入】信息；强烈推荐使用实现<see cref="IComponent"/>接口的标签和<see cref="AppScanAttribute"/>配合使用，完成自动type扫描注入</remarks>
    /// <returns>分析出来的依赖注入信息集合</returns>
    public static IList<DIDescriptor>? BuildFromXmlFile(in string file, in string? keyPrefix, in LifetimeType defaultLifetime)
    {
        XmlDocument doc = XmlHelper.Load(file);
        if (doc == null) return null;
        //  分析注册的container下的Register值；且不能重复
        XmlNodeList? containers = doc.SelectNodes("/configuration/container");
        if (containers?.Any() != true) return null;
        //  分析类型映射：验重
        IDictionary<string, Type> aliases = new Dictionary<string, Type>();
        doc.SelectNodes("/configuration/aliases/add")?.ForEach(node =>
        {
            string key = node.GetAttribute("key"),
                   type = node.GetAttribute("value");
            ThrowIfNullOrEmpty(key);
            aliases.Add(key, TypeHelper.LoadType(type));
        });
        //  遍历container节点分析注册信息
        List<DIDescriptor> descriptors = new List<DIDescriptor>();
        foreach (XmlNode cNode in containers)
        {
            XmlNodeList? registers = cNode.SelectNodes("register");
            if (registers?.Any() != true) continue;
            //  遍历Register做注入
            string? container = Default(cNode.GetAttribute("name"), null);
            foreach (XmlNode rNode in registers)
            {
                string? register = Default(rNode.GetAttribute("name"), null),
                       lifetime = Default(rNode.GetAttribute("lifetime"), null),
                       fromTypeName = Default(rNode.GetAttribute("from"), null),
                       toTypeName = Default(rNode.GetAttribute("to"), null);
                if (fromTypeName == null)
                {
                    string msg = $"register节点from属性为空。{rNode.OuterXml}";
                    throw new ApplicationException(msg);
                }
                //  from、to类型映射
                aliases.TryGetValue(fromTypeName!, out Type? from);
                from ??= TypeHelper.LoadType(fromTypeName);
                Type? to = null;
                if (toTypeName != null)
                {
                    aliases.TryGetValue(toTypeName, out to);
                    to ??= TypeHelper.LoadType(toTypeName);
                }
                to ??= from;
                //  构建依赖注入；key做拼接，null转成"null"
                string? key = keyPrefix?.Length > 0
                    ? string.Join(STR_Separator, keyPrefix, container, register)
                    : string.Join(STR_Separator, container, register);
                LifetimeType ltType = lifetime == null ? defaultLifetime : lifetime.AsEnum<LifetimeType>();
                descriptors.Add(new(key, from, lifetime: ltType, to));
            }
        }
        return descriptors;
    }
    #endregion

    #endregion
}

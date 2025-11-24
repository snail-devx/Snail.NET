using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.Extensions;
using Snail.Aspect.Distribution.Enumerations;
using static Snail.Aspect.Common.Utils.SyntaxMiddlewareHelper;

namespace Snail.Aspect.Distribution.DataModels
{
    /// <summary>
    /// [CacheMethod]配置选项；从语法节点分析出具体的属性信息
    /// </summary>
    internal readonly struct CacheMethodOptions
    {
        #region 属性变量
        /// <summary>
        /// 是否是有效的配置选项
        /// </summary>
        public bool IsValid { get; }
        /// <summary>
        /// 缓存方法标签
        /// </summary>
        public AttributeSyntax Attribute { get; }

        /// <summary>
        /// 缓存类型：是对象缓存，还是哈希缓存
        /// </summary>
        public CacheType Type { get; }

        /// <summary>
        /// 缓存操作类型：<br />
        ///     1、<see cref="CacheActionType.Load"/>：传入缓存key，取到了则直接返回；取不到再执行方法代码；若传入多key，则自动过滤取到的，最后再合并<br />
        ///     1、<see cref="CacheActionType.LoadSave"/>：传入缓存key，取到了则直接返回；取不到再执行方法代码并把取到数据再加入缓存；若传入多key，则自动过滤取到的，最后再合并<br />
        ///     2、<see cref="CacheActionType.Save"/>：方法返回数据，自动加入缓存中<br />
        ///     3、<see cref="CacheActionType.Delete"/>：传入的缓存Key，执行完方法后自动删除<br />
        /// </summary>
        public CacheActionType Action { get; }

        /// <summary>
        /// 缓存主Key：；参照<see cref="Attributes.CacheMethodBase.MasterKey"/>
        /// </summary>
        public AttributeArgumentSyntax MasterKey { get; }
        /// <summary>
        /// 缓存数据Key前缀；参照<see cref="Attributes.CacheMethodBase.DataKeyPrefix"/>
        /// </summary>
        public AttributeArgumentSyntax DataKeyPrefix { get; }

        /// <summary>
        /// 缓存数据类型名称；<br />
        ///     1、必传，基于此分析缓存数据类型<br />
        ///     2、最初想基于方法返回值分析，这样限制太多，且分析得不一定准确<br />
        ///     3、抛出错误信息时，基于此节点做定位使用；支持属性值和泛型参数值
        /// </summary>
        public SyntaxNode DataType { get; }
        /// <summary>
        /// 缓存数据类型
        /// </summary>
        public ITypeSymbol DataTypeSymbol { get; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="context"></param>
        public CacheMethodOptions(AttributeSyntax attr, SourceGenerateContext context) : this(attr, context, null, null)
        {
        }
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="context"></param>
        /// <param name="dataTypeNode">缓存数据类型节点；为null时则基于<paramref name="attr"/>分析；和<paramref name="dataTypeSymbol"/>同时传入时生效</param>
        /// <param name="dataTypeSymbol">缓存数据类型；为null时则基于<paramref name="attr"/>分析；和<paramref name="dataTypeNode"/>同时传入时生效</param>
        public CacheMethodOptions(AttributeSyntax attr, SourceGenerateContext context, SyntaxNode dataTypeNode, ITypeSymbol dataTypeSymbol)
        {
            //  默认值处理
            {
                IsValid = true;
                Attribute = attr;
                Type = CacheType.ObjectCache;
                Action = CacheActionType.Load;
                MasterKey = null;
                DataKeyPrefix = null;
                DataType = null;
                DataTypeSymbol = null;
            }
            //  外部同时传入dataTypeNode、dataTypeSymbol时，生效
            if (dataTypeNode != null && dataTypeSymbol != null)
            {
                DataType = dataTypeNode;
                DataTypeSymbol = dataTypeSymbol;
            }
            //  遍历属性参数：分析具体指
            foreach (var ag in attr.GetArguments())
            {
                switch (ag.NameEquals?.Name?.Identifier.ValueText)
                {
                    case "Type":
                        Type = GetEnumByFullEnumValuePath<CacheType>($"{context.Semantic.GetSymbolInfo(ag.Expression).Symbol}");
                        break;
                    case "Action":
                        Action = GetEnumByFullEnumValuePath<CacheActionType>($"{context.Semantic.GetSymbolInfo(ag.Expression).Symbol}");
                        break;
                    //  缓存主Key；若传入了则强制非Null
                    case "MasterKey":
                        MasterKey = Type == CacheType.ObjectCache ? null : ag;
                        break;
                    //  数据Key前缀，为空则强制null
                    case "DataKeyPrefix":
                        DataKeyPrefix = SyntaxExtensions.IsNullOrEmpty(ag) ? null : ag;
                        break;
                    //  分析缓存数据类型：并将类型加入命名空间
                    case "DataType":
                        //  外部传入了数据类型，则不用分析了，正常也不会进入此逻辑
                        if (dataTypeSymbol == null)
                        {
                            if (ag.Expression is TypeOfExpressionSyntax typeOfNode == true)
                            {
                                DataType = ag;
                                DataTypeSymbol = context.Semantic.GetTypeInfo(typeOfNode.Type).Type;
                            }
                            else
                            {
                                context.ReportError(
                                    message: "[CacheMethod]的DataType值必须为 typeof(类型) ",
                                    syntax: ag
                                );
                                IsValid = false;
                            }
                        }
                        break;
                    //  其他的Key值，不支持，忽略掉
                    default: break;
                }
            }
            //  验证处理：对属性参数值做一些合法性验证
            if (Type == CacheType.HashCache && SyntaxExtensions.IsNullOrEmpty(MasterKey))
            {
                context.ReportError("[CacheMethod]参数 Type=HashCache 时，MasterKey值必传", attr);
                IsValid = false;
            }
            //      数据类型判断：必传，则必须是class
            if (DataType == null)
            {
                context.ReportError("[CacheMethod]未传入 DataType 值", attr);
                IsValid = false;
            }
            else
            {
                context.AddNamespaces($"{DataTypeSymbol.ContainingNamespace}");
                if (DataTypeSymbol.TypeKind != TypeKind.Class)
                {
                    context.ReportError("[CacheMethod]的DataType值只能是class", DataType);
                    IsValid = false;
                }
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 结构选项；将如下属性解构成字符串，方便做代码生成 <br/>
        ///     1、<see cref="Attributes.CacheMethodBase.MasterKey"/> <br/>
        ///     2、<see cref="Attributes.CacheMethodBase.DataKeyPrefix"/> <br/>
        /// </summary>
        /// <param name="masterKey"></param>
        /// <param name="dataKeyPrefix"></param>
        public void DeconstructKey(out string masterKey, out string dataKeyPrefix)
        {
            masterKey = MasterKey == null ? "null" : $"{MasterKey.Expression}";
            dataKeyPrefix = DataKeyPrefix == null ? "null" : $"{DataKeyPrefix.Expression}";
        }
        /// <summary>
        /// 结构选项；将如下属性结构成字符串，方便做代码生成 <br/>
        ///     1、<see cref="Attributes.CacheMethodBase.Type"/> <br/>
        ///     2、<see cref="Attributes.CacheMethodAttribute.DataType"/> <br/>
        /// </summary>
        /// <param name="cacheType">缓存类型字符串：如CacheType.Object</param>
        /// <param name="dataType">缓存数据类型：如string、TestCache</param>
        public void DeconstructType(out string cacheType, out string dataType)
        {
            dataType = $"{DataTypeSymbol.Name}";
            cacheType = $"{nameof(CacheType)}.{Type}";
        }
        #endregion
    }
}

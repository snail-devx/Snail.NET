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
        ///     1、<see cref="CacheActionType.Load"/>：传入缓存key，取到了则直接返回；取不到再执行方法代码并把取到数据再加入缓存；若传入多key，则自动过滤取到的，最后再合并<br />
        ///     2、<see cref="CacheActionType.Save"/>：方法返回数据，自动加入缓存中<br />
        ///     3、<see cref="CacheActionType.Delete"/>：传入的缓存Key，执行完方法后自动删除<br />
        /// </summary>
        public CacheActionType Action { get; }

        /// <summary>
        /// 缓存主Key：根据<see cref="Type"/>取值，此值意义不一样<br />
        ///     1、在<see cref="CacheType.ObjectCache"/>缓存时，目前忽略<br />
        ///     2、在<see cref="CacheType.HashCache"/>缓存时，为Hash缓存key<br />
        /// </summary>
        public AttributeArgumentSyntax MasterKey { get; }

        /// <summary>
        /// 缓存数据类型名称；<br />
        ///     1、必传，基于此分析缓存数据类型<br />
        ///     2、最初想基于方法返回值分析，这样限制太多，且分析得不一定准确<br />
        /// </summary>
        public AttributeArgumentSyntax DataType { get; }
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
        public CacheMethodOptions(AttributeSyntax attr, SourceGenerateContext context)
        {
            //  默认值处理
            {
                IsValid = true;
                Attribute = attr;
                Type = CacheType.ObjectCache;
                Action = CacheActionType.Load;
                MasterKey = null;
                DataType = null;
                DataTypeSymbol = null;
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
                        MasterKey = ag;
                        break;
                    //  分析缓存数据类型：并将类型加入命名空间
                    case "DataType":
                        if (ag.Expression is TypeOfExpressionSyntax typeOfNode == true)
                        {
                            DataType = ag;
                            DataTypeSymbol = context.Semantic.GetTypeInfo(typeOfNode.Type).Type;
                            context.AddNamespaces($"{DataTypeSymbol.ContainingNamespace}");
                            break;
                        }
                        else
                        {
                            context.ReportError(
                                message: "[CacheMethod]的DataType值必须为 typeof(类型) ",
                                syntax: ag
                            );
                            IsValid = false;
                        }
                        break;
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
            else if (DataTypeSymbol.TypeKind != TypeKind.Class)
            {
                context.ReportError("[CacheMethod]的DataType值只能是class", DataType);
                IsValid = false;
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 解构成字符串，方便做代码生成
        /// </summary>
        /// <param name="cacheType">缓存类型字符串：如CacheType.Object</param>
        /// <param name="dataType">缓存数据类型：如string、TestCache</param>
        /// <param name="masterKey">缓存主Key</param>
        public void Deconstruct(out string cacheType, out string dataType, out string masterKey)
        {
            dataType = $"{DataTypeSymbol.Name}";
            masterKey = MasterKey == null ? "null" : $"{MasterKey.Expression}";
            cacheType = $"{nameof(CacheType)}.{Type}";
        }
        #endregion
    }
}

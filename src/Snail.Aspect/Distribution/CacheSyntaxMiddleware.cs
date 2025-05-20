using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.DataModels;
using Snail.Aspect.Common.Delegates;
using Snail.Aspect.Common.Extensions;
using Snail.Aspect.Common.Interfaces;
using Snail.Aspect.Distribution.Attributes;
using Snail.Aspect.Distribution.DataModels;
using Snail.Aspect.Distribution.Enumerations;
using Snail.Aspect.Distribution.Interfaces;
using Snail.Aspect.Distribution.Utils;
using static Snail.Aspect.Common.Utils.SyntaxMiddlewareHelper;

namespace Snail.Aspect.Distribution
{
    /// <summary>
    /// 【CacheAspect】语法节点源码中间件<br/>
    ///     1、侦测打了<see cref="CacheAspectAttribute"/>标签的class和interface节点，为其生成实现class，并注册为组件 <br />
    /// </summary>
    internal class CacheSyntaxMiddleware : ITypeDeclarationMiddleware
    {
        #region 属性变量
        /// <summary>
        /// 名称：本地方法名称
        /// </summary>
        protected const string NAME_LocalMethod = "_CacheNextCodeMethod";
        /// <summary>
        /// 代码：Cacher判空
        /// </summary>
        protected const string CODE_CacherJudgeNull = "ThrowIfNull(_cacher, \"_cacher为null，无法进行Cache操作\");";
        /// <summary>
        /// 变量名：<see cref=" CacheMethodAttribute.MasterKey"/>
        /// </summary>
        protected const string VAR_MasterKey = "aspectMasterKey";
        /// <summary>
        /// 变量名：<see cref="CacheMethodAttribute.DataKeyPrefix"/>  
        /// </summary>
        protected const string VAR_DataKeyPrefix = "aspectDataKeyPrefix";

        /// <summary>
        /// 类型名：<see cref="CacheAspectAttribute"/>
        /// </summary>
        protected static readonly string TYPENAME_CacheAspectAttribute = typeof(CacheAspectAttribute).FullName;
        /// <summary>
        /// 类型名：<see cref="CacheMethodAttribute"/>
        /// </summary>
        protected static readonly string TYPENAME_CacheMethodAttribute = typeof(CacheMethodAttribute).FullName;
        /// <summary>
        /// 类型名：<see cref="CacheKeyAttribute"/>
        /// </summary>
        protected static readonly string TYPENAME_CacheKeyAttribute = typeof(CacheKeyAttribute).FullName;
        /// <summary>
        /// 类型名：IIdentity
        /// </summary>
        protected const string TYPENAME_IIdentity = "Snail.Abstractions.Identity.Interfaces.IIdentity";

        /// <summary>
        /// 固定需要引入的命名空间集合
        /// </summary>
        protected static readonly IReadOnlyList<string> FixedNamespaces = new List<string>()
        {
            //  全局依赖的
            typeof(Task).Namespace,//                           System
            "Snail.Utilities.Common.Utils",//                   typeof(ObjectHelper).Namespace,           
            "Snail.Utilities.Collections.Utils",//              typeof(ListHelper).Namespace,//
            "Snail.Utilities.Common.Extensions",
            "Snail.Utilities.Collections.Extensions",
            //       这几个为了方便内部判断，如IsNullOrEmpty
            "static Snail.Utilities.Common.Utils.ArrayHelper",
            "static Snail.Utilities.Common.Utils.ObjectHelper",
            "static Snail.Utilities.Common.Utils.StringHelper",
            "static Snail.Utilities.Collections.Utils.DictionaryHelper",
            "static Snail.Utilities.Collections.Utils.ListHelper",
            //  数据包相关和缓存转换相关
            "Snail.Abstractions.Common.DataModels",
            "Snail.Abstractions.Common.Interfaces",
            "Snail.Abstractions.Identity.Interfaces",
            //  依赖注入相关：将生成的class注册为Interface实现组件
            "Snail.Abstractions.Dependency.Attributes",//       typeof(InjectAttribute).Namespace,//                
            "Snail.Abstractions.Dependency.Enumerations",//     typeof(LifetimeType).Namespace,//                   
            //  缓存处理实现时所需接口
            "Snail.Abstractions.Distribution",//                typeof(ICacher).Namespace,//                        
            "Snail.Abstractions.Distribution.Attributes",//     typeof(CacherAttribute).Namespace,//                
            "Snail.Abstractions.Distribution.Extensions",//     typeof(CacherExtensions).Namespace,//       
            "Snail.Abstractions.Identity.Extensions",        
            //  缓存 切面编程相关命名空间
            typeof(CacheAspectAttribute).Namespace,
            typeof(CacheActionType).Namespace,
            typeof(ICacheAnalyzer).Namespace,
            $"static {typeof(CacheAspectHelper).FullName}",
        };

        /// <summary>
        /// [CacheAspect]特性标签
        /// </summary>
        protected readonly AttributeSyntax ANode;
        /// <summary>
        /// 缓存分析器参数：<see cref="ICacheAnalyzer"/>分析缓存相关Key
        /// </summary>
        protected readonly AttributeArgumentSyntax AnalyzerArg;
        /// <summary>
        /// 是否需要【辅助】代码
        /// </summary>
        private bool _needAssistantCode = false;
        #endregion

        #region 构造方法
        /// <summary>
        /// 私有构造方法
        /// </summary>
        /// <param name="cacheAttr"></param>
        private CacheSyntaxMiddleware(AttributeSyntax cacheAttr)
        {
            ANode = cacheAttr;
            cacheAttr.HasAnalyzer(out AnalyzerArg);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 构建中间件
        /// </summary>
        /// <param name="node"></param>
        /// <param name="semantic"></param>
        /// <returns></returns>
        public static ITypeDeclarationMiddleware Build(TypeDeclarationSyntax node, SemanticModel semantic)
        {
            //  仅针对“有【CacheAspectAttribute】属性标记的interface和class”做处理
            if (node is InterfaceDeclarationSyntax || node is ClassDeclarationSyntax)
            {
                AttributeSyntax attr = node.AttributeLists.GetAttribute(semantic, TYPENAME_CacheAspectAttribute);
                return attr != null
                    ? new CacheSyntaxMiddleware(attr)
                    : null;
            }
            return null;
        }
        #endregion

        #region ITypeDeclarationMiddleware
        /// <summary>
        /// 准备生成：做一下信息初始化，或者将将一些信息加入上下文
        /// </summary>
        /// <param name="context"></param>
        void ITypeDeclarationMiddleware.PrepareGenerate(SourceGenerateContext context)
        {
            context.ReportErrorIf
            (
                condition: context.TypeSyntax.TypeParameterList?.Parameters.Count > 0,
                message: $"[CacheAspect]暂不支持在泛型class/interface中使用",
                syntax: context.TypeSyntax.TypeParameterList?.Parameters.First()
            );
        }

        /// <summary>
        /// 生成方法代码；仅包括方法内部代码
        /// </summary>
        /// <param name="method">方法语法节点</param>
        /// <param name="context">上下文对象</param>
        /// <param name="options">方法生成配置选项</param>
        /// <param name="next">下一步操作；若为null则不用继续执行，返回即可</param>
        /// <remarks>若不符合自身业务逻辑</remarks>
        /// <returns>代码字符串</returns>
        string ITypeDeclarationMiddleware.GenerateMethodCode(MethodDeclarationSyntax method, SourceGenerateContext context, MethodGenerateOptions options, MethodCodeDelegate next)
        {
            //  1、无缓存属性标记，直接执行下一步逻辑
            AttributeSyntax attr = method.AttributeLists.GetAttribute(context.Semantic, TYPENAME_CacheMethodAttribute);
            if (attr == null)
            {
                string nextCode = next?.Invoke(method, context, options);
                return nextCode;
            }
            //  2、实现前的基础验证：只要有标记属性标签，都算实现了；内部再判断是否能够进行实现
            context.Generated = true;
            {
                //  泛型参数方法处理起来太麻烦了，这里强制不支持
                if (method.TypeParameterList?.Parameters.Count > 0)
                {
                    context.ReportError("[CacheMethod]不支持泛型方法", method.TypeParameterList.Parameters.First());
                    return null;
                }
                //  方法必须是异步的：强制规则，推进异步编程
                if (options.IsAsync != true)
                {
                    context.ReportError(message: "[CacheMethod]标记方法必须为异步：返回值为Task/Task<T>", method.ReturnType);
                    return null;
                }
            }
            //  3、进行缓存请求相关代码实现：将next代码构建为本地方法
            _needAssistantCode = true;
            CacheMethodOptions cacheOptions = new CacheMethodOptions(attr, context);
            StringBuilder builder = new StringBuilder();
            //      辅助代码：masterKey和dataKeyPrefix处理
            cacheOptions.DeconstructKey(out string masterKey, out string dataKeyPrefix);
            if (cacheOptions.MasterKey != null)
            {
                if (AnalyzerArg != null)
                {
                    string ampMapName = context.GetMethodParameterMapName(method);
                    builder.Append(context.LinePrefix)
                           .Append($"string {VAR_MasterKey} = ")
                           .AppendLine($"_cacheAnalyzer.AnalysisMasterKey({masterKey}, {ampMapName});");
                }
                else
                {
                    builder.Append(context.LinePrefix).AppendLine($"string {VAR_MasterKey} = {masterKey};");
                }
            }
            if (cacheOptions.DataKeyPrefix != null)
            {
                if (AnalyzerArg != null)
                {
                    string ampMapName = context.GetMethodParameterMapName(method);
                    builder.Append(context.LinePrefix)
                           .Append($"string {VAR_DataKeyPrefix} = ")
                           .AppendLine($"_cacheAnalyzer.AnalysisDataKeyPrefix({dataKeyPrefix}, {ampMapName});");
                }
                else
                {
                    builder.Append(context.LinePrefix).AppendLine($"string {VAR_DataKeyPrefix} = {dataKeyPrefix};");
                }
            }
            //      业务代码
            string code = null;
            if (cacheOptions.IsValid == true)
            {
                switch (cacheOptions.Action)
                {
                    case CacheActionType.Load:
                        code = GenerateLoadCode(method, context, options, cacheOptions, next, false);
                        break;
                    case CacheActionType.LoadSave:
                        code = GenerateLoadCode(method, context, options, cacheOptions, next, true);
                        break;
                    case CacheActionType.Save:
                        code = GenerateSaveCode(method, context, options, cacheOptions, next);
                        break;
                    case CacheActionType.Delete:
                        code = GenerateDeleteCode(method, context, options, cacheOptions, next);
                        break;
                    //  不支持的值，先报错
                    default:
                        context.ReportError($"[CacheMethod]传入了不支持的 Action 值：{cacheOptions.Action}", method);
                        return null;
                }
            }
            //  返回生成代码：标记此插件已生成
            context.AddGeneratedMiddleware("[CacheAspect]");
            if (string.IsNullOrEmpty(code) == false)
            {
                builder.AppendLine(code);
                code = builder.ToString();
            }
            return code;
        }

        /// <summary>
        /// 生成<see cref="ITypeDeclarationMiddleware.GenerateMethodCode"/>的辅助 <br />
        ///     1、多个方法用到的通用逻辑，抽取成辅助方法 
        ///     2、方法实现所需的依赖注入变量 <br />
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        string ITypeDeclarationMiddleware.GenerateAssistantCode(SourceGenerateContext context)
        {
            /** 生成的辅助代码 样例

            //  生成[CacheAspect]辅助代码;
            //      依赖注入属性
            [Cacher, Server(Workspace = "Test", Code = "Default")]
            private ICacher? _cacher { init; get; }
            [Inject(Key = "xxx")]
            private ICacheAnalyzer? _cacheAnalyzer { init; get; }

            */
            if (_needAssistantCode == false)
            {
                return null;
            }
            //  添加需要的命名空间
            context.AddNamespaces(FixedNamespaces);
            //  生成辅助代码
            StringBuilder builder = new StringBuilder();
            builder.Append(context.LinePrefix).AppendLine("//  生成[CacheAspect]辅助代码;");
            //      解析属性标签节点：生成Server的依赖依赖注入代码
            string serverInjectCode = BuildServerInjectCodeByAttribute(ANode, context);
            builder.Append(context.LinePrefix).AppendLine($"[Cacher, {serverInjectCode}]")
                   .Append(context.LinePrefix).AppendLine("private ICacher? _cacher { init; get; }");
            //      生成解析器依赖注入代码
            GenerateAnalyzerAssistantCode(builder, context, AnalyzerArg, nameof(ICacheAnalyzer), "_cacheAnalyzer");

            return builder.ToString();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 生成【加载缓存】的相关代码实现 <br />
        ///     1、先从缓存中基于<see cref="CacheKeyAttribute"/>取数据，能全部取到则直接返回 <br />
        ///     2、部分取到或者没取到时，则执行<see cref="NAME_LocalMethod"/>本地方法，分析其中的Data数据将其加入缓存中 <br />
        ///     3、合并缓存数据和<see cref="NAME_LocalMethod"/>数据返回 <br />
        /// </summary>
        /// <param name="method"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="cacheOptions"></param>
        /// <param name="next">是否有next中间件生成的代码，有则可以执行<see cref="NAME_LocalMethod"/>方法，得到next代码执行结果</param>
        /// <param name="needSaveCache">是否需要进行新数据的Save缓存操作</param>
        /// <returns></returns>
        private static string GenerateLoadCode(MethodDeclarationSyntax method, SourceGenerateContext context, MethodGenerateOptions options, CacheMethodOptions cacheOptions, MethodCodeDelegate next, bool needSaveCache)
        {
            //  1、准备工作：分析后面会用到的一些参数
            ReturnTypeOptions rtOptions; CacheKeyOptions keyOptions;
            {
                //  检测数据类型匹配：否则可能出现返回值和缓存数据类型不匹配，导致运行时报错了
                /*  onlyClass强制true；仅允许class类作为返回值，避免最终合并数据时“interface等无法进行为null初始化”*/
                rtOptions = CheckCacheReturnDataType(context, options, cacheOptions, onlyClass: true);
                //  分析方法参数，得到CacheKey值：并确认方法参数无out、int等参数
                if (CheckMethodParameters(method, context, mustCacheKey: true, out keyOptions) == false)
                {
                    return null;
                }
                //  返回值类型：必须得有返回值，否则无数据，保存啥
                if (options.ReturnType == null)
                {
                    string message = "[CacheMethod]标记方法执行【加载缓存】时必须有返回值：返回值为Task<T>，否则没有意义";
                    context.ReportError(message, method);
                    return null;
                }
            }
            //  2、生成缓存加载方法
            StringBuilder builder = new StringBuilder();
            string oldLinePrefix = context.LinePrefix;
            //      需确保key有值才执行
            {
                builder.Append(context.LinePrefix).AppendLine($"{options.ReturnType} cacheNextData = default;");
                builder.Append(context.LinePrefix).AppendLine($"if (IsNullOrEmpty({keyOptions.VarName}) == false)")
                       .Append(context.LinePrefix).AppendLine("{");
                context.LinePrefix = $"{context.LinePrefix}\t";
            }
            //      生成具体逻辑代码
            {
                string nextRunCode = GenerateRunCodeWithNext(method, context, options, next, NAME_LocalMethod);
                bool hasNextRunCode = string.IsNullOrEmpty(nextRunCode) == false;
                //      加载缓存；并将已有数据从key中剔除
                GenerateLoadCodeByCache(builder, context, cacheOptions, keyOptions, hasNextRunCode);
                //      执行Next中间件代码，并根据需要保存数据
                if (hasNextRunCode == true)
                {
                    builder.Append(context.LinePrefix).AppendLine($"if (IsNullOrEmpty({keyOptions.VarName}) == false)")
                           .Append(context.LinePrefix).AppendLine("{")
                           .Append(context.LinePrefix).Append("\t").AppendLine($"cacheNextData = {nextRunCode}");
                    if (needSaveCache == true)
                    {
                        nextRunCode = context.LinePrefix;
                        context.LinePrefix = $"{context.LinePrefix}\t";
                        GenerateSaveCodeWithNextData(builder, context, options, cacheOptions, rtOptions, needCacherJudge: false);
                        context.LinePrefix = nextRunCode;
                    }
                    builder.Append(context.LinePrefix).AppendLine("}");
                }
                //      合并缓存和next数据：cacheLoadData有值才做此操作
                builder.Append(context.LinePrefix).Append("if (")
                       .Append(keyOptions.IsMulti ? "IsNullOrEmpty(cacheLoadData) == false" : "cacheLoadData != null")
                       .AppendLine(")");
                builder.Append(context.LinePrefix).AppendLine("{");
                GenerateLoadCodeByMerge(builder, context, options, rtOptions, cacheOptions, keyOptions);
                builder.Append(context.LinePrefix).AppendLine("}");
            }
            //      结束生成；收尾
            {
                context.LinePrefix = oldLinePrefix;
                builder.Append(context.LinePrefix).AppendLine("}");
                builder.Append(context.LinePrefix).AppendLine($"return cacheNextData;");
            }

            return builder.ToString();
        }
        /// <summary>
        /// 生成【缓存加载】的加载缓存相关代码<br />
        ///     1、加载完缓存，从key中剔除已加载数据
        ///     2、配合<see cref="GenerateLoadCode"/>使用，提取其中逻辑抽取成方法，减少相主方法代码
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="context"></param>
        /// <param name="cacheOptions"></param>
        /// <param name="keyOptions"></param>
        /// <param name="hasNextRunCode">是否存在next运行代码，不存在时不进行key数据剔除操作</param>
        private static void GenerateLoadCodeByCache(StringBuilder builder, SourceGenerateContext context, CacheMethodOptions cacheOptions, CacheKeyOptions keyOptions, bool hasNextRunCode)
        {
            cacheOptions.DeconstructType(out string cacheType, out string dataType);
            string key = keyOptions.VarName;
            string cacheDataKey = null;
            if (cacheOptions.DataKeyPrefix != null)
            {
                cacheDataKey = context.GetVarName($"{key}ToCache");
                builder.Append(context.LinePrefix).AppendLine($"var {cacheDataKey} = CombineDataKey({key}, {VAR_DataKeyPrefix});");
            }
            //  从缓存取数据：如果有DataKeyPrefix，则需要先处理做拼接
            builder.Append(context.LinePrefix).AppendLine(CODE_CacherJudgeNull);
            switch (cacheOptions.Type)
            {
                case CacheType.ObjectCache:
                    builder.Append(context.LinePrefix).AppendLine($"var cacheLoadData = await _cacher.GetObject<{dataType}>({cacheDataKey ?? key});");
                    break;
                case CacheType.HashCache:
                    builder.Append(context.LinePrefix).AppendLine($"var cacheLoadData = await _cacher.GetHash<{dataType}>({VAR_MasterKey ?? "null"}, {cacheDataKey ?? key});");
                    break;
                default:
                    context.ReportError($"保存缓存时不支持[Type]值{cacheType}", cacheOptions.Attribute);
                    break;
            }
            //  合并数据，得到已有数据；对key做重置操作，剔除掉已有的
            if (hasNextRunCode == true)
            {
                if (keyOptions.IsArray == true)
                {
                    builder.Append(context.LinePrefix).AppendLine($"{key} = cacheLoadData?.Count > 0")
                           .Append(context.LinePrefix).Append('\t').AppendLine($"? {key}.Except(cacheLoadData.Select(cld => (cld as IIdentity).Id)).ToArray()")
                           .Append(context.LinePrefix).Append('\t').AppendLine($": {key};");
                }
                else if (keyOptions.IsList == true)
                {
                    builder.Append(context.LinePrefix).AppendLine($"cacheLoadData?.ForEach(cld => (cld as IIdentity).Id?.RemoveFrom({key}));");
                }
                else
                {
                    builder.Append(context.LinePrefix).AppendLine($"{key} = cacheLoadData != null ? null : {key};");
                }
            }
        }
        /// <summary>
        /// 生成【缓存加载】的数据合并相关代码<br />
        ///     1、执行nextCode，接收返回值；并作为新缓存保存<br />
        ///     2、将缓存数据和nextCode返回数据合并一起返回
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="rtOptions"></param>
        /// <param name="cacheOptions"></param>
        /// <param name="keyOptions"></param>
        private static void GenerateLoadCodeByMerge(StringBuilder builder, SourceGenerateContext context, MethodGenerateOptions options, ReturnTypeOptions rtOptions, CacheMethodOptions cacheOptions, CacheKeyOptions keyOptions)
        {
            string bagDataType = rtOptions.GetDataBagTypeName(), multiDataType = rtOptions.GetMultiTypeName();
            cacheOptions.DeconstructType(out _, out string dataType);
            string linePrefix = $"{context.LinePrefix}\t";
            //  合并数据：基于不同数据类型，做区分处理
            string mergeVarName = rtOptions.IsDataBag ? context.GetVarName("bagData") : "cacheNextData";
            //      单项数据：不用解包，直接基于cacheLoadData取具体数据
            if (rtOptions.IsMulti == false)
            {
                builder.Append(linePrefix)
                       .Append(rtOptions.IsDataBag ? "var " : string.Empty)
                       .Append(mergeVarName).Append(" = ")
                       .AppendLine(keyOptions.IsMulti ? "cacheLoadData.FirstOrDefault();" : "cacheLoadData;");
            }
            //      多项数据：数组：先对数据包，做解包操作
            else if (rtOptions.IsArray == true)
            {
                _ = rtOptions.IsDataBag == true
                    ? builder.Append(linePrefix).AppendLine($"var {mergeVarName} = (({bagDataType})cacheNextData)?.GetData();")
                    : null;
                builder.Append(linePrefix).Append(mergeVarName).Append(" = ");
                _ = keyOptions.IsMulti == false
                    ? builder.AppendLine($"cacheLoadData.AsList().TryAddRange({mergeVarName}).ToArray();")
                    : builder.AppendLine($"cacheLoadData.TryAddRange({mergeVarName}).ToArray();");
            }
            //      多项数据：列表、字典：先对数据包，做解包操作
            else
            {
                _ = rtOptions.IsDataBag == true
                    ? builder.Append(linePrefix).Append($"var {mergeVarName} = (({bagDataType})cacheNextData)?.GetData() ?? ")
                    : builder.Append(linePrefix).Append($"{mergeVarName} ??= ");
                builder.AppendLine($"new {multiDataType}();");
                if (rtOptions.IsList == true)
                {
                    builder.Append(linePrefix).AppendLine($"cacheLoadData.InsertTo({mergeVarName});");
                }
                else if (rtOptions.IsDictionary == true)
                {
                    builder.Append(linePrefix).AppendLine($"cacheLoadData.AddTo({mergeVarName});");
                }
                //      多项数据：其他情况，忽略不做处理
                else { }
            }

            //  DataBag数据执行SetData
            if (rtOptions.IsDataBag == true)
            {
                builder.Append(linePrefix).AppendLine($"cacheNextData ??= new {options.ReturnType}();");
                builder.Append(linePrefix).AppendLine($"(({bagDataType})cacheNextData).SetData({mergeVarName});");
            }
        }

        /// <summary>
        /// 生成【保存缓存】的相关实现代码 <br />
        ///     1、等<paramref name="next"/>执行完成后，得到返回值，将这些返回值加入缓存中 <br />
        ///     2、缓存数据类型，需能自动分析出自己的缓存Key值 <br />
        /// </summary>
        /// <param name="method"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="cacheOptions"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        private static string GenerateSaveCode(MethodDeclarationSyntax method, SourceGenerateContext context, MethodGenerateOptions options, CacheMethodOptions cacheOptions, MethodCodeDelegate next)
        {
            //  1、基础验证：确保执行下一步时，不用关心数据有效性等
            ReturnTypeOptions rtOptions;
            {
                //  检测数据类型匹配：否则可能出现返回值和缓存数据类型不匹配，导致运行时报错了
                /*  这里允许Interface等类型，不会像Load那样存在创建新对象的情况，适当放宽     onlyClass: false*/
                rtOptions = CheckCacheReturnDataType(context, options, cacheOptions, onlyClass: false);
                //  返回值类型：必须得有返回值，否则无数据，保存啥
                if (options.ReturnType == null)
                {
                    string message = "[CacheMethod]标记方法执行【保存缓存】操作时必须有返回值：返回值为Task<T>，否则保存啥数据";
                    context.ReportError(message, method);
                    return null;
                }
                //  不支持in、out参数等
                CheckMethodParameters(method, context, mustCacheKey: false, out _);
            }
            //  2、执行nextCode；无代码时，给出空实现
            StringBuilder builder = new StringBuilder();
            string nextRunCode = GenerateRunCodeWithNext(method, context, options, next, NAME_LocalMethod);
            if (string.IsNullOrEmpty(nextRunCode) == false)
            {
                //  接收nextRunCode值给cacheNextData，然后保存缓存，
                builder.Append(context.LinePrefix).AppendLine($"{options.ReturnType} cacheNextData = {nextRunCode}");
                GenerateSaveCodeWithNextData(builder, context, options, cacheOptions, rtOptions);
                builder.Append(context.LinePrefix).AppendLine("return cacheNextData;");
            }
            else
            {
                builder.Append(context.LinePrefix).AppendLine("//   执行保存缓存时，无NextCode代码，无法取到要保存的缓存数据，直接空实现");
                builder.Append(context.LinePrefix).AppendLine("await Task.Yield();");
                builder.Append(context.LinePrefix).AppendLine("return default;");
            }

            return builder.ToString();
        }
        /// <summary>
        /// 基于【NextCode】的返回值【cacheNextData】生成保存缓存的代码<br />
        ///     1、方法<see cref="GenerateSaveCode"/>和<see cref="GenerateLoadCode"/>复用<br />
        ///     2、代码逻辑：如果是DataBag类型数据，则解包cacheNextData数据得到cacheBagData；然后执行保存缓存逻辑<br />
        ///     3、生成代码不包含NextCode执行赋值cacheNextData逻辑；不包含返回值；外部自己确定什么时候返回
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="cacheOptions"></param>
        /// <param name="rtOptions"></param>
        /// <param name="needCacherJudge">是否需要进行_cacher判断</param>
        private static void GenerateSaveCodeWithNextData(StringBuilder builder, SourceGenerateContext context, MethodGenerateOptions options, CacheMethodOptions cacheOptions, ReturnTypeOptions rtOptions, bool needCacherJudge = true)
        {
            /*  生成代码参考：
                TestDataBag? cacheNextData = await base.SaveObject(key);
                var cacheBagData = cacheNextData?.GetData();
                if(IsNullOrEmpty(cacheBagData) == false)
                {
                    ThrowIfNull(_cacher, "_cacher为null，无法进行Cache操作");
                    await _cacher!.AddHash<TestCache>("12312", cacheBagData!);
                }
             */

            //  参数准备
            cacheOptions.DeconstructType(out string cacheType, out string dataType);
            //      若返回值为数据包，则需要转成实际保存对象：cacheBagData
            string saveDataVar = null;
            {
                if (rtOptions.IsDataBag == true)
                {
                    //  这里需要处理一下，得到DataBag做强转，转换成 IDataBag<>
                    string bagTypeName = rtOptions.GetDataBagTypeName();
                    builder.Append(context.LinePrefix).AppendLine($"var cacheBagData = (({bagTypeName})cacheNextData)?.GetData();");
                    saveDataVar = "cacheBagData";
                }
                saveDataVar = saveDataVar ?? "cacheNextData";
            }
            //  生成保存代码：若数据为空，则不用执行保存操作了
            builder.Append(context.LinePrefix).AppendLine(rtOptions.IsMulti
                        ? $"if (IsNullOrEmpty({saveDataVar}) == false)"
                        : $"if ({saveDataVar} != null)"
                    )
                   .Append(context.LinePrefix).AppendLine("{");
            //      若存在DataKeyPrefix，则将缓存转换成字段
            string dataCacheKeyMapVar = null;
            if (cacheOptions.DataKeyPrefix != null)
            {
                dataCacheKeyMapVar = context.GetVarName("cacheDataMap");
                builder.Append(context.LinePrefix).Append('\t')
                       .AppendLine($"var {dataCacheKeyMapVar} = BuildCacheMap<{dataType}>({saveDataVar}, cld => (cld as IIdentity).Id, {VAR_DataKeyPrefix});");
            }
            //      基于缓存Type，生成保存代码
            string runCode = null;
            switch (cacheOptions.Type)
            {
                case CacheType.ObjectCache:
                    runCode = $"await _cacher.AddObject<{dataType}>({dataCacheKeyMapVar ?? saveDataVar});";
                    break;
                case CacheType.HashCache:
                    runCode = $"await _cacher.AddHash<{dataType}>({VAR_MasterKey ?? "null"}, {dataCacheKeyMapVar ?? saveDataVar});";
                    break;
                //  还没支持的的缓存类型，先报错
                default:
                    context.ReportError($"保存缓存时不支持[Type]值{cacheType}", cacheOptions.Attribute);
                    break;
            }
            if (runCode != null)
            {
                builder.Append(context.LinePrefix).Append('\t').AppendLine(runCode);
            }
            //      收尾代码
            builder.Append(context.LinePrefix).AppendLine("}");
        }

        /// <summary>
        /// 生成【删除缓存】的相关实现代码 <br />
        ///     1、等<paramref name="next"/>执行完成后，执行缓存数据删除 <br />
        /// </summary>
        /// <param name="method"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="cacheOptions"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        private static string GenerateDeleteCode(MethodDeclarationSyntax method, SourceGenerateContext context, MethodGenerateOptions options, CacheMethodOptions cacheOptions, MethodCodeDelegate next)
        {
            //  1、准备工作：分析方法参数，得到缓存Key值；并确认方法参数无out、int等参数
            if (CheckMethodParameters(method, context, mustCacheKey: true, out CacheKeyOptions keyOptions) == false)
            {
                return null;
            }
            //  2、执行NextCode代码，获取返回数据，使用 cacheNextData 接收
            StringBuilder builder = new StringBuilder();
            string nextRunCode = GenerateRunCodeWithNext(method, context, options, next, NAME_LocalMethod);
            if (string.IsNullOrEmpty(nextRunCode) == false)
            {
                _ = options.ReturnType == null
                    ? builder.Append(context.LinePrefix).AppendLine($"{nextRunCode}")
                    : builder.Append(context.LinePrefix).AppendLine($"{options.ReturnType} cacheNextData = {nextRunCode}");
            }
            //  3、执行删除缓存操作：基于key的模式做处理
            {
                builder.Append(context.LinePrefix).AppendLine($"if (IsNullOrEmpty({keyOptions.VarName}) == false)")
                       .Append(context.LinePrefix).AppendLine("{")
                       .Append(context.LinePrefix).Append('\t').AppendLine(CODE_CacherJudgeNull);
                //  合并DataKeyPrefi
                string key = keyOptions.VarName;
                if (cacheOptions.DataKeyPrefix != null)
                {
                    string tmpKeyVar = context.GetVarName("keysToCache");
                    builder.Append(context.LinePrefix).Append('\t').AppendLine($"var {tmpKeyVar} = CombineDataKey({key}, {VAR_DataKeyPrefix});");
                    key = tmpKeyVar;
                }
                //  生成删除缓存代码
                cacheOptions.DeconstructType(out string cacheType, out string dataType);
                switch (cacheOptions.Type)
                {
                    case CacheType.ObjectCache:
                        builder.Append(context.LinePrefix).Append('\t').AppendLine($"await _cacher.RemoveObject<{dataType}>({key});");
                        break;
                    case CacheType.HashCache:
                        builder.Append(context.LinePrefix).Append('\t').AppendLine($"await _cacher.RemoveHash<{dataType}>({VAR_MasterKey ?? "null"}, {key});");
                        break;
                    //  还没支持的的缓存类型，先报错
                    default:
                        context.ReportError($"删除缓存时不支持[Type]值{cacheType}", cacheOptions.Attribute);
                        break;
                }
                builder.Append(context.LinePrefix).AppendLine("}");
            }
            //  4、有数据则直接返回：若无nextCode代码，则直接返回default
            if (options.ReturnType != null)
            {
                _ = string.IsNullOrEmpty(nextRunCode)
                    ? builder.Append(context.LinePrefix).AppendLine("return default;")
                    : builder.Append(context.LinePrefix).AppendLine("return cacheNextData;");
            }

            return builder.ToString();
        }

        /// <summary>
        /// 检测【缓存DataType】和【ReturnType】是否匹配可用
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options">返回值类型</param>
        /// <param name="cacheOptions">缓存数据类型</param>
        /// <param name="onlyClass">是否仅允许class类作为返回值类型；传true时，接口等类型将报错</param>
        /// <returns>返回值信息选项；为null则默认，返回值分析则默认走<see cref="CacheMethodOptions.DataType"/></returns>
        private static ReturnTypeOptions CheckCacheReturnDataType(SourceGenerateContext context, MethodGenerateOptions options, CacheMethodOptions cacheOptions, bool onlyClass = true)
        {
            ReturnTypeOptions rtOptions = new ReturnTypeOptions(context, options, onlyClass);
            //  1、【缓存DataType】需实现【IIdentity】；否则无法分析缓存数据Id；这里在Save时，会有一个情况不用实现（returnType为字典时），但先不考虑，简化一下
            if (cacheOptions.DataTypeSymbol.IsIIdentity() == false)
            {
                string msg = $"{cacheOptions.DataTypeSymbol.Name}需要实现接口[{TYPENAME_IIdentity}]；否则无法分析缓存数据Id";
                context.ReportError(msg, cacheOptions.DataType);
                return rtOptions;
            }
            //  2、判断返回值的实际缓存类型和【缓存DataType】的匹配程度；不匹配给出提示
            /*  简化规则：二者类型一致，不考虑基类、继承的情况；在Save时，ReturnType可为基类；但Load时ReturnType不能为基类，相互矛盾，简化一下*/
            if (rtOptions.DataTypeSymbol != null)
            {
                if (rtOptions.IsDictionary == true && rtOptions.KeyType.IsString() == false)
                {
                    string msg = $"{rtOptions.DataTypeSymbol.Name}为IDictionary<TKey, TValue> 时，Key必须为string，否则无法进行缓存数据Key处理";
                    context.ReportError(msg, options.ReturnType);
                }
                //  3、类型不匹配，给出错误提示：兼容一下 TestCache? 可空类型的情况，但不是很准确，也不考虑 IList<TestCache?>的情况
                bool bValue = $"{rtOptions.DataTypeSymbol}" == $"{cacheOptions.DataTypeSymbol}"
                    || $"{rtOptions.DataTypeSymbol}".TrimEnd('?') == $"{cacheOptions.DataTypeSymbol}";
                if (bValue == false)
                {
                    string dataType = $"{cacheOptions.DataTypeSymbol.Name}";
                    dataType = $"{dataType}/IList<{dataType}>/{dataType}[]/IDictionary<string,{dataType}>";
                    dataType = $"{dataType}/Snail.Abstractions.Common.Interfaces.IDataBag<T>(T为{dataType})";
                    string msg = $"返回值{options.ReturnType}分析出的缓存数据类型{rtOptions.DataTypeSymbol}不符合[CacheMethod.DataType]类型{cacheOptions.DataTypeSymbol.Name}要求：{dataType}";
                    context.ReportError(msg, options.ReturnType);
                }
            }

            return rtOptions;
        }
        /// <summary>
        /// 检测方法参数合法性
        /// </summary>
        /// <param name="method"></param>
        /// <param name="context"></param>
        /// <param name="mustCacheKey"></param>
        /// <param name="keyOptions">CacheKey参数配置选项</param>
        /// <returns></returns>
        private static bool CheckMethodParameters(MethodDeclarationSyntax method, SourceGenerateContext context, bool mustCacheKey, out CacheKeyOptions keyOptions)
        {
            CacheKeyOptions? options = null;
            ForEachMethodParametes(method, context, (name, parameter) =>
            {
                if (parameter.AttributeLists.GetAttribute(context.Semantic, TYPENAME_CacheKeyAttribute) != null)
                {
                    context.ReportErrorIf(condition: options != null, "不支持多个[CacheKey]标记参数", parameter);
                    options = new CacheKeyOptions(parameter, context);
                }
            });
            keyOptions = options ?? default;
            //  验证CacheKey必传
            if (mustCacheKey == true && keyOptions.IsValid == false)
            {
                context.ReportErrorIf(options == null, "无[CacheKey]标记参数，无法分析【加载缓存】数据key", method);
                return false;
            }

            return true;
        }
        #endregion
    }
}

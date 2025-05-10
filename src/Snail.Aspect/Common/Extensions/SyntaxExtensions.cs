using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Snail.Aspect.Common.Extensions
{
    /// <summary>
    /// 语法树相关扩展方法
    /// </summary>
    internal static class SyntaxExtensions
    {
        //  语法树相关参照：https://learn.microsoft.com/zh-cn/dotnet/api/microsoft.codeanalysis.csharp.syntax.memberdeclarationsyntax?view=roslyn-dotnet-4.9.0

        #region 公共方法

        #region TypeSyntax：类型节点
        /// <summary>
        /// 获取类型所在的命名空间，仅返回自己
        /// </summary>
        /// <param name="type"></param>
        /// <param name="semantic"></param>
        /// <returns></returns>
        public static string GetNamespace(this TypeSyntax type, SemanticModel semantic)
        {
            var typeInfo = semantic.GetTypeInfo(type);
            return $"{typeInfo.Type?.ContainingNamespace}";

            /** 废弃下面的方式；会导致 Snail.Aspect.Web.Enumerations.HttpMethodType.Get 这类引用，会将命名空间拆开逐级往上找
            var ti = semantic.GetSymbolInfo(type).Symbol;

            //  测试的时候，用这个，输出详细信息
            //return $"命名空间：{ti?.ContainingNamespace}---{ti}---{node.GetType()}";
            //return $"{ti?.ContainingNamespace} -- {ti.ContainingType} - {ti.Name}--{type}--{type.GetType().Name}";
            return $"{ti?.ContainingNamespace}  {type}  {type.GetType().Name}"; 
            */
        }
        /// <summary>
        /// 获取类型使用到的命名空间 <br />
        ///     1、类型自身所处的命名空间 <br />
        ///     2、类型为泛型时，泛型参数类型所处的命名空间，包括泛型参数内部的泛型参数 <br />
        /// 举例：AspectTest{XC{LockList{Disposable[]}}}则会返回下列类型所在的命名空间 <br />
        ///     1、AspectTest、XC、LockList、Disposable
        /// </summary>
        /// <param name="type"></param>
        /// <param name="semantic"></param>
        /// <remarks>针对内部嵌套类型时没办法分析出来上层类型；比较复杂，不兼容这种情况</remarks>
        /// <returns></returns>
        public static List<string> GetUsedNamespaces(this TypeSyntax type, SemanticModel semantic)
        {
            //Task<List<string>>    Task<List<string>>
            //code.AppendLine(cn.ToFullString());

            // Task<List<string>>   System.Threading.Tasks.Task<System.Collections.Generic.List<string>>
            // Task<List<T>>        System.Threading.Tasks.Task<System.Collections.Generic.List<T>>
            //code.Append(type.Type.ToDisplayString());
            //code.Append("---").AppendLine(type.Type.Name);

            //  Task<List<string>>  System.Threading.Tasks  System.Collections.Generic  System
            //code.Append("*******").AppendLine(type.Type.ContainingNamespace.ToDisplayString());
            List<string> rt = type.DescendantNodesAndSelf()
                .OfType<TypeSyntax>()// TypeSyntax      以及相关派生子类  
                .Select(node => GetNamespace(node, semantic))
                .Where(ns => string.IsNullOrEmpty(ns) == false)
                .Distinct()
                .ToList();
            return rt;
        }
        /// <summary>
        /// 获取实际类型名称；举例： <br />
        ///     1、Task 返回：System.Threading.Tasks.Task <br />
        ///     2、Task&lt;HttpResult&gt; 返回：System.Threading.Tasks.Task&lt;Snail.Abstractions.Web.DataModels.HttpResult&gt; <br />
        /// </summary>
        /// <param name="node"></param>
        /// <param name="semantic"></param>
        /// <returns>加上命名空间的类型名称；如Task则返回</returns>
        /// <remarks></remarks>
        public static string GetTypeName(this TypeSyntax node, SemanticModel semantic)
        {
            var symbol = semantic.GetSymbolInfo(node).Symbol;
            return symbol == null ? null : $"{symbol}";
        }
        /// <summary>
        /// 是否是类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="semantic"></param>
        /// <returns></returns>
        public static bool IsTaskType(this TypeSyntax type, SemanticModel semantic)
        {
            string typeName = $"{semantic.GetSymbolInfo(type).Symbol}";
            return typeName == "System.Threading.Tasks.Task"
                || typeName.StartsWith("System.Threading.Tasks.Task<");
        }
        #endregion

        #region TypeDeclarationSyntax：类型定义节点，如定义class、struct、interface
        /// <summary>
        /// 获取【定义的类型】所在的命名空间
        /// </summary>
        /// <param name="tds"></param>
        /// <returns></returns>
        public static string GetNamespace(this TypeDeclarationSyntax tds)
        {
            SyntaxNode tmpNode = tds;
            while ((tmpNode = tmpNode.Parent) != null)
            {
                if (tmpNode is NamespaceDeclarationSyntax ns)
                {
                    return ns.Name.ToString();
                }
            }
            return null;
        }
        /// <summary>
        /// 获取【定义的类型】所使用到的命名空间<br />
        ///     1、自身所处命名空间<br />
        ///     2、using使用到其他类型的命名空间；不带using关键字<br />
        /// </summary>
        /// <param name="tds"></param>
        /// <remarks>针对内部嵌套类型时没办法分析出来上层类型；比较复杂，不兼容这种情况</remarks>
        /// <returns></returns>
        public static IList<string> GetUsedNamespaces(this TypeDeclarationSyntax tds)
        {
            //  往↑找节点，然后看使用到的using节点
            List<string> nss = new List<string>();
            SyntaxNode tmpNode = tds.Parent;
            while (tmpNode != null)
            {
                foreach (var cNode in tmpNode.ChildNodes())
                {
                    switch (cNode)
                    {
                        //   using static xxx.xx;
                        case UsingDirectiveSyntax ud:
                            nss.Add($"{ud.StaticKeyword} {ud.Name}".Trim());
                            break;
                        //  namespace xxxx.xxxx.xx
                        case NamespaceDeclarationSyntax nd:
                            nss.Add($"{nd.Name}");
                            break;
                        //  
                        default: break;
                    }
                }
                //  继续往上找
                tmpNode = tmpNode.Parent;
            }
            return nss;
        }
        #endregion

        #region 属性相关：AttributeSyntax、AttributeArgumentSyntax、SyntaxList<AttributeListSyntax>
        /// <summary>
        /// 获取属性列表，转成<see cref="IList{AttributeSyntax}"/>更通用
        /// </summary>
        /// <param name="attributeLists"></param>
        /// <returns></returns>
        public static IEnumerable<AttributeSyntax> GetAttributes(this SyntaxList<AttributeListSyntax> attributeLists)
        {
            List<AttributeSyntax> atts = new List<AttributeSyntax>();
            foreach (var aList in attributeLists)
            {
                foreach (var attr in aList.Attributes)
                {
                    yield return attr;
                }
            }
        }
        /// <summary>
        /// 获取指定类型的属性
        /// </summary>
        /// <param name="attributeLists"></param>
        /// <param name="semantic"></param>
        /// <param name="typeSymbol">属性类型的Symbol，访问此类型的全路径：如“Snail.Aspect.Web.Enumerations.HttpMethodType”</param>
        /// <returns></returns>
        public static AttributeSyntax GetAttribute(this SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semantic, string typeSymbol)
        {
            //  遍历找属性值
            foreach (var aList in attributeLists)
            {
                foreach (var node in aList.Attributes)
                {
                    var symbol = semantic.GetSymbolInfo(node).Symbol;
                    if (symbol != null && $"{symbol.ContainingType}" == typeSymbol)
                    {
                        return node;
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 获取属性列表用到的命名空间
        /// </summary>
        /// <param name="attributeLists"></param>
        /// <param name="semantic"></param>
        /// <returns></returns>
        public static IList<string> GetUsedNamespaces(this SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semantic)
        {
            List<string> nss = new List<string>();
            foreach (var aList in attributeLists)
            {
                foreach (var attr in aList.Attributes)
                {
                    nss.TryAddRange(attr.GetUsedNamespaces(semantic));
                }
            }
            return nss;
        }

        /// <summary>
        /// 获取属性的参数信息
        /// </summary>
        /// <param name="attr"></param>
        /// <returns>属性参数语法列表</returns>
        /// <remarks>若为构造方法参数，AttributeArgumentSyntax无NameEquals属性</remarks>
        public static IList<AttributeArgumentSyntax> GetArguments(this AttributeSyntax attr)
        {
            //  最初是想转成字典，但属性构造方法传入参数，无Name值，转字典会导致多个参数冲掉
            List<AttributeArgumentSyntax> args = new List<AttributeArgumentSyntax>();
            args.TryAddRange(attr.ArgumentList?.Arguments.Select(arg => arg));
            return args;
        }
        /// <summary>
        /// 获取【属性节点】所使用到的命名空间 <br />
        ///     1、属性自身类型的命名空间，如<see cref="Web.Attributes.HttpAspectAttribute"/> <br />
        ///     2、使用属性时传入的参数值类型命名空间，如[Http("Test", Workspace = "Test", Code = Xx.Xxx.Code, Analyzer = Cons.Analyzer)]中Cons.Analyzer的Cons类命名空间<br />
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="semantic"></param>
        /// <remarks>针对内部嵌套类型时没办法分析出来上层类型；比较复杂，不兼容这种情况</remarks>
        /// <returns></returns>
        public static IList<string> GetUsedNamespaces(this AttributeSyntax attr, SemanticModel semantic)
        {
            List<string> nss = new List<string>();
            //  获取自身，Name类型是TypeSyntax的派生类；可以直接通过语法树分析命名空间，不用单独分析取
            {
                nss.TryAddRange(attr.Name.GetUsedNamespaces(semantic));
                //nss.TryAdd($"{semantic.GetSymbolInfo(attr).Symbol?.ContainingNamespace}");
                //  若Name是GenericNameSyntax，则可以分析出 TypeArgumentList.Arguments信息
                //if (attr.Name is GenericNameSyntax gs)
                //{
                //    foreach (var typeArg in gs.TypeArgumentList.Arguments)
                //    {
                //        nss.TryAddRange(typeArg.GetNamespaces(semantic));
                //    }
                //}
            }
            //  获取使用到的属性参数值命名空间
            if (attr.ArgumentList != null && attr.ArgumentList.Arguments.Count > 0)
            {
                foreach (var arg in attr.ArgumentList.Arguments)
                {
                    nss.TryAddRange(arg.GetUsedNamespaces(semantic));
                }
            }

            return nss;
        }

        /// <summary>
        /// 获取【属性参数】所用到的命名空间 <br />
        ///     1、仅分析参数值类型所在命名空间
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="semantic"></param>
        /// <remarks>针对内部嵌套类型时没办法分析出来上层类型；比较复杂，不兼容这种情况</remarks>
        /// <returns></returns>
        public static IList<string> GetUsedNamespaces(this AttributeArgumentSyntax arg, SemanticModel semantic)
        {
            /*  不用管参数值，只管参数值即可，且主要考虑 值为引入类型的属性、typeof值等情况 
             *      1、引入其他类型的值：
             *          Analyzer = Cons.Analyzer        
             *          XC<LockList<Disposable>>.Type
             *      2、通过using static直接访问的属性值：Analyzer = Analyzer   
             *          TT = typeof(XC<LockList<Disposable>>)
             *          TT = typeof(Disposable)
             */
            //  找到子节点，找到类型语法节点，取信息
            List<string> nss = arg.ChildNodes()
                .Where(item => item is MemberAccessExpressionSyntax    //   成员访问 XXX.Str
                    || item is TypeOfExpressionSyntax                  //   typeof
                    || item is InvocationExpressionSyntax              //   nameof
                )
                .SelectMany(item => item.DescendantNodes())
                .OfType<TypeSyntax>()
                .Select(type => type.GetNamespace(semantic))
                .Where(ns => string.IsNullOrEmpty(ns) == false)
                .ToList();
            //  测试时做分类使用
            //nss.Insert(0, $"====={arg};{string.Join("\t\n    ", arg.DescendantNodes().Select(node => $"{node}\t{node.GetType().Name}"))}");
            //nss.Insert(0, $"====={arg}");


            return nss;
        }
        /// <summary>
        /// 参数值是null或者空字符串
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this AttributeArgumentSyntax arg)
        {
            string value = arg?.Expression == null
                ? null
                : $"{arg.Expression}";
            return string.IsNullOrEmpty(value)
                || value == "\"\""
                || value == "null";
        }
        #endregion

        #region MethodDeclarationSyntax：方法节点
        /// <summary>
        /// 方法是否已经实现过了
        /// </summary>
        /// <param name="mNode"></param>
        /// <returns></returns>
        public static bool HasImplemented(this MethodDeclarationSyntax mNode)
        {
            var node = mNode.ChildNodes().LastOrDefault();
            //  若实现了，则最后一个子节点是 BlockSyntax或者ArrowExpressionClauseSyntax
            return node != null
                && (node is BlockSyntax || node is ArrowExpressionClauseSyntax);
        }
        /// <summary>
        /// 获取方法的真是返回值类型；
        /// </summary>
        /// <param name="mNode"></param>
        /// <param name="semantic"></param>
        /// <param name="isAsync">out参数：方法是否是Task异步</param>
        /// <param name="ns">方法返回值用到的数据类型的命名空间</param>
        /// <remarks>针对内部嵌套类型时没办法分析出来上层类型；比较复杂，不兼容这种情况</remarks>
        /// <returns>若为Task{T}则返回T；若为void则返回null</returns>
        public static TypeSyntax GetRealReturnType(this MethodDeclarationSyntax mNode, SemanticModel semantic, out bool isAsync, out List<string> ns)
        {
            ns = mNode.ReturnType.GetUsedNamespaces(semantic);
            isAsync = mNode.ReturnType.IsTaskType(semantic);

            TypeSyntax type = isAsync
                ? mNode.ReturnType.DescendantNodes().OfType<TypeSyntax>().FirstOrDefault()
                : mNode.ReturnType;
            //  type.ToFullString().Trim() == "void"
            string typeName = type?.ToFullString()?.Trim();
            return typeName == "void"
                ? null
                : type;
        }

        ///// <summary>
        ///// 方法是否是void方法；无法返回值
        ///// </summary>
        ///// <param name="mNode"></param>
        ///// <remarks>mNode.Modifiers是空的，即使标记为void，不能使用此方式判断</remarks>
        ///// <returns></returns>
        //public static bool IsVoidMethod(this MethodDeclarationSyntax mNode)
        //    => mNode.Modifiers.Any(m => m.IsKind(SyntaxKind.VoidKeyword));
        #endregion

        #region MemberDeclarationSyntax：成员节点（属性、字段、方法）
        /// <summary>
        /// 是否是静态成员（方法、属性、变量、、）
        /// </summary>
        /// <param name="mNode"></param>
        /// <returns></returns>
        public static bool IsStatic(this MemberDeclarationSyntax mNode)
            => mNode.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
        /// <summary>
        /// 是否是私有成员（方法、属性、变量、、）
        /// </summary>
        /// <param name="mNode"></param>
        /// <returns></returns>
        public static bool IsPrivate(this MemberDeclarationSyntax mNode)
            => mNode.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));
        #endregion

        #region ParameterSyntax：参数节点
        /// <summary>
        /// 是否是out参数
        /// </summary>
        /// <param name="syntax"></param>
        /// <returns></returns>
        public static bool IsOutParameter(this ParameterSyntax syntax)
            => syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword));
        #endregion

        #endregion
    }
}

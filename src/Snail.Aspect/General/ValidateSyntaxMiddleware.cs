using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.DataModels;
using Snail.Aspect.Common.Delegates;
using Snail.Aspect.Common.Extensions;
using Snail.Aspect.Common.Interfaces;
using Snail.Aspect.General.Attributes;

namespace Snail.Aspect.General
{
    /// <summary>
    /// 【Validate】语法中间件 <br />
    ///     1、侦测打了<see cref="ValidateAspectAttribute"/>标签的interface、class，为其生成实现class，并注册为组件 <br />
    ///     2、自动为方法加入【验证逻辑】，如验证方法参数有效性，字段属性有效性
    /// </summary>
    internal class ValidateSyntaxMiddleware : ITypeDeclarationMiddleware
    {
        #region 属性变量
        /// <summary>
        /// 类型名：<see cref="ValidateAspectAttribute"/>
        /// </summary>
        protected static readonly string TYPENAME_ValidateAspectAttribute = typeof(ValidateAspectAttribute).FullName;
        #endregion

        #region 构造方法
        /// <summary>
        /// 私有构造方法
        /// </summary>
        /// <param name="cacheAttr"></param>
        private ValidateSyntaxMiddleware(AttributeSyntax cacheAttr)
        {
            // ANode = cacheAttr;
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
                AttributeSyntax attr = node.AttributeLists.GetAttribute(semantic, TYPENAME_ValidateAspectAttribute);
                return attr != null
                    ? new ValidateSyntaxMiddleware(attr)
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
            return null;
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
            return null;
        }
        #endregion
    }
}
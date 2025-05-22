using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Snail.Aspect.Common;
using Snail.Aspect.Common.Interfaces;
using Snail.Aspect.Distribution;
using Snail.Aspect.General;
using Snail.Aspect.General.Attributes;
using Snail.Aspect.Web;
using Snail.Aspect.Web.Attributes;

namespace Snail.Aspect
{
    /// <summary>
    /// 源码生成器 <br />
    ///     1、自动为打了<see cref="HttpAspectAttribute"/>标签的Interface生成实现class，并注册为组件 <br />
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class SourceGenerator : IIncrementalGenerator
    {
        #region 构造方法
        /// <summary>
        /// 静态构造方法
        /// </summary>
        static SourceGenerator()
        {
            //  配置【class和Interface】语法的源码生成中间件
            //      打了<see cref="ValidateAspectAttribute"/>标签的class和interface节点，自动生成实现类
            ClassInterfaceSyntaxProxy.Config(ValidateSyntaxMiddleware.Build);
            //      通用的切面编程句柄方法；为实现【IAspectHandle】接口的方法生成
            ClassInterfaceSyntaxProxy.Config(MethodSyntaxMiddleware.Build);
            //      打了<see cref="LockAspectAttribute"/>标签的class和interface节点，自动生成实现类
            ClassInterfaceSyntaxProxy.Config(LockSyntaxMiddleware.Build);
            //      打了<see cref="CacheAspectAttribute"/>标签的class和interface节点，自动生成实现类
            ClassInterfaceSyntaxProxy.Config(CacheSyntaxMiddleware.Build);
            //      打了<see cref="HttpAspectAttribute"/>标签的Interface节点，自动生成实现类
            ClassInterfaceSyntaxProxy.Config(HttpSyntaxMiddleware.Build);
        }
        #endregion

        #region IIncrementalGenerator
        /// <summary>
        /// 初始化；用于遍历分析语法
        /// </summary>
        /// <param name="context"></param>
        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            //  语法提供程序
            var providers = new List<IncrementalValueProvider<ImmutableArray<ISyntaxProxy>>>()
            {
                //  HTTP语法代理器提供程序：HttpAttribute标记接口，自动生成实现类
                //HttpSyntaxProxy.BuildProvider(context).Collect(),//合并到ClassInterfaceSyntaxProxy处理
                //  为类+接口代理生成源码：predicate先始终为true，具体的处理，在transform中做逻辑
                ClassInterfaceSyntaxProxy.BuildProvider(context).Collect(),
            };
            //  遍历注册提供程序，并执行源码生成
            foreach (var provider in providers)
            {
                context.RegisterSourceOutput(provider, (ctx, proxies) =>
                {
                    foreach (ISyntaxProxy proxy in proxies.Where(px => px != null))
                    {
                        string code = proxy.GenerateCode(ctx);
                        //  Key值为空、null，则不生成源码；生辰源码时加上时间戳和程序集信息，方便查问题
                        if (string.IsNullOrEmpty(proxy.Key) == false)
                        {
                            string declaration = $"//{nameof(SourceGenerator)}:{DateTime.Now} {typeof(SourceGenerator).Assembly.FullName}";
                            ctx.AddSource($"{proxy.Key}.g.cs", $"{declaration}\r\n{code ?? string.Empty}");
                        }
                    }
                });
            }
        }
        #endregion
    }
}

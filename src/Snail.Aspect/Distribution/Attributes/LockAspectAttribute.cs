﻿using System;
using Snail.Aspect.Distribution.Interfaces;

namespace Snail.Aspect.Distribution.Attributes
{
    /// <summary>
    /// 特性标签：锁切面，标记当前类中有需要并发锁
    ///     1、约束缓存服务器；此类下的<see cref="CacheMethodAttribute"/>标记的方法进行自动进行缓存操作<br />
    ///     2、配合【Snail.Aspect】项目使用，自动为接口生成实现类，并注册为依赖注入组件 <br />
    /// </summary>
    /// <remarks>------------------------------------------------------------------------------------------- <br />
    ///     1、目标项目引入【Snail.Aspect】项目包，并在引用包的条目上增加配置： <br />
    ///         OutputItemType="Analyzer" ReferenceOutputAssembly="true" <br />
    ///     2、目标项目文件“PropertyGroup”节点下增加如下配置，即可看到生成的源代码<br />
    ///          &lt;EmitCompilerGeneratedFiles&gt;true&lt;/EmitCompilerGeneratedFiles&gt;
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class LockAspectAttribute : Attribute
    {
        /// <summary>
        /// 缓存服务器所在工作空间Key值；<br /> 
        /// </summary>
        public string Workspace { set; get; }

        /// <summary>
        /// 缓存服务器类型；用于对多个服务器做分组用<br /> 
        ///     1、无分组的服务器取null即可<br /> 
        /// </summary>
        public string Type { set; get; }

        /// <summary>
        /// 缓存服务器编码
        /// </summary>
        public string Code { set; get; }

        /// <summary>
        /// 并发锁分析器<see cref="ILockAnalyzer"/>的依赖注入Key值
        /// </summary>
        /// <remarks>不传入则采用默认的分析器</remarks>
        public string Analyzer { set; get; }
    }
}

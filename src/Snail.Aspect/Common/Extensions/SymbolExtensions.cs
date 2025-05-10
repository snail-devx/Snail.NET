using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Snail.Aspect.Common.Extensions
{
    /// <summary>
    /// 语法符号 相关扩展方法
    /// </summary>
    internal static class SymbolExtensions
    {
        #region 公共方法

        #region ITypeSymbol
        /// <summary>
        /// 是否是class
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsClass(this ITypeSymbol type)
        {
            return type.TypeKind == TypeKind.Class;
        }

        /// <summary>
        /// 是否是[string]类型符号
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsString(this ITypeSymbol type)
        {
            string typeName = $"{type}";
            return typeName == "string" || typeName == "String";
        }
        /// <summary>
        /// 是否是数组
        /// </summary>
        /// <param name="type"></param>
        /// <param name="elementType">数组元素类型；如string[],则为string</param>
        /// <returns></returns>
        public static bool IsArray(this ITypeSymbol type, out ITypeSymbol elementType)
        {
            elementType = type is IArrayTypeSymbol ats ? ats.ElementType : null;
            return elementType != null;
        }
        /// <summary>
        /// 是否是List集合；实现IList接口
        /// </summary>
        /// <param name="type"></param>
        /// <param name="genericArgType">泛型类型，如<see cref="IList{Int32}"/>则为<see cref="Int32"/></param>
        /// <param name="inherit">是否算继承类型：为true时<paramref name="type"/>及基类实现了<see cref="IList{T}"/>也算List</param>
        /// <returns></returns>
        public static bool IsList(this ITypeSymbol type, out ITypeSymbol genericArgType, bool inherit = true)
        {
            //  遍历自身+基类实现接口
            genericArgType = null;
            if (type is INamedTypeSymbol nrt && nrt.IsGenericType)
            {
                string TypeName = $"{type}";
                if (TypeName.StartsWith("System.Collections.Generic.IList<") == true
                    || TypeName.StartsWith("System.Collections.Generic.List<") == true)
                {
                    genericArgType = nrt.TypeArguments.First();
                    return true;
                }
            }
            if (inherit == true)
            {
                foreach (INamedTypeSymbol iNode in type.AllInterfaces)
                {
                    if (IsList(iNode, out genericArgType, inherit: false) == true)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 是否是字典；实现IDictionary接口
        /// </summary>
        /// <param name="type"></param>
        /// <param name="keyType">字典Key类型</param>
        /// <param name="valueType">字典Value类型</param>
        /// <param name="inherit">是否算继承类型：为true时<paramref name="type"/>及基类实现了<see cref="IDictionary{TKey, TValue}"/>也算IDictionary</param>
        /// <returns></returns>
        public static bool IsDictionary(this ITypeSymbol type, out ITypeSymbol keyType, out ITypeSymbol valueType, bool inherit = true)
        {
            //  遍历自身+基类实现接口
            keyType = null;
            valueType = null;
            if (type is INamedTypeSymbol nrt && nrt.IsGenericType)
            {
                string TypeName = $"{type}";
                if (TypeName.StartsWith("System.Collections.Generic.IDictionary<") == true
                    || TypeName.StartsWith("System.Collections.Generic.Dictionary<") == true)
                {
                    keyType = nrt.TypeArguments.First();
                    valueType = nrt.TypeArguments.Last();
                    return true;
                }
            }
            if (inherit == true)
            {
                foreach (INamedTypeSymbol iNode in type.AllInterfaces)
                {
                    if (IsDictionary(iNode, out keyType, out valueType, inherit: false) == true)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 是否是数据包类型：Snail.Abstractions.Common.Interfaces.IDataBag
        /// </summary>
        /// <param name="type"></param>
        /// <param name="genericArgType">泛型类型，如IDataBag{Int32}则为<see cref="Int32"/></param>
        /// <param name="inherit">是否算继承类型：为true时<paramref name="type"/>及基类实现了IDataBag{Int32}也算IDataBag</param>
        /// <returns></returns>
        public static bool IsDataBag(this ITypeSymbol type, out ITypeSymbol genericArgType, bool inherit = true)
        {
            //  遍历自身+基类实现接口
            genericArgType = null;
            if (type is INamedTypeSymbol nrt && nrt.IsGenericType)
            {
                if ($"{type}".StartsWith("Snail.Abstractions.Common.Interfaces.IDataBag<") == true)
                {
                    genericArgType = nrt.TypeArguments.First();
                    return true;
                }
            }
            if (inherit == true)
            {
                foreach (INamedTypeSymbol iNode in type.AllInterfaces)
                {
                    if (IsDataBag(iNode, out genericArgType, inherit: false) == true)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 是否是【IIdentity】类型：Snail.Abstractions.Identity.Interfaces.IIdentity
        /// </summary>
        /// <param name="type"></param>
        /// <param name="inherit">是否算竭诚类型：为true时，则查找<paramref name="type"/>的所有实现接口</param>
        /// <returns></returns>
        public static bool IsIIdentity(this ITypeSymbol type, bool inherit = true)
        {
            //  遍历自身+基类实现接口
            if ($"{type}" == "Snail.Abstractions.Identity.Interfaces.IIdentity" == true)
            {
                return true;
            }
            if (inherit == true)
            {
                foreach (INamedTypeSymbol iNode in type.AllInterfaces)
                {
                    if (IsIIdentity(iNode) == true)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #endregion
    }
}

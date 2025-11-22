using Snail.Utilities.Common.Utils;

namespace Snail.Utilities.Common.Extensions
{
    /// <summary>
    ///     <see cref="Type"/>扩展方法 <br />
    /// </summary>
    public static class TypeExtensions
    {
        #region 属性变量
        /// <summary>
        /// IList类型
        /// </summary>
        private static readonly Type _iListType = typeof(IList<>);
        /// <summary>
        /// String类型
        /// </summary>
        private static readonly Type _stringType = typeof(string);
        /// <summary>
        /// 可空类型
        /// </summary>
        private static readonly Type _nullableType = typeof(Nullable<>);
        #endregion

        #region 公共方法
        /// <summary>
        /// 是否是基础数据类型：C#基元类型、String、枚举、值类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsBaseType(this Type type)
        {
            //   IsPrimitive：Boolean 、、 Byte SByte 、 Int16 、、 UInt16 Int32 、、 UInt32 Int64 UInt64 IntPtr UIntPtr Char Double Single 
            return type.IsPrimitive == true
                || type.IsEnum == true
                || type.IsValueType == true
                || type == _stringType;
        }

        /// <summary>
        /// 是否是String类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsString(this Type type) => type == _stringType;

        /// <summary>
        /// <paramref name="type"/>是否是泛型类型创建<br />
        /// 1、即<paramref name="type"/>是通过<see cref="Type.MakeGenericType(Type[])"/>指定泛型参数构建出来的新类型<br />
        /// 2、举例List&lt;String&gt;就是基于<see cref="List{T}"/>构建的
        /// </summary>
        /// <param name="type"></param>
        /// <param name="definitionType">out参数：泛型定义类型，如<see cref="List{String}"/>，则definitiontype=typeof <see cref="List{T}"/></param>
        /// <returns>是泛型类型创建的新类型返回true；否则返回false</returns>
        public static bool IsGenericMakeType(this Type type, out Type? definitionType)
        {
            definitionType = type.IsGenericType == true
                ? type.GetGenericTypeDefinition()
                : null;
            return definitionType != null;
        }
        /// <summary>
        /// <paramref name="type"/>是否是<paramref name="definitionType"/>泛型创建的类型<br />
        ///     1、即<paramref name="type"/>是通过<see cref="Type.MakeGenericType(Type[])"/>指定泛型参数构建出来的新类型<br />
        ///     2、举例List&lt;String&gt;就是基于<see cref="List{T}"/>构建的
        /// </summary>
        /// <param name="type"></param>
        /// <param name="definitionType">泛型类型，如 typeof(<see cref="List{T}"/>)</param>
        /// <returns>是泛型类型<paramref name="definitionType"/>创建的新类型返回true；否则返回false</returns>
        public static bool IsGenericMakeType(this Type type, Type definitionType)
        {
            //  直接调用 IsMakeGenericType(this Type type, out Type? definitionType) 更方便，但更繁琐，简化调用堆栈
            return type.IsGenericType == true
               ? type.GetGenericTypeDefinition() == definitionType
               : false;
        }
        /// <summary>
        /// 是否是 List。如<see cref="List{String}"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="genericArg">集合泛型参数，如<see cref="List{String}"/>，则genericArg=typeof(<see cref="string"/>) </param>
        /// <returns></returns>
        public static bool IsList(this Type type, out Type? genericArg)
        {
            //  判断是否是List的泛型
            genericArg = null;
            if (type.IsGenericMakeType(out Type? definitionType) == true)
            {
                genericArg = definitionType == _iListType || definitionType!.GetInterface(_iListType.Name) != null
                     ? type.GenericTypeArguments[0]
                     : null;
            }
            return genericArg != null;
        }
        /// <summary>
        /// 是否是数组，如String[]
        /// </summary>
        /// <param name="type"></param>
        /// <param name="genericArg">数组泛型参数，如String[]，则genericArg = typeof(String)</param>
        /// <returns></returns>
        public static bool IsArray(this Type type, out Type? genericArg)
        {
            genericArg = null;
            //  数组的具体类型比较麻烦，拼接上具体的程序集名称。遇到ExpressType不拼接加载不出来
            if (type.IsArray == true)
            {
                string newType = $"{type.FullName![..^2]},{type.Assembly.FullName}";
                genericArg = TypeHelper.LoadType(newType);
            }
            return genericArg != null;
        }
        /// <summary>
        /// 是否是可空类型，如int? bool?
        /// </summary>
        /// <param name="type"></param>
        /// <param name="genericArg">nullable泛型参数，如bool?，则genericArg=typeof（bool）</param>
        /// <returns></returns>
        public static bool IsNullable(this Type type, out Type? genericArg)
        {
            //  判断是否是Nullable的泛型
            genericArg = type.IsGenericMakeType(_nullableType) == true
                    ? type.GenericTypeArguments[0]
                    : null;
            return genericArg != null;
        }
        /// <summary>
        /// 是否是指定类型的可空类型
        ///     如判断是否是int?
        /// </summary>
        /// <param name="type"></param>
        /// <param name="targetType">谁的可空类型，如typeof(int)表示 int的可空类型判断 int?</param>
        /// <returns></returns>
        public static bool IsNullable(this Type type, Type? targetType)
            => type.IsNullable(out Type? genericType) == true && genericType == targetType;

        /// <summary>
        /// 是否是可空枚举类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="genericArg">nullable泛型参数，如 DataType?，则genericArg=typeof（DataType）</param>
        /// <returns></returns>
        public static bool IsEnumNullable(this Type type, out Type? genericArg)
        {
            type.IsNullable(out genericArg);
            genericArg = genericArg?.IsEnum == true ? genericArg : null;
            return genericArg != null;
        }
        #endregion
    }
}

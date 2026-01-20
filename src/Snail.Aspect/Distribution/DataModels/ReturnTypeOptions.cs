using Microsoft.CodeAnalysis;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.DataModels;
using Snail.Aspect.Common.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail.Aspect.Distribution.DataModels;

/// <summary>
/// [CacheMethod]方法的返回值配置选项
/// </summary>
internal readonly struct ReturnTypeOptions
{
    #region 属性变量
    /// <summary>
    /// 实际的返回值数据类型，可用于缓存操作的数据类型
    /// <para>1、<see cref="IsPayload"/>为true时，则是指IPayload&lt;T&gt;的T类型 </para>
    /// <para>2、<see cref="IsList"/>为true时， List{T}的T类型 </para>
    /// <para>、、、 </para>
    /// </summary>
    public ITypeSymbol? DataTypeSymbol { get; }

    /// <summary>
    /// 是否是IPayload对象
    /// </summary>
    public bool IsPayload { get; }

    /// <summary>
    /// 是否是多模式：List、Dictionary、Array、、、
    /// <para>1、为false时，为单模式 </para>
    /// <para>2、<see cref="IsPayload"/>为true时，则是指IPayload&lt;T&gt;的T类型  </para>
    /// </summary>
    public bool IsMulti { get; }
    /// <summary>
    /// 多模式类型：List{string}、string[]、、、
    /// <para>1、<see cref="IsMulti"/>为true时，生效 </para>
    /// <para>2、<see cref="IsPayload"/>为true时，则是指IPayload&lt;T&gt;的T类型  </para>
    /// </summary>
    public ITypeSymbol? MultiTypeSymbol { get; }

    /// <summary>
    /// 是否是数组
    /// <para>1、<see cref="IsMulti"/>为true时，生效 </para>
    /// <para>2、<see cref="IsPayload"/>为true时，则是指IPayload&lt;T&gt;的T类型  </para>
    /// </summary>
    public bool IsArray { get; }
    /// <summary>
    /// 是否是List对象
    /// <para>1、<see cref="IsMulti"/>为true时，生效 </para>
    /// <para>2、<see cref="IsPayload"/>为true时，则是指IPayload&lt;T&gt;的T类型  </para>
    /// </summary>
    public bool IsList { get; }

    /// <summary>
    /// 是否是Dictionary
    /// <para>1、<see cref="IsMulti"/>为true时，生效 </para>
    /// <para>2、<see cref="IsPayload"/>为true时，则是指IPayload&lt;T&gt;的T类型  </para>
    /// </summary>
    public bool IsDictionary { get; }
    /// <summary>
    /// 字典Key类型
    /// <para>1、<see cref="IsDictionary"/>为true时，生效 </para>
    /// </summary>
    public ITypeSymbol? KeyType { get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="context"></param>
    /// <param name="options"></param>
    /// <param name="onlyClass">是否仅允许class类作为返回值类型；传true时，接口等类型将报错</param>
    public ReturnTypeOptions(SourceGenerateContext context, MethodGenerateOptions options, bool onlyClass = true)
    {
        //  默认值处理
        {
            DataTypeSymbol = null;
            IsPayload = false;
            IsMulti = false;
            MultiTypeSymbol = null;
            IsArray = false;
            IsList = false;
            IsDictionary = false;
            KeyType = null;
        }
        //  解析分析
        if (options.ReturnType != null)
        {
            //  本地方法：基于类型的Kind值检测合法性；需要注意 可能存在 TestCache[]；也不是class，但允许使用，这里先不考虑
            void CheckReturnTypeByKind(ITypeSymbol checkType)
            {
                if (checkType.IsArray(out ITypeSymbol? tmpType) == true)
                {
                    checkType = tmpType!;
                }
                if (onlyClass == true && checkType.IsClass() == false)
                {
                    context.ReportError($"{checkType}必须是class类型", options.ReturnType);
                }
            }

            //  遍历分析自身+实现接口 分析
            DataTypeSymbol = context.Semantic.GetTypeInfo(options.ReturnType).Type;
            CheckReturnTypeByKind(DataTypeSymbol!);
            //      IPayload类型数据，则分析Payload类型作为实际数据类型
            if (DataTypeSymbol!.IsPayload(out ITypeSymbol? realType, inherit: true) == true)
            {
                IsPayload = true;
                CheckReturnTypeByKind(realType!);
                DataTypeSymbol = realType;
            }
            //      整理遍历做分析自身+实际实现接口
            List<ITypeSymbol> types = [DataTypeSymbol!, .. DataTypeSymbol!.AllInterfaces];
            realType = null;
            foreach (var ti in types)
            {
                //  Array数组
                if (ti.IsArray(out realType) == true)
                {
                    IsArray = true;
                    MultiTypeSymbol = DataTypeSymbol;
                    break;
                }
                //  List集合
                if (ti.IsList(out realType, inherit: false) == true)
                {
                    IsList = true;
                    MultiTypeSymbol = DataTypeSymbol;
                    break;
                }
                //  Dictionary字典
                if (ti.IsDictionary(out ITypeSymbol? keyType, out realType, inherit: false) == true)
                {
                    IsDictionary = true;
                    MultiTypeSymbol = DataTypeSymbol;
                    KeyType = keyType;
                    break;
                }
            }
            if (realType != null)
            {
                CheckReturnTypeByKind(realType);
                DataTypeSymbol = realType;
            }
            //  返回值为多值类型
            IsMulti = IsArray || IsList || IsDictionary;
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 获取IPayload类型名称：返回IPayload{具体类型}
    /// </summary>
    /// <returns></returns>
    public string? GetPayloadTypeName()
        => IsPayload ? $"IPayload<{GetSymbolName(MultiTypeSymbol ?? DataTypeSymbol!)}>" : null;
    /// <summary>
    /// 获取多类型名称；如IList{具体类型}
    /// </summary>
    /// <returns></returns>
    public string? GetMultiTypeName()
        => MultiTypeSymbol != null ? GetSymbolName(MultiTypeSymbol) : null;
    #endregion

    #region 私有方法
    /// <summary>
    /// 获取类型名称
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static string GetSymbolName(ITypeSymbol type)
    {
        /* 还需要支持对?的判断逻辑 */

        StringBuilder code = new();
        switch (type.Name)
        {
            case "String":
            case "Int32":
            case "Object":
                code.Append(type.Name.ToLower());
                break;
            default: code.Append(type.Name); break;
        }
        if (type is INamedTypeSymbol nts && nts.TypeArguments.Length > 0)
        {
            code.Append('<');
            code.Append(string.Join(", ", nts.TypeArguments.Select(GetSymbolName)));
            code.Append('>');
        }
        //  如果是数组的话，需要追加上
        code.Append(type.TypeKind == TypeKind.Array ? "[]" : string.Empty);

        return code.ToString();
    }
    #endregion
}

using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.DataModels;
using Snail.Utilities.Collections;
using System.Collections.ObjectModel;

namespace Snail.Database.Utils;

/// <summary>
/// 数据库实体助手类：用于进行数据库实体特性标签分析
/// </summary>
public static class DbModelHelper
{
    #region 属性变量
    /// <summary>
    /// 数据实体-数据表映射。key为数据实体Type；value为对应数据表描述信息
    /// </summary>
    private static readonly LockMap<Type, DbModelTable> _dbTableMap = new();
    /// <summary>
    /// 无效基类
    /// </summary>
    private static readonly List<Type> _assignableFromTypes = new()
    {
        typeof(Array),
        typeof(Enum),
        typeof(string),
    };
    #endregion

    #region 公共方法
    /// <summary>
    /// 获取数据库实体对应的数据表信息
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <returns></returns>
    public static DbModelTable GetTable<DbModel>() where DbModel : class
        => GetTable(typeof(DbModel));
    /// <summary>
    /// 获取数据库实体对应的数据表信息
    /// </summary>
    /// <param name="type">数据库实体类型；类型必须有<see cref="DbTableAttribute"/>特性标签</param>
    /// <returns></returns>
    public static DbModelTable GetTable(Type type)
    {
        ThrowIfNull(type);
        IsValidDbModelType(type, throwEx: true);
        return _dbTableMap.GetOrAdd(type, BuildTable);
    }
    /// <summary>
    /// 判断指定类型是否是DbModel
    /// </summary>
    /// <param name="type"></param>
    /// <param name="throwError">不是是，是否报错</param>
    /// <returns></returns>
    public static bool IsDbModel(Type type, bool throwError = false)
    {
        ThrowIfNull(type);
        //  验证一下有效性
        return type.GetCustomAttributes<DbTableAttribute>().Any() == true
            && IsValidDbModelType(type, throwError);
    }

    /// <summary>
    /// 构建数据库字段值；将传入属性值转换成数据库字段类型值
    /// </summary>
    /// <param name="pValue">属性值</param>
    /// <param name="field">字段信息</param>
    /// <returns></returns>
    public static object? BuildFieldValue(object? pValue, DbModelField field)
    {
        ThrowIfNull(field);
        //  值为null和非null做区分；非null做值类型检测和转换
        object? newValue;
        if (pValue == null)
        {
            newValue = null;
        }
        else if (pValue.GetType() == field.Type)
        {
            newValue = pValue;
        }
        //  为可空类型时，做特例处理
        else
        {
            field.Type.IsNullable(out Type? type);
            type ??= field.Type;
            try { newValue = Convert.ChangeType(pValue, type); }
            catch (Exception ex)
            {
                string msg = $"转换{field.Name}字段值失败：fieldType：{type}；value：{pValue.GetType()}";
                throw new ApplicationException(msg, ex);
            }
        }
        //  主键字段做验证
        if (field.PK == true)
        {
            ThrowIfNull(newValue, "主键字段值不能为null");
            if (newValue is string str)
            {
                ThrowIfNullOrEmpty(str, "主键字段值不能为空字符串");
            }
        }
        //  返回
        return newValue;
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 构建数据库实体表信息；配合<see cref="GetTable(Type)"/>构建
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static DbModelTable BuildTable(Type type)
    {
        //  分析DbTableAttribute属性：必填和值补偿；被DbTableAttribute标记的类型是否有效
        DbTableAttribute tableAttr = type.GetCustomAttribute<DbTableAttribute>()
            ?? throw new ApplicationException($"{type}必须标记DbTableAttribute特性；");
        //  分析字段属性：默认取继承属性
        List<DbModelField> fields = new List<DbModelField>();
        DbModelField? pkField = null;
        int pkCount = 0;
        foreach (PropertyInfo pi in type.GetProperties(BINDINGFLAGS_InsPublic))
        {
            //  分析属性的特性标签：不保存字段先忽略掉
            DbFieldAttribute? fieldAttr = pi.GetCustomAttribute<DbFieldAttribute>();
            if (fieldAttr != null && fieldAttr.Ignored == true)
            {
                continue;
            }
            //      不能存在同名字段
            string fieldName = Default(fieldAttr?.Name, defaultStr: pi.Name)!;
            DbModelField? field = fields.FirstOrDefault(field => field.Name == fieldName);
            if (field != null)
            {
                fieldName = $"{type}已经存在同名数据库字段。FieldName:{fieldName};Property：{field.Property}";
                throw new ApplicationException(fieldName);
            }
            //  合法，构建字段添加，并做主键Id计数
            field = new DbModelField()
            {
                Name = fieldName,
                Type = fieldAttr?.Type ?? pi.PropertyType,
                Property = pi,
                PK = fieldAttr?.PK == true,
            };
            if (field.PK == true)
            {
                pkCount += 1;
                pkField = field;
            }
            fields.Add(field);
        }
        //  字段属性必要约束
        if (fields.Count == 0)
        {
            string msg = $"{type}无有效的数据字段";
            throw new ApplicationException(msg);
        }
        if (pkCount != 1)
        {
            string msg = $"仅支持单主键实体，{type}实际主键数量为{pkCount}";
            throw new ApplicationException(msg);
        }
        //  整理返回
        return new DbModelTable()
        {
            Type = type,
            Name = Default(tableAttr.Name, type.Name)!,
            Routing = tableAttr.Routing,

            PKField = pkField!,
            Fields = new ReadOnlyCollection<DbModelField>(fields),
        };
    }

    /// <summary>
    /// 判断是否是有效的DbModel类型
    /// </summary>
    /// <param name="type"></param>
    /// <param name="throwEx">是否报错</param>
    /// <returns></returns>
    private static bool IsValidDbModelType(Type type, bool throwEx = false)
    {
        ThrowIfNull(type);
        var dealError = (string exMessage) =>
        {
            if (throwEx == true)
            {
                throw new ApplicationException($"{type}不是有效的DbModel类型：{exMessage}");
            }
            return false;
        };
        //  进行类型判断：class+非值类型
        if (type.IsClass != true)
        {
            return dealError("非Class类型");
        }
        if (type.IsValueType == true)
        {
            return dealError("值类型不能作为DbModel");
        }
        //      基元类型（Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single）
        if (type.IsPrimitive == true)
        {
            return dealError("不能为C#基元类型，如Boolean、Byte、、");
        }
        //      可以是泛型，但必须指定泛型参数
        if (type.IsGenericType == true && type.ContainsGenericParameters == true)
        {
            return dealError("泛型类，但未指定泛型参数");
        }
        //      不能是Array、Enum等
        if (_assignableFromTypes.Any(item => item.IsAssignableFrom(type) == true) == true)
        {
            return dealError("不能为Array、Enum、、、");
        }
        //      泛型List、Dictionary、、、，暂时不验证，后期再说

        //      后续再补充其他验证规则  

        //  整体验证都通过了，返回true
        return true;
    }
    #endregion
}

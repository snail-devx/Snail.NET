using System.Reflection;

namespace Snail.Test.Common;

/// <summary>
/// 反射相关测试
/// </summary>
internal class ReflectTest
{

    #region 公共方法
    /// <summary>
    /// 测试泛型方法反射调用
    /// </summary>
    [Test]
    public void TestGenericMethod()
    {
        Type thisType = typeof(ReflectTest);
        MethodInfo method = typeof(ReflectTest).GetMethod(nameof(BuildDbFieldValues_Func), BindingFlags.Static | BindingFlags.NonPublic)!;
        method = method.MakeGenericMethod(typeof(string));
        var str = method.Invoke(null, [new List<object> { "1", "ddd" }, true]);

        Delegate func = BuildDbFieldValuesDelegate(typeof(string));
        str = func.DynamicInvoke(new List<object> { "1ddd", "ddd" }, true);

    }
    #endregion

    #region 私有方法
    private static Delegate BuildDbFieldValuesDelegate(Type targetType)
    {
        MethodInfo method = typeof(ReflectTest).GetMethod(nameof(BuildDbFieldValues_Func), BindingFlags.Static | BindingFlags.NonPublic)!;
        method = method.MakeGenericMethod(targetType);
        //  构建委托
        Type funcType = typeof(Func<,,>).MakeGenericType(typeof(IList<object>), typeof(bool), typeof(IList<>).MakeGenericType(targetType));
        return Delegate.CreateDelegate(funcType, method);
    }
    /// <summary>
    /// 构建数据库字段值
    /// </summary>
    /// <typeparam name="FieldType"></typeparam>
    /// <param name="values"></param>
    /// <param name="isPK"></param>
    /// <returns></returns>
    private static IList<FieldType> BuildDbFieldValues_Func<FieldType>(IList<object> values, bool isPK)
    {
        List<FieldType> newValues = [];
        for (var index = 0; index < values.Count; index++)
        {
            FieldType newValue = (FieldType)Convert.ChangeType(values[index], typeof(FieldType));
            newValues.Add(newValue);
        }
        return newValues;
    }
    #endregion

}

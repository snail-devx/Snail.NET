using Snail.Utilities.Common.Delegates;
using System.Diagnostics;
using System.Reflection;
using static Snail.Utilities.Common.Utils.TypeHelper;

namespace Snail.Test.Common;

/// <summary>
/// 反射相关测试
/// </summary>
internal class ReflectTest
{
    #region 构造方法
    public ReflectTest()
    {

    }
    #endregion

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

    /// <summary>
    /// 测试方法代理
    /// </summary>
    [Test]
    public void TestMethodDelegate()
    {
        MethodInfo method;
        MethodDelegate func;
        //  静态方法
        method = typeof(ReflectTest).GetMethod(nameof(TestSubStringStatic1), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)!;
        func = CreateDelegate(method, out _, out _);
        Assert.That("012".Equals(func(null!, ["01234567890", 3])));
        method = typeof(ReflectTest).GetMethod(nameof(TestVoidMethodStatic2), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)!;
        func = CreateDelegate(method, out _, out _);
        Assert.That(func(null!, ["01234567890", 3]) == null);
        //  实例级方法
        method = typeof(ReflectTest).GetMethod(nameof(TestSubStringInstance1), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)!;
        func = CreateDelegate(method, out _, out _);
        Assert.That("012".Equals(func(this, ["01234567890", 3])));
        method = typeof(ReflectTest).GetMethod(nameof(TestVoidMethodInstance2), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)!;
        func = CreateDelegate(method, out _, out _);
        Assert.That(func(this, ["01234567890", 3]) == null);
        //  测试in参数
        method = typeof(ReflectTest).GetMethod(nameof(TestInMethod), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)!;
        func = CreateDelegate(method, out _, out _);
        Assert.That("012".Equals(func(this, ["01234567890", 3])));
        //  测试ref参数
        method = typeof(ReflectTest).GetMethod(nameof(TestRefMethod), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)!;
        func = CreateDelegate(method, out _, out _);
        string str = "01234567890";
        Assert.That("012".Equals(func(this, [str, 3])));
        //  测试ref参数
        method = typeof(ReflectTest).GetMethod(nameof(TestOutMethod), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)!;
        func = CreateDelegate(method, out _, out _);
        Assert.That(func(this, [str]) == null);
    }

    /// <summary>
    /// 测试构造方法
    /// </summary>
    [Test]
    public void TestConstructorDelegate()
    {
        ConstructorInfo[] method = typeof(ReflectTest).GetConstructors();
    }

    [Test]
    public void TestMethodDelegatePer()
    {
        /** 性能测试结果稳定在：创建委托比较耗时，已优化成IL代码生成，但未读懂代码，暂时不放开使用
            单次反射:1828
            100000反射:9ms
            创建委托耗时:7ms
            单次委托:7
            100000委托:3ms
         */

        MethodInfo method = typeof(ReflectTest).GetMethod(nameof(TestSubStringStatic1), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)!;
        //      单次调用
        Stopwatch sw = Stopwatch.StartNew();
        method.Invoke(null, ["01234567890", 3]);
        TestContext.Out.WriteLine($"单次反射:{sw.ElapsedTicks}");
        //      多次调用
        sw.Restart();
        for (var index = 0; index < 100000; index++)
        {
            method.Invoke(null, ["01234567890", 3]);
        }
        TestContext.Out.WriteLine($"100000反射:{sw.ElapsedMilliseconds}ms");
        //      单次委托调用
        //sw.Restart();
        //method.GetParameters();
        //TestContext.Out.WriteLine($"获取方法参数耗时:{sw.ElapsedTicks}");
        sw.Restart();
        MethodDelegate func = CreateDelegate(method, out _, out _);
        TestContext.Out.WriteLine($"创建委托耗时:{sw.ElapsedMilliseconds}ms");
        sw.Restart();
        func(null!, ["01234567890", 3]);
        TestContext.Out.WriteLine($"单次委托:{sw.ElapsedTicks}");
        //      多次委托调用
        sw.Restart();
        for (var index = 0; index < 100000; index++)
        {
            func(null!, ["01234567890", 3]);
        }
        TestContext.Out.WriteLine($"100000委托:{sw.ElapsedMilliseconds}ms");
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

    #region 委托构建测试方法
    private static string TestSubStringStatic1(string s, int length)
    {
        return s.Substring(0, length);
    }
    private static void TestVoidMethodStatic2(string s, int leng)
    {

    }
    private string TestSubStringInstance1(string s, int length)
    {
        return s.Substring(0, length);
    }
    private void TestVoidMethodInstance2(string s, int leng)
    {

    }
    /// <summary>
    /// 测试in参数
    /// </summary>
    /// <param name="s"></param>
    /// <param name="leng"></param>
    /// <returns></returns>
    private string TestInMethod(in string s, in int leng)
    {
        return s.Substring(0, leng);
    }
    /// <summary>
    /// 测试ref参数
    /// </summary>
    /// <param name="s"></param>
    /// <param name="leng"></param>
    /// <returns></returns>
    private string TestRefMethod(ref string s, ref int leng)
    {
        s = s.Substring(0, leng);
        leng = s.Length;
        return s;
    }
    /// <summary>
    /// 测试ref参数
    /// </summary>
    /// <param name="newStr"></param>
    /// <returns></returns>
    private void TestOutMethod(out string newStr)
    {
        newStr = Guid.NewGuid().ToString();
    }
    #endregion

}

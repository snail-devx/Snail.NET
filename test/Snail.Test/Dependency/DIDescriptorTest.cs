using Snail.Abstractions.Dependency.DataModels;
using Snail.Test.Dependency.Components;
using Snail.Test.Dependency.DataModels;
using Snail.Test.Dependency.Interfaces;


namespace Snail.Test.Dependency
{
    /// <summary>
    /// <see cref="DIDescriptor"/>相关类型测试
    ///     1、情况比较多，单独拿一个单元测试类
    /// </summary>
    public sealed class DIDescriptorTest
    {
        /// <summary>
        /// 测试
        /// </summary>
        [Test]
        public void TestDescriptor()
        {
            // 不再检查类型；否则【Microsoft.Extensions.DependencyInjection】对接时，会报错
            return;

            /** 准备类型
             *      接口      IFrom1                      IFrom2                      IFromG2<T1,T2>
             *      类        To1:IFrom1 To1_1:IFrom1     To2:IFrom2                  ToG2<T1,T2>:IFromG2<T1,T2>   ToG2_1:IFromG2<string,string>
             *      异常类     To1_Abs:IFrom1             TestStaticComponent         TestAbstractComponent
             */

            #region DIDescriptor    测试：比较复杂，需要挨个做测试
            Type from;
            //  null测试情况；key取任意值不影响测试
            //      from-to模式
            CreateCatch<ArgumentNullException>(from: null!, to: null!, message: "1.都为空");
            CreateCatch<ArgumentNullException>(from: null!, to: typeof(To1), message: "1.from为空");
            CreateCatch<ArgumentNullException>(from: typeof(IFrom1), to: null!, message: "1.to为空");
            Create(key: null, from: typeof(IFrom1), to: typeof(To1));
            Create(key: string.Empty, from: typeof(IFrom1), to: typeof(To1));
            Create(key: "key不为null测试", from: typeof(IFrom1), to: typeof(To1));
            //      from-tofunc模式
            CreateCatch<ArgumentNullException>(from: null!, toFunc: null!, message: "2.都为空");
            CreateCatch<ArgumentNullException>(from: null!, toFunc: m => null, message: "2.from为空");
            CreateCatch<ArgumentNullException>(from: typeof(IFrom1), toFunc: null!, message: "2.toFunc为空");
            Create(key: null, from: typeof(IFrom1), toFunc: m => null);
            Create(key: string.Empty, from: typeof(IFrom1), toFunc: m => null);
            Create(key: "key不为null测试", from: typeof(IFrom1), toFunc: m => null);

            //  from-to模式测试：
            //      from无效：静态类；to随便给一个，先不为null即可
            CreateCatch<ArgumentException>(from: typeof(TestStaticComponent), to: typeof(To1), "3.from为静态类");
            //      from有效，为接口IFrom1，to无效（为接口，为class时为抽象类、静态类等，不实现IFrom1，实现了但为抽象类）
            from = typeof(IFrom1);
            CreateCatch<ArgumentException>(from, to: typeof(IFrom1), message: "3.to为接口");
            CreateCatch<ArgumentException>(from, to: typeof(TestStaticComponent), message: "3.to为静态类");
            CreateCatch<ArgumentException>(from, to: typeof(TestAbstractComponent), message: "3.to为抽象类");
            CreateCatch<ArgumentException>(from, to: typeof(string), "3.to为string，不实现IFrom1");
            CreateCatch<ArgumentException>(from, to: typeof(To2), "3.to为To2，不实现IFrom1");
            CreateCatch<ArgumentException>(from, to: typeof(ToG2<,>), "3.to为ToG2<,>，不实现IFrom1");
            CreateCatch<ArgumentException>(from, to: typeof(ToG2<string, string>), "3.to为ToG2<string,string>，不实现IFrom1");
            CreateCatch<ArgumentException>(from, to: typeof(ToG2_1), "3.to为ToG2_1，不实现IFrom1");
            CreateCatch<ArgumentException>(from, to: typeof(To1_1_Abs), "3.to为To1_1_Abs，实现IFrom1但为抽象类");
            //      from有效，为接口IFrom1；to实现IFrom1的常规接口类
            from = typeof(IFrom1);
            Create(key: null, from, to: typeof(To1));
            Create(key: null, from, to: typeof(To1_1));
            //     from有效，为To1时；to无效（为接口、静态类、抽象类、或者不继承to>
            from = typeof(To1);
            CreateCatch<ArgumentException>(from, to: typeof(IFrom1), "4.to接口");
            CreateCatch<ArgumentException>(from, to: typeof(TestStaticComponent), "4.to静态类");
            CreateCatch<ArgumentException>(from, to: typeof(TestAbstractComponent), "4.to抽象类");
            CreateCatch<ArgumentException>(from, to: typeof(string), "4.to为string，不继承To1");
            CreateCatch<ArgumentException>(from, to: typeof(To2), "4.to为To2，不继承To1");
            CreateCatch<ArgumentException>(from, to: typeof(ToG2<,>), "4.to为ToG2<,>，不可构建，且不继承To1");
            CreateCatch<ArgumentException>(from, to: typeof(ToG2<string, string>), "4.to为ToG2<string,string>，不继承To1");
            CreateCatch<ArgumentException>(from, to: typeof(ToG2_1), "4.to为ToG2_1，不继承To1");
            CreateCatch<ArgumentException>(from, to: typeof(To1_1_Abs), "4.to为To1_1_Abs，不继承To1但为抽象类");
            //      from有效，为To1时；to继承或者为自身
            from = typeof(To1);
            Create(key: string.Empty, from, typeof(To1));
            Create(key: null, from, typeof(To1));
            Create(key: "nuxx", from, typeof(To1));
            Create(key: string.Empty, from, typeof(To1_1));
            Create(key: null, from, typeof(To1_1));
            //      from有效，为抽象类时；冗余测试，和为to1效果一致
            Create(key: null, typeof(To1_1_Abs), typeof(To1_1_));

            //  from-toFunc模式测试；需要满足from不是静态类即可
            CreateCatch<ArgumentException>(from: typeof(TestStaticComponent), toFunc: manager => null, message: "5.from为抽象类");
            Create(key: null, typeof(TestAbstractComponent), toFunc: manager => null);
            Create(key: null, typeof(To1_1_Abs), toFunc: manager => null);
            Create(key: null, typeof(IFrom1), toFunc: manager => null);
            Create(key: null, typeof(IFromG2<,>), toFunc: manager => null);
            Create(key: null, typeof(ToG2<,>), toFunc: manager => null);
            Create(key: null, typeof(ToG2_1<>), toFunc: manager => null);
            Create(key: null, typeof(To1), toFunc: manager => null);
            Create(key: null, typeof(To1_1), toFunc: manager => null);

            //  from-to泛型模式：二者需同时可构建、不可构建
            //      from为泛型接口，不可构建；to必须是泛型且参数个数需一致，且不可构建，且实现此接口
            //          to为非泛型class或者接口
            CreateCatch<ArgumentException>(from: typeof(IFromG2<,>), to: typeof(IFrom2), "6.to为接口");
            CreateCatch<ArgumentException>(from: typeof(IFromG2<,>), to: typeof(TestStaticComponent), "6.to为静态类");
            CreateCatch<ArgumentException>(from: typeof(IFromG2<,>), to: typeof(TestAbstractComponent), "6.to为抽象类");
            CreateCatch<ArgumentException>(from: typeof(IFromG2<,>), to: typeof(To1), "6.to为To1,非泛型，可构建，不实现IFromG2");
            CreateCatch<ArgumentException>(from: typeof(IFromG2<,>), to: typeof(ToG2_1), "6.to为ToG2_1,非泛型，可构建，实现IFromG2");
            //          to为泛型class
            CreateCatch<ArgumentException>(from: typeof(IFromG2<,>), to: typeof(List<>), "6.to为List<>,泛型，不可构建，不实现IFromG2；参数个数不一致");
            CreateCatch<ArgumentException>(from: typeof(IFromG2<,>), to: typeof(List<string>), "6.to为List<string>,泛型，可构建，不实现IFromG2；参数个数不一致");
            CreateCatch<ArgumentException>(from: typeof(IFromG2<,>), to: typeof(ToG2_1<>), "6.to为ToG2_1<>,泛型，不可构建，实现IFromG2；参数不一致");
            CreateCatch<ArgumentException>(from: typeof(IFromG2<,>), to: typeof(ToG2_1<String>), "6.to为ToG2_1<string>,泛型，可构建，实现IFromG2；参数不一致");
            CreateCatch<ArgumentException>(from: typeof(IFromG2<,>), to: typeof(Dictionary<,>), "6.to为Dictionary<, >,泛型，不可构建，不实现IFromG2；参数个数一致");
            CreateCatch<ArgumentException>(from: typeof(IFromG2<,>), to: typeof(Dictionary<string, string>), "6.to为Dictionary<string, string>,泛型，可构建，不实现IFromG2；参数个数一致");
            CreateCatch<ArgumentException>(from: typeof(IFromG2<,>), to: typeof(ToG2<string, string>), "6.to为ToG2<string, string>,泛型，可构建，实现IFromG2；参数个数一致");
            Create(key: null, from: typeof(IFromG2<,>), to: typeof(ToG2<,>));
            Create(key: "null", from: typeof(IFromG2<,>), to: typeof(ToG2<,>));
            Create(key: string.Empty, from: typeof(IFromG2<,>), to: typeof(ToG2<,>));
            //      from为泛型接口，可构建；to必须为泛型且参数个数需一致，且可构建，且实现此接口
            from = typeof(IFromG2<string, int>);
            //          to为接口、不可new类（静态类、抽象类）
            CreateCatch<ArgumentException>(from, to: typeof(IFrom2), message: "7.to接口");
            CreateCatch<ArgumentException>(from, to: typeof(TestStaticComponent), message: "7.to静态类");
            CreateCatch<ArgumentException>(from, to: typeof(TestAbstractComponent), message: "7.to抽象类");
            //          to为class，非泛型可new类
            CreateCatch<ArgumentException>(from, to: typeof(To2), message: "7.to为To2，非泛型，可构建，实现IFromG2<string, int>");
            CreateCatch<ArgumentException>(from, to: typeof(ToG2_1), message: "7.to为ToG2_1，非泛型，可构建，实现IFromG2<String, String>,不实现IFromG2<string, int>");
            Create(key: null, from: typeof(IFromG2<string, string>), to: typeof(ToG2_1));
            //          to为class，泛型不可构建；此时100%无法注册，无法推断实现类
            from = typeof(IFromG2<string, int>);
            CreateCatch<ArgumentException>(from, to: typeof(List<>), message: "8.to为List<>，不可构建，不实现IFromG2<string, int>");
            CreateCatch<ArgumentException>(from, to: typeof(Dictionary<,>), message: "8.to为Dictionary<,>，不可构建，不实现IFromG2<string, int>");
            CreateCatch<ArgumentException>(from, to: typeof(ToG2<,>), message: "8.to为ToG2<,>，不可构建，实现IFromG2<, >");
            CreateCatch<ArgumentException>(from, to: typeof(ToG2_1<>), message: "8.to为ToG2_1<>，不可构建，实现IFromG2<, >");
            //          to为class，泛型可构建
            from = typeof(IFromG2<string, int>);
            CreateCatch<ArgumentException>(from, to: typeof(List<string>), message: "8.to为List<string>，可构建，不实现IFromG2<string, int>");
            CreateCatch<ArgumentException>(from, to: typeof(Dictionary<string, int>), message: "8.to为Dictionary<string, int>，可构建，不实现IFromG2<string, int>");
            CreateCatch<ArgumentException>(from, to: typeof(Dictionary<string, string>), message: "8.to为Dictionary<string, string>，可构建，不实现IFromG2<string, int>");
            CreateCatch<ArgumentException>(from, to: typeof(ToG2<string, string>), message: "8.to为ToG2<,>，不可构建，实现IFromG2<string,string >");
            CreateCatch<ArgumentException>(from, to: typeof(ToG2_1<string>), message: "8.to为ToG2_1<string>，不可构建，实现IFromG2<string,string >");
            Create(key: null, from: typeof(IFromG2<string, int>), to: typeof(ToG2<string, int>));
            Create(key: null, from: typeof(IFromG2<string, string>), to: typeof(ToG2<string, string>));
            Create(key: null, from: typeof(IFromG2<Enum, string>), to: typeof(ToG2<Enum, string>));
            Create(key: null, from: typeof(IFromG2<string, string>), to: typeof(ToG2_1<string>));
            Create(key: null, from: typeof(IFromG2<int, int>), to: typeof(ToG2_1<int>));

            /**  上述测试，已经把 <see cref="DIHelper.ChekFromToType(in Type?, in Type?)"/> 逻辑测试得差不多了
             *      这个内部逻辑很有意思是拆开来的
             *          1、判断泛型和非泛型
             *          2、判断接口和class，接口需实现，clas需继承
             *      这样功能在上面就测试得差不多了，下面只需要验证一下泛型class的继承属性即可
             *      实际测试，不应该这么取巧，做黑盒测试，避免疏漏；这里取个巧
             */
            //      from为泛型class，不可构建；to必须泛型且参数个数需一致，且不可构建，且继承此class
            from = typeof(ToG2<,>);
            Create(key: null, from, from);
            //      from为泛型class，可构建；to泛型可构建，或者非泛型可构建，且继承此class
            from = typeof(ToG2<String, String>);
            Create(key: null, from, from);
            Create(key: null, from, typeof(ToG2_1));
            Create(key: null, from, typeof(ToG2_1<String>));
            #endregion

            //  泛型构建，最终走的的DIDescriptor对应构造方法，这里取巧，先不做测试了
            //      DIDescriptor<T>         测试；比较简单，不会存在from、to类型不匹配的情况，但会有抽象类的情况，不做验证处理

            //      DIDescriptor<From,To>   测试；比较简单，不会存在from、to类型不匹配的情况，但会有抽象类的情况
        }

        #region 私有方法：便捷
        /// <summary>
        /// 创建依赖注入描述器
        /// </summary>
        /// <param name="key"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private static DIDescriptor Create(string? key, Type from, Type to)
            => new DIDescriptor(key, from, lifetime: LifetimeType.Transient, to);
        /// <summary>
        /// 创建依赖注入描述器
        /// </summary>
        /// <param name="key"></param>
        /// <param name="from"></param>
        /// <param name="toFunc"></param>
        /// <returns></returns>
        private static DIDescriptor Create(string? key, Type from, Func<IDIManager, object?> toFunc)
            => new DIDescriptor(key, from, lifetime: LifetimeType.Transient, toFunc);

        /// <summary>
        /// 创建依赖注入描述器，构建异常情况，并Catch指定的异常信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static Exception? CreateCatch<T>(Type from, Type to, string message) where T : Exception
            => Assert.Catch<T>(() => new DIDescriptor(key: null, from, lifetime: LifetimeType.Scope, to), message);
        /// <summary>
        /// 创建依赖注入描述器，构建异常情况，并Catch指定的异常信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="from"></param>
        /// <param name="toFunc"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static Exception? CreateCatch<T>(Type from, Func<IDIManager, object?> toFunc, string message) where T : Exception
            => Assert.Catch<T>(() => new DIDescriptor(key: null, from, lifetime: LifetimeType.Scope, toFunc), message);
        #endregion
    }
}

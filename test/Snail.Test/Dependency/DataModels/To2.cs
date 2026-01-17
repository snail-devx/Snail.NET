using Snail.Test.Dependency.Interfaces;

namespace Snail.Test.Dependency.DataModels
{
    /// <summary>
    /// 实体，实现<see cref="IFrom2"/>
    /// </summary>
    public class To2 : IFrom2
    {
        /// <summary>
        /// 属性注入测试
        /// </summary>
        [Inject]
        public IFrom1? IF1_P { set; get; }
        /// <summary>
        /// 属性注入测试
        /// </summary>
        [Inject]
        public IFrom1? IF1_PR { init; get; }
        /// <summary>
        /// 属性注入测试
        /// </summary>
        [Inject]
        public IFrom1? IF1_PRP { private init; get; }
        [Inject]
        public IFrom1? IF1_PRP2 { private set; get; }

        /// <summary>
        /// 字段注入测试
        /// </summary>
        [Inject]
        public IFrom1? IF1_F;
        /// <summary>
        /// 字段注入测试
        /// </summary>
        [Inject]
        private IFrom1? IF1_FP;
        /// <summary>
        /// 字段注入测试
        /// </summary>
        [Inject]
        public readonly IFrom1? IF1_FR;

        /// <summary>
        /// 
        /// </summary>
        [Inject(Key = "key")]
        public IFrom1? IF1_KF;
        /// <summary>
        /// 
        /// </summary>
        [Inject(Key = "key2")]
        public IFrom1? IF1_K2F;

        /// <summary>
        /// 是否是【方法注入】
        /// </summary>
        public string? IsMethodInject { private set; get; }
        /// <summary>
        /// 是否是【方法注入】
        /// </summary>
        public string? IsMethodInject2 { private set; get; }

        /// <summary>
        /// 是否是【构造方法】注入
        /// </summary>
        public string? IsConstructorInject { private set; get; }
        /// <summary>
        /// 是构造方法注入的参数
        /// </summary>
        public string? IsConstructorParam { private set; get; }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IFrom1? GetIF1_FPValue() => IF1_FP;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="if1"></param>
        /// <param name="if2"></param>
        public To2([Inject] IFrom1 if1, [Inject(Key = "key")] IFrom1 if2)
        {
            IF1_PR = if2;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="if1"></param>
        /// <param name="if11"></param>
        /// <param name="x"></param>
        [Inject]
        public To2(IFrom1 if1, [Parameter<IFrom1>(Key = "key")] IFrom1 if11, string x)
        {
            IsConstructorInject = "InjectConstruct";
            IsConstructorParam = x;
        }

        [Inject]
        private void InjectMethod()
        {
            IsMethodInject = "InjectMethod";
        }

        [Inject]
        public void InjectMethod2()
        {
            IsMethodInject2 = "InjectMethod2";
        }
    }
}

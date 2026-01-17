using Snail.Test.Aspect.Components;

namespace Snail.Test.Aspect
{
    /// <summary>
    /// 通用切面编程的源码生成
    /// </summary>
    class GeneralSourceGeneratorTest
    {
        [Test]
        public async Task Test()
        {
            IApplication app = new Application();
            app.Run();


            GeneralAspectTest aspect = app.ResolveRequired<GeneralAspectTest>();
            aspect.TesVoid();
            await aspect.TestTask("1");
            Assert.That("TestString" == aspect.TestString());
            Assert.That("修改返回值" == await aspect.TestTaskString());
        }
    }
}

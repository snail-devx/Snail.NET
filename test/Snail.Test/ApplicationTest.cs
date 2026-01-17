namespace Snail.Test
{
    /// <summary>
    /// 应用程序测试
    /// </summary>
    public sealed class ApplicationTest
    {
        [Test]
        public void TestApp()
        {
            IApplication app = new Application();
            app.OnScan += (services, type, attrs) =>
            {

            };
            app.Run();
            //string str = string.Join(':', [null, "1", null]);

            Testx(out _);
        }

        public void Testx(out string app)
        {
            app = "";
        }
    }
}

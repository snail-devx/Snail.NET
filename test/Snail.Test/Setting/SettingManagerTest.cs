using Snail.Abstractions.Setting;
using Snail.Abstractions.Setting.Extensions;
using Snail.Setting;
using Snail.Utilities.IO.Utils;

namespace Snail.Test.Setting
{
    /// <summary>
    /// 应用程序配置管理器测试
    /// </summary>
    public sealed class SettingManagerTest
    {
        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void TestSettingManager()
        {
            //  基础测试
            ISettingManager setting = new SettingManager();
            TestSetting(setting, setting.Run);
            //  对接到app上的测试
            IApplication app = new Application();
            TestSetting(app.Setting, app.Run);
        }

        /// <summary>
        /// 测试环境变量
        /// </summary>
        [Test]
        public void TestEnvVars()
        {
            ISettingManager setting = new SettingManager();
            setting.Run();

            Assert.That(setting.AnalysisVars("") == "");
            Assert.That(setting.AnalysisVars(null!) == null);

            //  无表达式，或者格式不对
            Assert.That(setting.AnalysisVars("1111") == "1111");
            //      无效表达式，边界条件不够，后续再补充
            Assert.That(setting.AnalysisVars("{") == "{");
            Assert.That(setting.AnalysisVars("${") == "${");
            Assert.That(setting.AnalysisVars("\\{") == "\\{");
            Assert.That(setting.AnalysisVars("}") == "}");
            Assert.That(setting.AnalysisVars("\\}") == "\\}");
            Assert.That(setting.AnalysisVars("1{11") == "1{11");
            Assert.That(setting.AnalysisVars("1${11") == "1${11");
            Assert.That(setting.AnalysisVars("1\\{11") == "1\\{11");
            Assert.That(setting.AnalysisVars("111}") == "111}");
            Assert.That(setting.AnalysisVars("1\\}11") == "1\\}11");
            Assert.That(setting.AnalysisVars("1{11\\}") == "1{11\\}");
            Assert.That(setting.AnalysisVars("1\\{11}") == "1\\{11}");
            //  正常表达式
            Assert.That(setting.AnalysisVars("${Env_1}") == "Env_1");
            Assert.That(setting.AnalysisVars("${Env_1}}") == "Env_1}");
            Assert.That(setting.AnalysisVars("${Env_2}") == "Env_2_File");
            Assert.That(setting.AnalysisVars("${Env_4}") == "Env_4_File2");
            Assert.That(setting.AnalysisVars("${Env_4}") == "Env_4_File2");
            Assert.That(setting.AnalysisVars("111 ${Env_1} xxx") == "111 Env_1 xxx");
            Assert.That(setting.AnalysisVars("Env_1${Env_1}Env_1") == "Env_1Env_1Env_1");
            Assert.That(setting.AnalysisVars("${Env_1}${Env_2}") == "Env_1Env_2_File");
            Assert.That(setting.AnalysisVars("${Env_1}${Env_2}${Env_1}") == "Env_1Env_2_FileEnv_1");

            //  测试变量名称不存在，注意大小写敏感
            Assert.Catch<ApplicationException>(() => setting.AnalysisVars("${Env_41}"), "变量[{Env_41}]无法从环境变量中查询到具体值。环境变量：${Env_41}");

        }

        #region 私有方法
        /// <summary>
        /// 测试配置
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="run"></param>
        private static void TestSetting(ISettingManager setting, Action run)
        {
            //  use资源监听测试
            bool testWorkspace = false, testProject = false;
            setting.Use(isProject: false, rsCode: "test-work", (workspace, project, code, type, content) =>
            {
                if (testWorkspace == true)
                {
                    throw new ApplicationException("test-work配置回调了多次，需注意");
                }
                if (workspace == "Test" && project == null && code == "test-work")
                {
                    testWorkspace = true;
                    FileHelper.ThrowIfNotFound(content);
                }
            });
            setting.Use(isProject: true, rsCode: "test-project", (workspace, project, code, type, content) =>
            {
                if (testProject == true)
                {
                    throw new ApplicationException("test-project配置回调了多次，需注意");
                }
                if (workspace == "Test" && project == "Project" && code == "test-project")
                {
                    testProject = true;
                    FileHelper.ThrowIfNotFound(content);
                }
            });
            //  运行配置管理器，进行测试
            Assert.That(testWorkspace == false, "读取工作空间配置：test-work；未运行前为false");
            Assert.That(testProject == false, "读取项目项目下配置：test-Project；未运行前为false");
            run.Invoke();
            Assert.That(testWorkspace, "读取工作空间配置：test-work");
            Assert.That(testProject, "读取项目项目下配置：test-Project");
            //      环境变量测试
            Assert.That(setting.GetEnv("Env_1") == "Env_1", "【内置】环境变量");
            Assert.That(setting.GetEnv("Env_01") == string.Empty, "【内置】环境变量；同一个envs下取最新的");
            Assert.That(setting.GetEnv("Env_2") == "Env_2_File", "【内置】环境变量；被【文件】环境变量配置覆盖");
            Assert.That(setting.GetEnv("Env_3") == "Env_3_Mult", "【内置】环境变量；被内置第二个evns节点覆盖");
            Assert.That(setting.GetEnv("Env_4") == "Env_4_File2", "【内置】环境变量；被【文件】环境变量第二个section配置覆盖");
            Assert.That(setting.GetEnv("Env_5") == "Env_5_File", "【文件】环境变量，同一个section下重复环境变量，取最新的一个");
            Assert.That(setting.GetEnv("Env_10") == "Env_10_File", "【文件】环境变量；第二个section中定义");
            Assert.That(setting.GetEnv("Env_x") == "Env_x", "【内置】环境变量；第二个envs中定义");
            Assert.That(setting.GetEnv("Env_NotFound") == null, "无此环境变量备注");
            Assert.That(setting.GetEnv("") == null, "无此环境变量备注");
            Assert.That(setting.GetEnv(string.Empty) == null, "无此环境变量备注");
            Assert.Catch<ArgumentNullException>(() => setting.GetEnv(null!), "环境变量名称不能为空");
        }
        #endregion
    }
}

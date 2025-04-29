using System.Runtime.CompilerServices;
using Snail.Abstractions.Setting;

namespace Snail.Setting
{
    /// <summary>
    /// 应用程序配置工厂
    /// </summary>
    public static class SettingFactory
    {
        #region 属性变量
        /// <summary>
        /// 管理器 构建器委托
        /// </summary>
        private static Func<ISettingManager>? _managerCreator;
        #endregion

        #region 公共方法
        /// <summary>
        /// 配置 构建器
        /// </summary>
        /// <param name="creator"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Config(in Func<ISettingManager> creator)
        {
            ThrowIfNull(creator);
            _managerCreator = creator;
        }

        /// <summary>
        /// 创建一个配置管理器
        /// </summary>
        /// <returns></returns>
        public static ISettingManager Create()
        {
            ThrowIfNull(_managerCreator, "构建器未配置，无法创建配置管理器");
            return _managerCreator!.Invoke();
        }
        #endregion
    }
}

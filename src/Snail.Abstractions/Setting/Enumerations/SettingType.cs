namespace Snail.Abstractions.Setting.Enumerations
{
    /// <summary>
    /// 配置类型；用于支持多种不同配置来源。如本地文件，数据库读取xml等等
    /// </summary>
    public enum SettingType
    {
        /// <summary>
        /// 本地配置文件
        /// </summary>
        File = 0,

        /// <summary>
        /// XML字符串
        /// </summary>
        Xml = 10,
    }
}

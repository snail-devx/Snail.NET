using System.Xml;
using Snail.Utilities.IO.Utils;

namespace Snail.Utilities.Xml.Utils
{
    /// <summary>
    /// Xml操作助手类
    /// </summary>
    public sealed class XmlHelper
    {
        #region 公共方法
        /// <summary>
        /// 加载xml文件
        /// </summary>
        /// <param name="file"></param>
        /// <exception cref="ArgumentNullException">file</exception>
        /// <exception cref="FileNotFoundException">file</exception>
        /// <exception cref="IOException">file</exception>
        /// <returns></returns>
        public static XmlDocument Load(string file)
        {
            FileHelper.ThrowIfNotFound(file);
            XmlDocument doc = new XmlDocument();
            Exception? ex = Run(doc.Load, file);
            if (ex != null)
            {
                string msg = $"加载Xml文件失败。file：{file}";
                throw new IOException(msg, ex);
            }
            return doc;
        }
        #endregion
    }
}

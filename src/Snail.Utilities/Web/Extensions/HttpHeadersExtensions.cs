using System.Net.Http.Headers;

namespace Snail.Utilities.Web.Extensions;
/// <summary>
/// <see cref="HttpHeaders"/>扩展方法
/// </summary>
public static class HttpHeadersExtensions
{
    extension<T>(T header) where T : HttpHeaders
    {
        /// <summary>
        /// 设置Header；存在覆盖，不存在添加
        /// <para>1、先移除key为<paramref name="name"/>的所有header数据，再添加</para>
        /// </summary>
        /// <param name="name">名称；确保非空，否则报错</param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException" />
        /// <returns></returns>
        public T Set(string name, string value)
        {
            header.Remove(name);
            header.Add(name, value);
            return header;
        }

        /// <summary>
        /// 尝试设置Header；存在覆盖，不存在添加
        /// </summary>
        /// <param name="condition">条件为true时，设置Header数据</param>
        /// <param name="name">名称；确保非空，否则报错</param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException" />
        /// <returns></returns>
        public T TrySet(bool condition, string name, string value)
        {
            if (condition == true)
            {
                header.Set(name, value);
            }
            return header;
        }
        /// <summary>
        /// 尝试添加Header
        /// </summary>
        /// <param name="condition">条件为true时，添加Header数据</param>
        /// <param name="name">名称；确保非空，否则报错</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public T TryAdd(bool condition, string name, string value)
        {
            if (condition == true)
            {
                header.Add(name, value);
            }
            return header;
        }
    }
}
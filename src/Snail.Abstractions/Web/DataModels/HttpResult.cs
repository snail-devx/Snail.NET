using Snail.Utilities.Common.Extensions;

namespace Snail.Abstractions.Web.DataModels;

/// <summary>
/// Http请求结果
/// <para>1、对Http请求结果包装；提供常用数据类型快捷访问属性，如转成String、Bytes、、、 </para>
/// <para>2、封装JSON反序列化方法，As《T》 </para>
/// </summary>
public sealed class HttpResult
{
    #region 属性变量
    /// <summary>
    /// 结果内容实体：暂时不对外提供访问
    /// </summary>
    public HttpContent Content { private init; get; }
    #endregion

    #region 快捷访问：后期考虑做缓存，重复访问时直接返回
    /// <summary>
    /// String结果值
    /// </summary>
    public string AsString => Content.ReadAsStringAsync().Result;
    /// <summary>
    /// 异步：String结果值
    /// </summary>
    public Task<string> AsStringAsync => Content.ReadAsStringAsync();

    /// <summary>
    /// Boolean结果值
    /// </summary>
    public bool AsBoolean => Convert.ToBoolean(Content.ReadAsStringAsync().Result);

    /// <summary>
    /// Int16结果值
    /// </summary>
    public short AsInt16 => Convert.ToInt16(Content.ReadAsStringAsync().Result);
    /// <summary>
    /// UInt16结果值
    /// </summary>
    public ushort AsUInt16 => Convert.ToUInt16(Content.ReadAsStringAsync().Result);
    /// <summary>
    /// Int32结果值
    /// </summary>
    public int AsInt32 => Convert.ToInt32(Content.ReadAsStringAsync().Result);
    /// <summary>
    /// UInt32结果值
    /// </summary>
    public uint AsUInt32 => Convert.ToUInt32(Content.ReadAsStringAsync().Result);
    /// <summary>
    /// Int64结果值
    /// </summary>
    public long AsInt64 => Convert.ToInt64(Content.ReadAsStringAsync().Result);
    /// <summary>
    /// UInt64结果值
    /// </summary>
    public ulong AsUInt64 => Convert.ToUInt64(Content.ReadAsStringAsync().Result);

    /// <summary>
    /// Single结果值
    /// </summary>
    public float AsSingle => Convert.ToSingle(Content.ReadAsStringAsync().Result);
    /// <summary>
    /// Double结果值
    /// </summary>
    public double AsDouble => Convert.ToDouble(Content.ReadAsStringAsync().Result);
    /// <summary>
    /// Decimal结果值
    /// </summary>
    public decimal AsDecimal => Convert.ToDecimal(Content.ReadAsStringAsync().Result);

    /// <summary>
    /// 字节数组结果值
    /// </summary>
    public byte[] AsBytes => Content.ReadAsByteArrayAsync().Result;
    /// <summary>
    /// 异步：字节数组结果值
    /// </summary>
    public Task<byte[]> AsBytesAsync => Content.ReadAsByteArrayAsync();

    /// <summary>
    /// 异步：流结果值
    /// </summary>
    public Stream AsStream => Content.ReadAsStream();
    /// <summary>
    /// 异步：流结果值
    /// </summary>
    public Task<Stream> AsStreamAsync => Content.ReadAsStreamAsync();
    #endregion

    #region 构造方法
    /// <summary>
    /// 基于响应结果构建描述器
    /// </summary>
    /// <param name="response">响应结果对象；若状态码非200，报错</param>
    public HttpResult(HttpResponseMessage response)
    {
        ThrowIfNull(response);
        //  如果不是success，则把具体的错误输出出来，避免报错时内容不清晰
        //response.EnsureSuccessStatusCode();
        if (response.IsSuccessStatusCode == false)
        {
            string msg = response.Content.ReadAsStringAsync().Result;
            throw new HttpRequestException(msg, inner: null, statusCode: response.StatusCode);
        }
        Content = response.Content;
    }
    /// <summary>
    /// 基于HttpContent实例构建描述器
    /// </summary>
    /// <param name="content"></param>
    public HttpResult(HttpContent content)
    {
        ThrowIfNull(content);
        Content = content;
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 同步：转换为指定类型实例
    /// </summary>
    /// <remarks>内部调用的是JSON反序列化逻辑，若反序列化失败会报错</remarks>
    /// <typeparam name="T"></typeparam>
    /// <returns>对象实例</returns>
    public T As<T>()
    {
        string str = Content.ReadAsStringAsync().Result;
        ThrowIfNull(str);
        return str.As<T>();
    }
    /// <summary>
    /// 异步：转换为指定类型实例
    /// </summary>
    /// <remarks>内部调用的是JSON反序列化逻辑，若反序列化失败会报错</remarks>
    /// <typeparam name="T"></typeparam>
    /// <returns>对象实例</returns>
    public async Task<T> AsAsync<T>()
    {
        string str = await Content.ReadAsStringAsync();
        ThrowIfNull(str);
        return str.As<T>();
    }
    #endregion
}

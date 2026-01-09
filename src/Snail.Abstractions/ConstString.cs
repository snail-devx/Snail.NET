namespace Snail.Abstractions;

/// <summary>
/// 常量字符串类，负责归并程序集中常用的一些字符串值
/// </summary>
public static class ConstString
{
    #region 常用字符串值
    /// <summary>
    /// 字符串：null
    /// </summary>
    public const string STR_Null = "null";
    /// <summary>
    /// 字符串：true
    /// </summary>
    public const string STR_True = "True";
    /// <summary>
    /// 字符串：false
    /// </summary>
    public const string STR_False = "False";
    /// <summary>
    /// 字符串：分隔符，用于切割和拼接字符串
    /// </summary>
    public const string STR_Separator = ":";
    #endregion

    #region 环境变量
    /// <summary>
    /// 环境变量：应用名称
    /// </summary>
    public const string ENV_AppName = "AppName";
    /// <summary>
    /// 环境变量：运行环境类型
    /// </summary>
    public const string ENV_RunType = "RunType";
    /// <summary>
    /// 环境变量：数据中心ID
    /// </summary>
    public const string Env_DatacenterId = "DatacenterId";
    /// <summary>
    /// 环境变量：工作节点ID
    /// </summary>
    public const string Env_WorkerId = "WorkerId";
    #endregion

    #region 中间件名称
    /// <summary>
    /// 中间件名称：遥测追踪
    /// </summary>
    public const string MIDDLEWARE_Telemetry = "Telemetry";
    /// <summary>
    /// 中间件名称：运行时上下文
    /// </summary>
    public const string MIDDLEWARE_RunContext = "RunContext";
    #endregion

    #region 上下文和特定Key值
    /// <summary>
    /// 上下文Key：trace_id
    /// </summary>
    public const string CONTEXT_TraceId = "X-Trace-Id";
    /// <summary>
    /// 上下文Key：parent_span_id
    /// </summary>
    public const string CONTEXT_ParentSpanId = "X-Parent-Span-ID";

    /// <summary>
    /// Key：是否已记录数据
    /// <para>1、接收数据时，有此标记，说明发送方已经记录了请求数据了，接收后不用重复记录</para>
    /// <para>2、发送请求时，若已经记录了发送数据，则上下文加上此标记，避免接收方重复记录</para>
    /// <para>在http请求的发送时，加上</para>
    /// </summary>
    public const string KEY_RecordData = "X-Record-Data";
    #endregion

    #region 依赖注入Key值
    /// <summary>
    /// 依赖注入Key：Guid主键策略
    /// </summary>
    public const string DIKEY_Guid = "Guid";
    /// <summary>
    /// 依赖注入Key：雪花算法主键策略
    /// </summary>
    public const string DIKEY_SnowFlake = "SnowFlake";

    /// <summary>
    /// 依赖注入Key：文件日志记录器
    /// </summary>
    public const string DIKEY_FileLogger = "FileLogger";

    /// <summary>
    /// 依赖注入Key：Redis组件，如缓存、分布式锁
    /// </summary>
    public const string DIKEY_Redis = "Redis";
    /// <summary>
    /// 依赖注入Key：RabbitMQ
    /// </summary>
    public const string DIKEY_RabbitMQ = "RabbitMQ";
    #endregion

    #region Culture相关值
    /// <summary>
    /// 语言环境：默认
    /// </summary>
    public const string CULTURE_Default = "zh-CN";
    /// <summary>
    /// 语言环境：中文
    /// </summary>
    public const string CULTURE_zhCN = "zh-CN";
    /// <summary>
    /// 语言环境：英语
    /// </summary>
    public const string CULTURE_enUS = "en-US";
    #endregion

    #region 反射相关
    /// <summary>
    /// 实例反射绑定标记：Instance+Public
    /// </summary>
    public const BindingFlags BINDINGFLAGS_InsPublic = BindingFlags.Instance | BindingFlags.Public;
    #endregion
}

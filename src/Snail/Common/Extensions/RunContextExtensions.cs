namespace Snail.Common.Extensions;

/// <summary>
/// <see cref="RunContext"/>扩展方法
/// </summary>
public static class RunContextExtensions
{
    extension(RunContext context)
    {
        #region 扩展属性
        /// <summary>
        /// 当前上下文是否禁用日志
        /// </summary>
        public bool DisableLog
        {
            get => STR_True.IsEqual(context.Get<string>(CONTEXT_DisableLog), ignoreCase: true) == false;
            set => context.Add<string>(CONTEXT_DisableLog, value.ToString());
        }

        /// <summary>
        /// trace_id；追踪Id 
        /// </summary>
        /// <para>1、分布式系统中用于唯一标识一次完整请求调用链路的全局唯一标识符</para>
        /// <para>2、全局唯一、贯穿整条调用链、所有 Span 共享同一个 trace_id</para>
        /// <para>3、若上下文中无此值，则表示为第一个入口请求，此时默认为<see cref="RunContext.ContextId"/>值</para>
        public string TraceId => Default(context.Get<string>(CONTEXT_TraceId), context.ContextId)!;
        /// <summary>
        /// span_id；操作id
        /// <para>1、封装此属性和标准分布式系统的追踪信息对齐，但从<see cref="RunContext.ContextId"/>取值</para>
        /// </summary>
        public string SpanId => context.ContextId;
        /// <summary>
        /// parent_span_id；父级操作Id
        /// <para>1、用于分布式系统进行链路追踪使用</para>
        /// <para>2、涉及到子操作时，在子的运行时上下文上传递</para>
        /// <para>3、涉及到其他系统站点调用过来时，本站点操作挂载到传递过来的父级操作Id下</para>
        /// </summary>
        /// <returns></returns>
        public string? ParentSpanId => context.Get<string>(CONTEXT_ParentSpanId);

        /// <summary>
        /// 初始化分布式追踪信息
        /// </summary>
        /// <param name="traceId">追踪id；为空将忽略</param>
        /// <param name="parentSpanId">父级操作id；为空将忽略</param>
        /// <returns></returns>
        public RunContext InitTelemetry(string? traceId, string? parentSpanId)
        {
            if (IsNullOrEmpty(traceId) == false)
            {
                context.Add<string>(CONTEXT_TraceId, traceId!);
            }
            if (IsNullOrEmpty(parentSpanId) == false)
            {
                context.Add<string>(CONTEXT_ParentSpanId, parentSpanId!);
            }
            return context;
        }
        #endregion
    }
}
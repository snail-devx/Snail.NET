using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace Snail.Common.Extensions
{
    /// <summary>
    /// <see cref="RunContext"/>扩展方法
    /// </summary>
    public static class RunContextExtensions
    {
        #region 属性变量
        #endregion

        #region 公共方法

        #region 其他快捷访问操作
        /// <summary>
        /// 授信权限Id  <br />
        ///     1、涉及到多站点协作时，用于进行多站点之间缓存数据共享等授信操作  <br />
        ///     2、获取失败，则默认使用ContextId填充值  <br />
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string TrustAuthId(this RunContext context)
        {
            string? tmpId = context.Get<string>(CONTEXT_TrustAuthId);
            return string.IsNullOrEmpty(tmpId) ? context.ContextId : tmpId;
        }
        /// <summary>
        /// 父级操作Id  <br />
        ///     1、涉及到子操作时，在子的运行时上下文上传递  <br />
        ///     2、涉及到其他站点调用过来时，本站点操作挂载到传递过来的父级操作Id下  <br />
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string? ParentActionId(this RunContext context)
            => context.Get<string>(CONTEXT_ParentActionId);
        #endregion

        #region 共享钥匙串相关：实现跨站点数据共享
        /// <summary>
        /// 初始化共享钥匙串
        /// </summary>
        /// <param name="context"></param>
        /// <param name="shareKeyChainJson">共享钥匙串JSON数据</param>
        /// <returns></returns>
        public static RunContext InitShareKeyChain(this RunContext context, string? shareKeyChainJson)
        {
            //  反序列化取值
            IDictionary<string, string> map = new Dictionary<string, string>();
            {
                try
                {
                    JObject jObject = string.IsNullOrEmpty(shareKeyChainJson) == true
                        ? new JObject()
                        : JObject.Parse(shareKeyChainJson);
                    foreach (var kv in jObject)
                    {
                        if (string.IsNullOrEmpty(kv.Key) == false && kv.Value != null)
                        {
                            map[kv.Key] = kv.Value.ToString();
                        }
                    }
                }
                catch { }
            }
            //  做一些兼容性工作；实际上不应该放到这里来做的
            //      取得TrustAuthId值，独立加入上下文，并从钥匙串中干掉
            if (map.Remove(CONTEXT_TrustAuthId, out string? tmpVlaue) == true)
            {
                tmpVlaue = Default(tmpVlaue, context.ContextId);
                context.Add(CONTEXT_TrustAuthId, tmpVlaue);
            }
            //      老系统中，会把ParentActionId做成shareKeyChain传递，这里干掉，避免往下流转时出问题
            map.Remove(CONTEXT_ParentActionId);
            //  加入上下文中
            context.Add(CONTEXT_ShareKeyChain, map);

            return context;
        }
        //  先不放开
        //  /// <summary>
        //  /// 初始化共享钥匙串
        //  /// </summary>
        //  /// <param name="context"></param>
        //  /// <param name="map">初始化字典</param>
        //  /// <returns></returns>
        //  public static RunContext InitShareKeyChain(this RunContext context, IDictionary<string, string>? map)
        //  {
        //      if (map?.Count > 0)
        //      {
        //          RunShareKeyChain(context, forceInit: true, dict => dict.Combine(map));
        //      }
        //      return context;
        //  }


        /// <summary>
        /// 添加共享钥匙串 <br />
        ///     1、key、value为空，不加
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key">钥匙串Key</param>
        /// <param name="value">钥匙串Value</param>
        public static RunContext AddShareKeyChain(this RunContext context, string key, string value)
        {
            if (key?.Length > 0 && value?.Length > 0)
            {
                RunShareKeyChain(context, forceInit: true, dict => dict[key] = value);
            }
            return context;
        }

        /// <summary>
        /// 获取共享钥匙串值
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key">钥匙串Key</param>
        /// <returns>key为空，返回null；否则返回具体值</returns>
        public static string? GetShareKeyChain(this RunContext context, string key)
        {
            //  取数据返回
            string? tmpValue = null;
            if (key?.Length > 0)
            {
                RunShareKeyChain(context, forceInit: false, dict => dict?.TryGetValue(key, out tmpValue));
            }
            return tmpValue;
        }
        /// <summary>
        /// 获取共享钥匙串存储信息字典
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static IDictionary<string, string>? GetShareKeyChain(this RunContext context)
        {
            IDictionary<string, string>? keyChain = RunShareKeyChain(context, forceInit: false);
            //  重新转成新字典，避免外部操作
            IDictionary<string, string> map = keyChain?.ToDictionary(kv => kv.Key, kv => kv.Value)
                ?? new Dictionary<string, string>();
            //  强制加上授信Id
            map[CONTEXT_TrustAuthId] = context.TrustAuthId();

            return map;
        }

        /// <summary>
        /// 移除指定的共享钥匙串
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key">钥匙串Key</param>
        /// <returns>返回移除的value值</returns>
        public static string? RemoveShareKeyChain(this RunContext context, string key)
        {
            string? tmpValue = null;
            if (key?.Length > 0)
            {
                RunShareKeyChain(context, forceInit: false, dict => dict.Remove(key, out tmpValue));
            }
            return tmpValue;
        }
        #endregion

        #endregion

        #region 私有方法
        /// <summary>
        /// 运行共享钥匙串相关操作
        /// </summary>
        /// <param name="context"></param>
        /// <param name="forceInit">为null时是否强制初始化共享钥匙串字典</param>
        /// <param name="action">取到的钥匙串字典例外处理；取到的字典非null时触发</param>
        /// <returns>共享钥匙串字典</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private static IDictionary<string, string>? RunShareKeyChain(RunContext context, bool forceInit, Action<IDictionary<string, string>>? action = null)
        {
            var keyChain = context.Get<Dictionary<string, string>>(CONTEXT_ShareKeyChain);
            if (keyChain == null && forceInit == true)
            {
                keyChain = new Dictionary<string, string>();
                context.Add(CONTEXT_ShareKeyChain, keyChain);
                action?.Invoke(keyChain!);
            }
            return keyChain;
        }
        #endregion
    }
}

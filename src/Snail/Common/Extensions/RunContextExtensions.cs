using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

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
        /// 授信权限Id
        /// <para>1、涉及到多站点协作时，用于进行多站点之间缓存数据共享等授信操作</para>
        /// <para>2、获取失败，则默认使用ContextId填充值</para>
        /// </summary>
        public string TrustAuthId => Default(context.Get<string>(CONTEXT_TrustAuthId), context.ContextId)!;
        /// <summary>
        /// 父级操作Id
        /// <para>1、涉及到子操作时，在子的运行时上下文上传递</para>
        /// <para>2、涉及到其他站点调用过来时，本站点操作挂载到传递过来的父级操作Id下</para>
        /// </summary>
        /// <returns></returns>
        public string? ParentActionId => context.Get<string>(CONTEXT_ParentActionId);
        #endregion

        #region 共享钥匙串相关
        /// <summary>
        /// 初始化共享钥匙串
        /// </summary>
        /// <param name="shareKeyChainJson">共享钥匙串JSON数据</param>
        /// <returns></returns>
        public RunContext InitShareKeyChain(string? shareKeyChainJson)
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

        /// <summary>
        /// 添加共享钥匙串 <br />
        ///     1、key、value为空，不加
        /// </summary>
        /// <param name="key">钥匙串Key</param>
        /// <param name="value">钥匙串Value</param>
        public RunContext AddShareKeyChain(string key, string value)
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
        /// <param name="key">钥匙串Key</param>
        /// <returns>key为空，返回null；否则返回具体值</returns>
        public string? GetShareKeyChain(string key)
        {
            //  取数据返回
            string? tmpValue = null;
            if (key?.Length > 0)
            {
                context.RunShareKeyChain(forceInit: false, dict => dict?.TryGetValue(key, out tmpValue));
            }
            return tmpValue;
        }
        /// <summary>
        /// 获取共享钥匙串存储信息字典
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string>? GetShareKeyChain()
        {
            IDictionary<string, string>? keyChain = RunShareKeyChain(context, forceInit: false);
            //  重新转成新字典，避免外部操作
            IDictionary<string, string> map = keyChain?.ToDictionary(kv => kv.Key, kv => kv.Value)
                ?? new Dictionary<string, string>();
            //  强制加上授信Id
            map[CONTEXT_TrustAuthId] = context.TrustAuthId;

            return map;
        }

        /// <summary>
        /// 移除指定的共享钥匙串
        /// </summary>
        /// <param name="key">钥匙串Key</param>
        /// <returns>返回移除的value值</returns>
        public string? RemoveShareKeyChain(string key)
        {
            string? tmpValue = null;
            if (key?.Length > 0)
            {
                context.RunShareKeyChain(forceInit: false, dict => dict.Remove(key, out tmpValue));
            }
            return tmpValue;
        }

        /// <summary>
        /// 运行共享钥匙串相关操作
        /// </summary>
        /// <param name="forceInit">为null时是否强制初始化共享钥匙串字典</param>
        /// <param name="action">取到的钥匙串字典例外处理；取到的字典非null时触发</param>
        /// <returns>共享钥匙串字典</returns>
        private IDictionary<string, string>? RunShareKeyChain(bool forceInit, Action<IDictionary<string, string>>? action = null)
        {
            Dictionary<string, string>? keyChain = forceInit == true
                ? context.GetOrAdd<Dictionary<string, string>>(CONTEXT_ShareKeyChain, _ => new Dictionary<string, string>())
                : context.Get<Dictionary<string, string>>(CONTEXT_ShareKeyChain);
            if (keyChain?.Count > 0 && action != null)
            {
                action.Invoke(keyChain);
            }
            return keyChain;
        }
        #endregion
    }
}
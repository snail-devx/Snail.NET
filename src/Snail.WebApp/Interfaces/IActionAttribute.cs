namespace Snail.WebApp.Interfaces
{
    /// <summary>
    /// 接口：API动作特性标签
    /// </summary>
    public interface IActionAttribute
    {
        /// <summary>
        /// 是否禁用此特性标签
        /// <para>1、false，启用；表示此标签生效，并结合请求消息做具体的处理</para>
        /// <para>2、true，禁用；表示此标签不生效</para>
        /// <para>采用禁用逻辑，体现标签打了就生效，特殊需求再禁用的逻辑</para>
        /// </summary>
        public bool Disabled { init; get; }
    }
}

using Snail.Abstractions.ErrorCode;
using Snail.Abstractions.ErrorCode.DataModels;
using Snail.Abstractions.ErrorCode.Interfaces;
using Snail.Abstractions.Setting.Enumerations;
using Snail.Utilities.Collections;
using Snail.Utilities.Xml.Extensions;
using Snail.Utilities.Xml.Utils;
using System.Xml;

namespace Snail.ErrorCode;

/// <summary>
/// 错误编码管理器
/// </summary>
[Component<IErrorCodeManager>]
public sealed class ErrorCodeManager : IErrorCodeManager
{
    #region 属性变量
    /// <summary>
    /// 错误编码映射，key为culture值，value为具体的错误字典
    /// </summary>
    private readonly LockMap<string, List<IErrorCode>> _errorMap = new LockMap<string, List<IErrorCode>>();
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    [Inject]
    public ErrorCodeManager(IApplication app)
    {
        ThrowIfNull(app);
        app.UseSetting(isProject: true, rsCode: "errorcode", WatchErrorCodeSetting);
    }
    #endregion

    #region IErrorCodeManager
    /// <summary>
    /// 注册错误编码信息；确保<see cref="IErrorCode.Code"/>唯一，重复注册以第一个为准
    /// </summary>
    /// <param name="culture">语言环境；传null则走默认zh-CN</param>
    /// <param name="errors">错误编码集合</param>
    /// <returns>管理器自身，方便链式调用</returns>
    IErrorCodeManager IErrorCodeManager.Register(string? culture, params IList<IErrorCode> errors)
    {
        if (errors?.Count > 0)
        {
            ThrowIfHasNull(errors!, $"{nameof(errors)}存在为null的数据");
            culture = Default(culture, CULTURE_Default)!;
            List<IErrorCode> codes = _errorMap.GetOrAdd(culture, key => new List<IErrorCode>());
            lock (codes)
            {
                codes.AddRange(errors);
            }
        }
        return this;
    }

    /// <summary>
    /// 根据错误编码信息，获取具体的错误信息对象
    /// <para>1、若自身<paramref name="culture"/>找不到，则尝试从zh-CN查找 </para>
    /// </summary>
    /// <param name="culture">语言环境；传null则走默认zh-CN</param>
    /// <param name="code">错误编码</param>
    /// <returns>编码信息</returns>
    IErrorCode? IErrorCodeManager.Get(string? culture, string code)
    {
        ThrowIfNullOrEmpty(code);
        //  后期当 culture 为空时，先从上下文上去一下全局的culture值  GLOBALIZATION
        culture = Default(culture, defaultStr: CULTURE_Default)!;
        //  查找信息，若失败则从默认语言环境再试一下
        _errorMap.TryGetValue(culture, out List<IErrorCode>? codes);
        IErrorCode? error = codes?.FirstOrDefault(item => item.Code == code);
        if (error == null && culture != CULTURE_Default)
        {
            _errorMap.TryGetValue(CULTURE_Default, out codes);
            error = codes?.FirstOrDefault(item => item.Code == code);
        }
        return error;
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 监听错误编码配置变化
    /// </summary>
    /// <param name="workspace">配置所属工作空间</param>
    /// <param name="project">配置所属项目；为null表示工作空间下的资源，如服务器地址配置等</param>
    /// <param name="rsCode">配置资源的编码，唯一</param>
    /// <param name="type">配置类型，配置文件，后续支持</param>
    /// <param name="content">配置内容，根据<paramref name="type"/>类型不一样，这里值不同
    /// <para>1、<see cref="SettingType.File"/>：<paramref name="content"/>为文件的绝对路径 </para>
    /// <para>2、<see cref="SettingType.Xml"/>：<paramref name="content"/>为xml内容字符串 </para>
    /// </param>
    private void WatchErrorCodeSetting(string workspace, string? project, string rsCode, SettingType type, string content)
    {
        //  先仅支持【文件】类型配置读取
        if (type != SettingType.File)
        {
            string msg = $"{nameof(ErrorCodeManager)}读取配置时，仅支持File类型，当前为：{type.ToString()}；content:{content}";
            throw new ApplicationException(msg);
        }
        XmlDocument doc = XmlHelper.Load(content);
        doc.SelectNodes("/configuration/culture")?.ForEach(cultureNode =>
        {
            string culture = cultureNode.GetAttribute("name");
            string exPrefix = $"workspace:{workspace};code:{rsCode};xpath:/configuration/culture[name={culture ?? STR_Null}]/add";
            IList<IErrorCode> errors = new List<IErrorCode>();
            cultureNode.SelectNodes("add")?.ForEach(addNode =>
            {
                string code = addNode.GetAttribute("code");
                ThrowIfNullOrEmpty(code, $"错误编码add节点code属性为空。{exPrefix}");
                string message = addNode.GetAttribute("message");
                ThrowIfNullOrEmpty(message, $"错误编码add节点message属性为空。{exPrefix}[code={code}]");
                errors.Add(new ErrorCodeDescriptor(code, message));
            });
            //  注册
            (this as IErrorCodeManager).Register(culture, errors);
        });
    }
    #endregion
}

using Microsoft.CodeAnalysis;

namespace Snail.Aspect.Common.Interfaces
{
    /// <summary>
    /// �ӿ�Լ�����﷨������
    /// </summary>
    internal interface ISyntaxProxy
    {
        /// <summary>
        /// ΨһKeyֵ������Ϊ���ɵ�Դ��cs�ļ����� <br />
        /// </summary>
        /// <remarks>������null���򲻻�����cs�ļ�</remarks>
        string Key { get; }

        /// <summary>
        /// ����HTTP�ӿ�ʵ����Դ��
        /// </summary>
        /// <param name="context"></param>
        /// <returns>���ɺõĴ���</returns>
        string GenerateCode(SourceProductionContext context);
    }
}

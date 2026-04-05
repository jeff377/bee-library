using System.Collections.Generic;

namespace Bee.Cache.Providers
{
    /// <summary>
    /// �֨����Ѫ̤����A�w�q�֨��ާ@���Τ@�����C
    /// </summary>
    public interface ICacheProvider
    {
        /// <summary>
        /// �P�_�֨����جO�_�s�b��֨����C
        /// </summary>
        /// <param name="key">�֨���ȡC</param>
        bool Contains(string key);

        /// <summary>
        /// �N�֨����ظm�J�֨��Ϥ��C
        /// </summary>
        /// <param name="key">�֨���ȡC</param>
        /// <param name="value">�n�m�J�֨�������C</param>
        /// <param name="policy">�֨����ب������C</param>
        void Set(string key, object value, CacheItemPolicy policy);

        /// <summary>
        /// �q�֨��Ǧ^���ءC
        /// </summary>
        /// <param name="key">�֨���ȡC</param>
        object Get(string key);

        /// <summary>
        /// �����֨����ءC
        /// </summary>
        /// <param name="key">�֨���ȡC</param>
        /// <returns>�Ǧ^�������֨�����,�Y�֨����ؤ��s�b�h�Ǧ^ null�C</returns>
        object Remove(string key);

        /// <summary>
        /// �q�֨����󲾰����w�ʤ��񪺧֨����ءC
        /// </summary>
        /// <param name="percent">�������ت��ƥئb�֨������`�Ƥ��Ҧ����ʤ���C</param>
        /// <returns>�q�֨��Ϥ����������ؼƶq�C</returns>
        long Trim(int percent);

        /// <summary>
        /// �Ǧ^�֨������֨������`�ơC
        /// </summary>
        long GetCount();

        /// <summary>
        /// ���o�Ҧ��֨�����ȲM��C
        /// </summary>
        /// <returns>�֨���Ȫ��r��C�|�C</returns>
        IEnumerable<string> GetAllKeys();
    }
}
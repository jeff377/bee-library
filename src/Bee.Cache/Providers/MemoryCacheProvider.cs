using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using Bee.Base;

namespace Bee.Cache.Providers
{
    /// <summary>
    /// ��� MemoryCache ���֨����Ѫ̹�@�C
    /// </summary>
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly MemoryCache _memoryCache;

        /// <summary>
        /// �غc�禡,�ϥιw�]�� MemoryCache�C
        /// </summary>
        public MemoryCacheProvider()
        {
            _memoryCache = MemoryCache.Default;
        }

        /// <summary>
        /// �غc�禡,�ϥΫ��w�� MemoryCache�C
        /// </summary>
        /// <param name="memoryCache">�O����֨���ҡC</param>
        public MemoryCacheProvider(MemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// ���o�����j�p�g���֨���ȡC
        /// </summary>
        /// <param name="key">��l��ȡC</param>
        private string GetCacheKey(string key)
        {
            return StrFunc.ToUpper(key);
        }

        /// <summary>
        /// �P�_�֨����جO�_�s�b��֨����C
        /// </summary>
        /// <param name="key">�֨���ȡC</param>
        public bool Contains(string key)
        {
            string cacheKey = GetCacheKey(key);
            return _memoryCache.Contains(cacheKey);
        }

        /// <summary>
        /// �N�֨����ظm�J�֨��Ϥ��C
        /// </summary>
        /// <param name="key">�֨���ȡC</param>
        /// <param name="value">�n�m�J�֨�������C</param>
        /// <param name="policy">�֨����ب������C</param>
        public void Set(string key, object value, CacheItemPolicy policy)
        {
            var cacheKey = GetCacheKey(key);
            var cacheItem = new CacheItem(cacheKey, value);
            var cachePolicy = CacheFunc.CreateCachePolicy(policy);
            _memoryCache.Set(cacheItem, cachePolicy);
        }

        /// <summary>
        /// �q�֨��Ǧ^���ءC
        /// </summary>
        /// <param name="key">�֨���ȡC</param>
        public object Get(string key)
        {
            string cacheKey = GetCacheKey(key);
            return _memoryCache.Get(cacheKey);
        }

        /// <summary>
        /// �����֨����ءC
        /// </summary>
        /// <param name="key">�֨���ȡC</param>
        /// <returns>�Ǧ^�������֨�����,�Y�֨����ؤ��s�b�h�Ǧ^ null�C</returns>
        public object Remove(string key)
        {
            string cacheKey = GetCacheKey(key);
            return _memoryCache.Remove(cacheKey);
        }

        /// <summary>
        /// �q�֨����󲾰����w�ʤ��񪺧֨����ءC
        /// </summary>
        /// <param name="percent">�������ت��ƥئb�֨������`�Ƥ��Ҧ����ʤ���C</param>
        /// <returns>�q�֨��Ϥ����������ؼƶq�C</returns>
        public long Trim(int percent)
        {
            return _memoryCache.Trim(percent);
        }

        /// <summary>
        /// �Ǧ^�֨������֨������`�ơC
        /// </summary>
        public long GetCount()
        {
            return _memoryCache.GetCount();
        }

        /// <summary>
        /// ���o�Ҧ��֨�����ȲM��C
        /// </summary>
        /// <returns>�֨���Ȫ��r��C�|�C</returns>
        public IEnumerable<string> GetAllKeys()
        {
            // MemoryCache �b�C�|�L�{���i��Q��L������ק�
            // �]����ĳ�� ToList() ���ƻs�@��
            return _memoryCache.Select(item => item.Key).ToList();
        }
    }
}
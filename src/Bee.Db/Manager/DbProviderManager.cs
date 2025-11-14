using System;
using System.Collections.Generic;
using System.Data.Common;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// DbProvider 管理類別，統一管理不同資料庫的 Factory。
    /// </summary>
    public static class DbProviderManager
    {
        /// <summary>
        /// 存放已註冊的 DbProviderFactory。
        /// </summary>
        private static readonly Dictionary<DatabaseType, DbProviderFactory> _factories = new Dictionary<DatabaseType, DbProviderFactory>();

        /// <summary>
        /// 註冊新的資料庫提供者。
        /// </summary>
        /// <param name="type">資料庫類型</param>
        /// <param name="factory">對應的 DbProviderFactory</param>
        /// <exception cref="ArgumentNullException">當 factory 為 null 時拋出異常</exception>
        public static void RegisterProvider(DatabaseType type, DbProviderFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "DbProviderFactory cannot be null.");

            _factories[type] = factory;
        }

        /// <summary>
        /// 取得指定類型的 DbProviderFactory。
        /// </summary>
        /// <param name="type">資料庫類型</param>
        /// <returns>對應的 DbProviderFactory</returns>
        /// <exception cref="KeyNotFoundException">當指定類型未註冊時拋出異常</exception>
        public static DbProviderFactory GetFactory(DatabaseType type)
        {
            if (_factories.TryGetValue(type, out var factory))
            {
                return factory;
            }
            throw new KeyNotFoundException($"Database provider not registered: {type}");
        }
    }
}

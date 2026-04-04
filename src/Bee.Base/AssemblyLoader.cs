using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bee.Base
{
    /// <summary>
    /// 組件動態載入器。
    /// </summary>
    public static class AssemblyLoader
    {
        // 快取已載入的組件，避免重複載入
        private static readonly Dictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 尋找組件。
        /// </summary>
        /// <param name="assemblyName">組件名稱。</param>
        public static Assembly FindAssembly(string assemblyName)
        {
            // 先從快取中找
            if (_loadedAssemblies.TryGetValue(assemblyName, out var cached))
                return cached;

            // 從目前 AppDomain 中查找
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (StrFunc.IsEquals(assembly.ManifestModule.Name, assemblyName))
                {
                    _loadedAssemblies[assemblyName] = assembly;
                    return assembly;
                }
            }

            return null;
        }

        /// <summary>
        /// 判斷組件是否已載入。
        /// </summary>
        /// <param name="assemblyName">組件名稱。</param>
        public static bool IsAssemblyLoaded(string assemblyName)
        {
            return FindAssembly(assemblyName) != null;
        }

        /// <summary>
        /// 載入組件。
        /// </summary>
        /// <param name="assemblyName">組件名稱。</param>
        /// <returns>已載入的組件。</returns>
        public static Assembly LoadAssembly(string assemblyName)
        {
            // 若組件已載入，則直接回傳
            var assembly = FindAssembly(assemblyName);
            if (assembly != null)
                return assembly;

            // 取得組件的完整路徑
            string assemblyFile;
            if (StrFunc.IsEmpty(FileFunc.GetDirectory(assemblyName)))
                assemblyFile = FileFunc.PathCombine(FileFunc.GetAssemblyPath(), assemblyName);
            else
                assemblyFile = assemblyName;

            // 載入組件
            assembly = Assembly.LoadFrom(assemblyFile);
            _loadedAssemblies[assemblyName] = assembly;

            return assembly;
        }

        /// <summary>
        /// 建立指定型別的執行個體。
        /// </summary>
        /// <param name="assemblyName">組件名稱。</param>
        /// <param name="typeName">型別名稱。</param>
        /// <param name="args">建構函式引數。</param>
        public static object CreateInstance(string assemblyName, string typeName, params object[] args)
        {
            // 載入組件
            var assembly = LoadAssembly(assemblyName);
            // 建立執行個體
            return assembly.CreateInstance(typeName, true, BindingFlags.CreateInstance, null, args, null, null);
        }

        /// <summary>
        /// 建立指定型別的執行個體。
        /// </summary>
        /// <param name="fullTypeName">完整型別名稱，格式為 "Bee.Business.TBusinessObject, Bee.Business" 或 "Bee.Business.TBusinessObject"。</param>
        /// <param name="args">建構函式引數。</param>
        public static object CreateInstance(string fullTypeName, params object[] args)
        {
            // 取得正確的組件名稱及型別名稱
            GetAssemblyAndType(fullTypeName, out string assemblyName, out string typeName);
            // 建立指定型別的執行個體
            return CreateInstance(assemblyName, typeName, args);
        }

        /// <summary>
        /// 取得類型宣告。
        /// </summary>
        /// <param name="assemblyName">組件名稱。</param>
        /// <param name="typeName">型別名稱。</param>
        public static Type GetType(string assemblyName, string typeName)
        {
            var assembly = LoadAssembly(assemblyName);
            return assembly.GetType(typeName);
        }

        /// <summary>
        /// 取得類型宣告。
        /// </summary>
        /// <param name="fullTypeName">完整型別名稱。</param>
        public static Type GetType(string fullTypeName)
        {
            // 取得正確的組件名稱及型別名稱
            GetAssemblyAndType(fullTypeName, out string assemblyName, out string typeName);
            return GetType(assemblyName, typeName);
        }

        /// <summary>
        /// 由傳入型別名稱取得正確的組件名稱及型別名稱。
        /// </summary>
        /// <param name="fullTypeName">完整型別名稱。</param>
        /// <param name="assemblyName">組件名稱。</param>
        /// <param name="typeName">型別名稱。</param>
        private static void GetAssemblyAndType(string fullTypeName, out string assemblyName, out string typeName)
        {
            string leftPart, rightPart;
            if (StrFunc.Contains(fullTypeName, ","))
            {
                // 範例為 "Bee.Business.TBusinessObject, Bee.Business"
                StrFunc.SplitLeft(fullTypeName, ",", out leftPart, out rightPart);
                assemblyName = StrFunc.Format("{0}.dll", StrFunc.Trim(rightPart));
                typeName = leftPart;
            }
            else
            {
                // 範例為 "Bee.Business.TBusinessObject"
                StrFunc.SplitRight(fullTypeName, ".", out leftPart, out rightPart);
                assemblyName = StrFunc.Format("{0}.dll", leftPart);
                typeName = fullTypeName;
            }
        }
    }
}

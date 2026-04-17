using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bee.Base
{
    /// <summary>
    /// Dynamic assembly loader.
    /// </summary>
    public static class AssemblyLoader
    {
        // Cache loaded assemblies to avoid reloading
        private static readonly Dictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Finds the specified assembly.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        public static Assembly? FindAssembly(string assemblyName)
        {
            // Check the cache first
            if (_loadedAssemblies.TryGetValue(assemblyName, out var cached))
                return cached;

            // Search in the current AppDomain
            var match = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => StrFunc.IsEquals(a.ManifestModule.Name, assemblyName));
            if (match != null)
            {
                _loadedAssemblies[assemblyName] = match;
                return match;
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified assembly has been loaded.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        public static bool IsAssemblyLoaded(string assemblyName)
        {
            return FindAssembly(assemblyName) != null;
        }

        /// <summary>
        /// Loads the specified assembly.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <returns>The loaded assembly.</returns>
        public static Assembly LoadAssembly(string assemblyName)
        {
            // Return the cached assembly if already loaded
            var assembly = FindAssembly(assemblyName);
            if (assembly != null)
                return assembly;

            // Resolve the full path of the assembly
            string assemblyFile;
            if (StrFunc.IsEmpty(FileFunc.GetDirectory(assemblyName)))
                assemblyFile = FileFunc.PathCombine(FileFunc.GetAssemblyPath(), assemblyName);
            else
                assemblyFile = assemblyName;

            // Load the assembly by bytes to avoid locking the file on disk
            assembly = Assembly.Load(File.ReadAllBytes(assemblyFile));
            _loadedAssemblies[assemblyName] = assembly;

            return assembly;
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="typeName">The type name.</param>
        /// <param name="args">Constructor arguments.</param>
        public static object? CreateInstance(string assemblyName, string typeName, params object[] args)
        {
            // Load the assembly
            var assembly = LoadAssembly(assemblyName);
            // Create an instance
            return assembly.CreateInstance(typeName, true, BindingFlags.CreateInstance, null, args, null, null);
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// <param name="fullTypeName">The fully qualified type name, e.g. "Bee.Business.TBusinessObject, Bee.Business" or "Bee.Business.TBusinessObject".</param>
        /// <param name="args">Constructor arguments.</param>
        public static object? CreateInstance(string fullTypeName, params object[] args)
        {
            // Resolve the assembly name and type name
            GetAssemblyAndType(fullTypeName, out string assemblyName, out string typeName);
            // Create an instance of the specified type
            return CreateInstance(assemblyName, typeName, args);
        }

        /// <summary>
        /// Gets the type declaration for the specified type.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="typeName">The type name.</param>
        public static Type? GetType(string assemblyName, string typeName)
        {
            var assembly = LoadAssembly(assemblyName);
            return assembly.GetType(typeName);
        }

        /// <summary>
        /// Gets the type declaration for the specified fully qualified type name.
        /// </summary>
        /// <param name="fullTypeName">The fully qualified type name.</param>
        public static Type? GetType(string fullTypeName)
        {
            // Resolve the assembly name and type name
            GetAssemblyAndType(fullTypeName, out string assemblyName, out string typeName);
            return GetType(assemblyName, typeName);
        }

        /// <summary>
        /// Resolves the assembly name and type name from the given fully qualified type name.
        /// </summary>
        /// <param name="fullTypeName">The fully qualified type name.</param>
        /// <param name="assemblyName">The output assembly name.</param>
        /// <param name="typeName">The output type name.</param>
        private static void GetAssemblyAndType(string fullTypeName, out string assemblyName, out string typeName)
        {
            string leftPart, rightPart;
            if (StrFunc.Contains(fullTypeName, ","))
            {
                // Example: "Bee.Business.TBusinessObject, Bee.Business"
                StrFunc.SplitLeft(fullTypeName, ",", out leftPart, out rightPart);
                assemblyName = StrFunc.Format("{0}.dll", StrFunc.Trim(rightPart));
                typeName = leftPart;
            }
            else
            {
                // Example: "Bee.Business.TBusinessObject"
                StrFunc.SplitRight(fullTypeName, ".", out leftPart, out rightPart);
                assemblyName = StrFunc.Format("{0}.dll", leftPart);
                typeName = fullTypeName;
            }
        }
    }
}

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
                .FirstOrDefault(a => StringUtilities.IsEquals(a.ManifestModule.Name, assemblyName));
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

            // Load via AssemblyName so the assembly resolves into the default load context.
            // Loading via Assembly.Load(byte[]) would create a SEPARATE assembly identity
            // (anonymous load context), splitting static-field state between this loader's
            // copy and project-referenced copies — breaking cross-layer wire-up.
            // The simple name (no path, no .dll) is what AssemblyName accepts.
            var simpleName = Path.GetFileNameWithoutExtension(assemblyName);
            try
            {
                assembly = Assembly.Load(new AssemblyName(simpleName));
            }
            catch (FileNotFoundException)
            {
                // Fallback: load by full file path. LoadFile keeps the assembly distinct from
                // the default context — only reach here when default-context resolution fails
                // (e.g. assembly lives outside probing path).
                string assemblyFile = StringUtilities.IsEmpty(Path.GetDirectoryName(assemblyName))
                    ? Path.Combine(FileUtilities.GetAssemblyPath(), assemblyName)
                    : assemblyName;
                assembly = Assembly.LoadFile(assemblyFile);
            }
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
            if (StringUtilities.Contains(fullTypeName, ","))
            {
                // Example: "Bee.Business.TBusinessObject, Bee.Business"
                fullTypeName.SplitLeft(",", out leftPart, out rightPart);
                assemblyName = StringUtilities.Format("{0}.dll", StringUtilities.Trim(rightPart));
                typeName = leftPart;
            }
            else
            {
                // Example: "Bee.Business.TBusinessObject"
                fullTypeName.SplitRight(".", out leftPart, out rightPart);
                assemblyName = StringUtilities.Format("{0}.dll", leftPart);
                typeName = fullTypeName;
            }
        }
    }
}

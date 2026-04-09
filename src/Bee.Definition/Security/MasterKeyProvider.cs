using Bee.Definition.Settings;
using System;
using System.IO;
using Bee.Base;
using Bee.Base.Security;

namespace Bee.Definition.Security
{
    /// <summary>
    /// Master key provider that loads the master key from a configured source.
    /// </summary>
    public static class MasterKeyProvider
    {
        /// <summary>
        /// Gets the master key content.
        /// </summary>
        /// <param name="source">The master key source configuration.</param>
        /// <param name="autoCreate">Indicates whether to automatically create the master key if it does not exist.</param>
        /// <returns>The decoded master key as a byte array.</returns>
        public static byte[] GetMasterKey(MasterKeySource source, bool autoCreate =false)
        {
            string keyText;

            switch (source.Type)
            {
                case MasterKeySourceType.File:
                    keyText = LoadFromFile(source.Value, autoCreate);
                    break;

                case MasterKeySourceType.Environment:
                    keyText = LoadFromEnvironment(source.Value, autoCreate);
                    break;

                default:
                    throw new InvalidOperationException("Unsupported master key source type.");
            }

            if (string.IsNullOrWhiteSpace(keyText))
                throw new InvalidOperationException("Master key is empty or not found.");

            try
            {
                return Convert.FromBase64String(keyText.Trim());
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Master key is not valid Base64 format.", ex);
            }
        }

        /// <summary>
        /// Loads the master key content from a file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="autoCreate">Indicates whether to automatically create the master key if the file does not exist.</param>
        /// <returns>The master key content.</returns>
        private static string LoadFromFile(string filePath, bool autoCreate)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                filePath = "Master.key";
            }

            // If the path is relative, prepend BackendInfo.DefinePath
            if (!Path.IsPathRooted(filePath))
            {
                filePath = FileFunc.PathCombine(BackendInfo.DefinePath, filePath);
            }

            if (!File.Exists(filePath))
            {
                if (autoCreate)
                {
                    string newKey = GenerateNewKey();
                    File.WriteAllText(filePath, newKey);
                    return newKey;
                }
                throw new FileNotFoundException("Master key file not found: " + filePath);
            }

            return File.ReadAllText(filePath);
        }

        /// <summary>
        /// Loads the master key content from an environment variable.
        /// </summary>
        /// <param name="varName">The environment variable name.</param>
        /// <param name="autoCreate">Indicates whether to automatically create the master key if the variable is not set.</param>
        /// <returns>The master key content.</returns>
        private static string LoadFromEnvironment(string varName, bool autoCreate)
        {
            if (string.IsNullOrWhiteSpace(varName))
            {
                varName = "BEE_MASTER_KEY";
            }

            string value = Environment.GetEnvironmentVariable(varName);

            if (string.IsNullOrWhiteSpace(value))
            {
                if (autoCreate)
                {
                    string newKey = GenerateNewKey();
                    Environment.SetEnvironmentVariable(varName, newKey);
                    return newKey;
                }
                throw new InvalidOperationException("Environment variable '" + varName + "' not found.");
            }

            return value;
        }

        /// <summary>
        /// Generates a new Base64-encoded master key.
        /// </summary>
        private static string GenerateNewKey()
        {
            // Generate a new Base64-encoded master key
            return AesCbcHmacKeyGenerator.GenerateBase64CombinedKey();
        }
    }

}

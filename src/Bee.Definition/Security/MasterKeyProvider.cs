using Bee.Definition.Settings;
using System;
using System.IO;
using System.Threading;
using Bee.Base;
using Bee.Base.Security;

namespace Bee.Definition.Security
{
    /// <summary>
    /// Master key provider that loads the master key from a configured source.
    /// </summary>
    public static class MasterKeyProvider
    {
        private const int ReadRetryCount = 5;
        private const int ReadRetryDelayMs = 50;

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
                if (!autoCreate)
                    throw new FileNotFoundException("Master key file not found: " + filePath);

                // Atomically create the file so concurrent callers (e.g. parallel test hosts
                // sharing a Define folder) don't overwrite each other's keys. If another
                // process wins the race, fall through to read the key it just wrote.
                string newKey = GenerateNewKey();
                try
                {
                    using (var fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(fs))
                    {
                        writer.Write(newKey);
                    }
                    return newKey;
                }
                catch (IOException)
                {
                    // Another process created the file between our File.Exists check and
                    // FileMode.CreateNew; fall through to read the winning key.
                }
            }

            return ReadAllTextShared(filePath);
        }

        /// <summary>
        /// Reads the entire file with <see cref="FileShare.ReadWrite"/> so concurrent
        /// readers do not collide with the brief write/truncate lock held by another
        /// process during file creation. Retries on transient <see cref="IOException"/>.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        private static string ReadAllTextShared(string filePath)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);
                    return reader.ReadToEnd();
                }
                catch (IOException) when (attempt < ReadRetryCount - 1)
                {
                    Thread.Sleep(ReadRetryDelayMs);
                    attempt++;
                }
            }
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

            string? value = Environment.GetEnvironmentVariable(varName);

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Amanotes.Core;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Scans assemblies and file system for FieldBase subclass types.
    /// </summary>
    public static class FieldTypeScanner
    {
        /// <summary>
        /// Scan all loaded assemblies for concrete FieldBase subclasses.
        /// Also scans the custom field path for .cs files that may define additional types.
        /// </summary>
        public static List<Type> ScanAll(string customFieldPath)
        {
            var result = new List<Type>(32);
            var foundNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Scan assembly containing FieldBase
            try
            {
                var assembly = Assembly.GetAssembly(typeof(FieldBase));
                if (assembly != null)
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(FieldBase)))
                        {
                            result.Add(type);
                            foundNames.Add(type.Name);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SDebug.LogError($"[FieldTypeScanner] Assembly scan error: {e.Message}");
            }

            // Scan custom field path for additional types
            if (Directory.Exists(customFieldPath))
            {
                try
                {
                    string[] files = Directory.GetFiles(customFieldPath, "*.cs", SearchOption.AllDirectories);
                    for (int i = 0; i < files.Length; i++)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(files[i]);
                        if (foundNames.Contains(fileName)) continue;

                        Type type = FindTypeByName(fileName);
                        if (type != null)
                        {
                            result.Add(type);
                            foundNames.Add(type.Name);
                        }
                    }
                }
                catch (Exception e)
                {
                    SDebug.LogError($"[FieldTypeScanner] File scan error: {e.Message}");
                }
            }

            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            return result;
        }

        /// <summary>
        /// Filter types to only those whose .cs file exists in the specified folder.
        /// </summary>
        public static List<Type> FilterByFolder(List<Type> allTypes, string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return new List<Type>(allTypes);

            var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] files = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
                fileNames.Add(Path.GetFileNameWithoutExtension(files[i]));

            var filtered = new List<Type>(fileNames.Count);
            for (int i = 0; i < allTypes.Count; i++)
            {
                if (fileNames.Contains(allTypes[i].Name))
                    filtered.Add(allTypes[i]);
            }
            return filtered;
        }

        /// <summary>
        /// Find a FieldBase type by class name across all loaded assemblies.
        /// </summary>
        public static Type FindTypeByName(string className)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == className && typeof(FieldBase).IsAssignableFrom(type))
                            return type;
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    foreach (var type in ex.Types)
                    {
                        if (type != null && type.Name == className && typeof(FieldBase).IsAssignableFrom(type))
                            return type;
                    }
                }
                catch { /* skip problematic assemblies */ }
            }
            return null;
        }

        /// <summary>
        /// Get the immediate subfolder name under customFieldPath for a given type's .cs file.
        /// Returns "Root" if at root level, "Other" if not found.
        /// </summary>
        public static string GetTypeFolder(Type type, string customFieldPath)
        {
            if (!Directory.Exists(customFieldPath)) return "Other";

            string[] files = Directory.GetFiles(customFieldPath, $"{type.Name}.cs", SearchOption.AllDirectories);
            if (files.Length == 0) return "Other";

            string filePath = files[0].Replace('\\', '/');
            string basePath = customFieldPath.Replace('\\', '/').TrimEnd('/');

            string relative = filePath.Length > basePath.Length + 1
                ? filePath.Substring(basePath.Length + 1)
                : string.Empty;

            int slashIdx = relative.IndexOf('/');
            if (slashIdx <= 0) return "Root";

            return relative.Substring(0, slashIdx);
        }
    }
}

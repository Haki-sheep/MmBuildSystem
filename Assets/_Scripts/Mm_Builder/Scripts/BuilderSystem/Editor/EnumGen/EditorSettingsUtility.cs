using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Mm_Budier.Editor
{
    // 编辑器配置加载与通用工具
    internal static class EditorSettingsUtility
    {
        public static BuilderEditorSettings LoadOrCreate()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BuilderEditorSettings>(BuilderEditorSettings.AssetPath);
            if (asset != null)
                return asset;

            EnsureFolder(Path.GetDirectoryName(BuilderEditorSettings.AssetPath));

            asset = ScriptableObject.CreateInstance<BuilderEditorSettings>();
            AssetDatabase.CreateAsset(asset, BuilderEditorSettings.AssetPath);

            if (ECubeTypeParser.TryImportFromFile(BuilderEditorSettings.DefaultEnumPath, out var entries, out _))
            {
                asset.Entries = entries;
                ECubeTypeCodeGenerator.EnrichEntriesFromCubeAssets(asset);
                SyncCustomGroups(asset);
                SyncEnumNamePresets(asset);
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static bool IsValidAssetsFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return false;

            var normalized = NormalizeAssetsPath(folderPath);
            return normalized.StartsWith("Assets/") && AssetDatabase.IsValidFolder(normalized);
        }

        public static string NormalizeAssetsPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            path = path.Replace('\\', '/').Trim();
            if (path.StartsWith("Assets/"))
                return path;

            var dataPath = Application.dataPath.Replace('\\', '/');
            if (path.StartsWith(dataPath))
                return "Assets" + path[dataPath.Length..];

            return path;
        }

        /// <summary>
        /// 打开系统文件夹选择器，返回 Assets 相对路径；取消则返回 null。
        /// </summary>
        public static string PickAssetsFolder(string title, string currentFolder)
        {
            var assetsRoot = Application.dataPath.Replace('\\', '/');
            var startDir = assetsRoot;

            if (!string.IsNullOrWhiteSpace(currentFolder))
            {
                var normalized = NormalizeAssetsPath(currentFolder);
                if (normalized.StartsWith("Assets/"))
                {
                    var absolute = $"{assetsRoot}/{normalized["Assets/".Length..]}";
                    if (Directory.Exists(absolute))
                        startDir = absolute;
                }
            }

            var picked = EditorUtility.OpenFolderPanel(title, startDir, string.Empty);
            if (string.IsNullOrEmpty(picked))
                return null;

            picked = picked.Replace('\\', '/');
            if (!picked.StartsWith(assetsRoot))
            {
                EditorUtility.DisplayDialog("路径无效", "请选择项目 Assets 目录内的文件夹。", "确定");
                return null;
            }

            return NormalizeAssetsPath(picked);
        }

        public static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return;

            var normalized = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
                return;

            var parts = normalized.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public static IEnumerable<string> GetMenuGroups(BuilderEditorSettings settings)
        {
            if (settings == null)
                yield break;

            var seen = new HashSet<string>();

            if (settings.CustomGroups != null)
            {
                foreach (var group in settings.CustomGroups.Where(g => !string.IsNullOrWhiteSpace(g)))
                {
                    seen.Add(group);
                    yield return group;
                }
            }

            if (settings.Entries == null)
                yield break;

            foreach (var group in settings.Entries
                         .Select(e => e.Group)
                         .Where(g => !string.IsNullOrWhiteSpace(g))
                         .OrderBy(g => g))
            {
                if (seen.Add(group))
                    yield return group;
            }
        }

        public static IEnumerable<string> GetGroupOptions(BuilderEditorSettings settings)
        {
            foreach (var group in GetMenuGroups(settings))
                yield return group;
        }

        public static void EnsureCustomGroup(BuilderEditorSettings settings, string group)
        {
            if (settings == null || string.IsNullOrWhiteSpace(group))
                return;

            settings.CustomGroups ??= new List<string>();
            if (!settings.CustomGroups.Contains(group))
                settings.CustomGroups.Add(group);
        }

        public static void SyncCustomGroups(BuilderEditorSettings settings)
        {
            if (settings?.Entries == null)
                return;

            settings.CustomGroups ??= new List<string>();

            foreach (var group in settings.Entries.Select(e => e.Group).Where(g => !string.IsNullOrWhiteSpace(g)))
            {
                if (!settings.CustomGroups.Contains(group))
                    settings.CustomGroups.Add(group);
            }
        }

        public static void SyncEnumNamePresets(BuilderEditorSettings settings)
        {
            if (settings == null)
                return;

            settings.EnumNamePresets ??= new List<string>();
            settings.EnumNamePresets.Clear();

            if (settings.Entries == null)
                return;

            foreach (var name in settings.Entries.Select(e => e.Name).Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                if (!settings.EnumNamePresets.Contains(name))
                    settings.EnumNamePresets.Add(name);
            }
        }

        public static bool IsValidEnumName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (!char.IsLetter(name[0]) && name[0] != '_')
                return false;

            for (var i = 1; i < name.Length; i++)
            {
                var c = name[i];
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            return true;
        }

        public static int GetNextEntryValue(BuilderEditorSettings settings)
        {
            return settings?.Entries is { Count: > 0 }
                ? settings.Entries.Max(e => e.Value) + 1
                : 0;
        }

        public static CubeTypeEntry TryCreateEntryForName(BuilderEditorSettings settings, string name)
        {
            if (settings == null || string.IsNullOrWhiteSpace(name))
                return null;

            settings.Entries ??= new List<CubeTypeEntry>();
            if (settings.Entries.Any(e => e.Name == name))
                return null;

            var entry = new CubeTypeEntry
            {
                Name = name,
                Value = GetNextEntryValue(settings),
                Group = string.Empty,
                CreateCubeData = true,
                CubeAssetName = name,
            };
            settings.Entries.Add(entry);
            return entry;
        }

        public static void MarkDirty(BuilderEditorSettings settings)
        {
            if (settings == null)
                return;

            EditorUtility.SetDirty(settings);
        }

        public static void Save(BuilderEditorSettings settings)
        {
            MarkDirty(settings);
            AssetDatabase.SaveAssets();
        }

        public static IEnumerable<string> GetNameOptions(BuilderEditorSettings settings, string includeName = null)
        {
            var names = new HashSet<string>();

            if (settings?.EnumNamePresets != null)
            {
                foreach (var name in settings.EnumNamePresets.Where(n => !string.IsNullOrWhiteSpace(n)))
                    names.Add(name);
            }

            if (!string.IsNullOrWhiteSpace(includeName))
                names.Add(includeName);

            return names.OrderBy(n => n);
        }
    }
}

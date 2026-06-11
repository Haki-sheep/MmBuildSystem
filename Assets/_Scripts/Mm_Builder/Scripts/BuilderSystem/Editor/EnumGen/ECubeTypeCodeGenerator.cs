using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Mm_Budier.Editor
{
    // 生成 ECubeType 脚本与 CubeData 资产
    internal static class ECubeTypeCodeGenerator
    {
        public static bool GenerateAll(BuilderEditorSettings settings, out string message)
        {
            message = string.Empty;
            if (settings == null)
            {
                message = "编辑器配置为空";
                return false;
            }

            if (!ValidateEntries(settings.Entries, out message))
                return false;

            NormalizeEntryDefaults(settings.Entries);

            if (!WriteEnumFile(settings, out message))
                return false;

            if (!CreateOrUpdateCubeDataAssets(settings, out var cubeMessage))
            {
                message = cubeMessage;
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            message = $"生成完成\n枚举 {settings.EnumOutputPath}\n{cubeMessage}";
            return true;
        }

        public static string BuildPreview(BuilderEditorSettings settings)
        {
            NormalizeEntryDefaults(settings.Entries);
            return BuildEnumSource(settings);
        }

        public static void EnrichEntriesFromCubeAssets(BuilderEditorSettings settings)
        {
            if (settings?.Entries == null || settings.Entries.Count == 0)
                return;

            var existingByType = LoadCubeDataByType(settings.CubeDataOutputFolder);
            foreach (var entry in settings.Entries)
            {
                if (!existingByType.TryGetValue(entry.Value, out var cubeData))
                    continue;

                entry.CubeAssetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(cubeData));
                entry.CubePrefab = cubeData.CubePrefab;
            }
        }

        public static bool TrySyncEntryCubeData(BuilderEditorSettings settings, CubeTypeEntry entry)
        {
            if (settings == null || entry == null || !entry.CreateCubeData)
                return false;

            var cubeData = FindCubeDataAsset(settings, entry);
            if (cubeData == null)
                return false;

            var so = new SerializedObject(cubeData);
            so.FindProperty("CubeType").intValue = entry.Value;
            so.FindProperty("CubePrefab").objectReferenceValue = entry.CubePrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cubeData);
            return true;
        }

        private static bool ValidateEntries(List<CubeTypeEntry> entries, out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "枚举条目为空";
                return false;
            }

            if (entries.Any(e => string.IsNullOrWhiteSpace(e.Name)))
            {
                error = "存在空的枚举名称";
                return false;
            }

            var duplicateNames = entries.GroupBy(e => e.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateNames.Count > 0)
            {
                error = $"枚举名称重复 {string.Join(" ", duplicateNames)}";
                return false;
            }

            var duplicateValues = entries.GroupBy(e => e.Value).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateValues.Count > 0)
            {
                error = $"枚举值重复 {string.Join(" ", duplicateValues)}";
                return false;
            }

            return true;
        }

        private static void NormalizeEntryDefaults(List<CubeTypeEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.CubeAssetName))
                    entry.CubeAssetName = entry.Name;
            }
        }

        private static bool WriteEnumFile(BuilderEditorSettings settings, out string error)
        {
            error = null;
            if (!Directory.Exists(settings.EnumOutputFolder))
                Directory.CreateDirectory(settings.EnumOutputFolder);

            File.WriteAllText(settings.EnumOutputPath, BuildEnumSource(settings), Encoding.UTF8);
            return true;
        }

        private static string BuildEnumSource(BuilderEditorSettings settings)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"namespace {settings.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public enum {settings.EnumClassName}");
            sb.AppendLine("    {");

            string lastGroup = null;
            for (var i = 0; i < settings.Entries.Count; i++)
            {
                var entry = settings.Entries[i];
                if (!string.IsNullOrWhiteSpace(entry.Group) && entry.Group != lastGroup)
                {
                    if (i > 0)
                        sb.AppendLine();
                    sb.AppendLine($"        //{entry.Group}");
                    lastGroup = entry.Group;
                }

                var comment = string.IsNullOrWhiteSpace(entry.Comment) ? string.Empty : $"       // {entry.Comment}";
                sb.AppendLine($"        {entry.Name} = {entry.Value},{comment}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static bool CreateOrUpdateCubeDataAssets(BuilderEditorSettings settings, out string message)
        {
            var folder = settings.CubeDataOutputFolder;
            if (!AssetDatabase.IsValidFolder(folder))
                EditorSettingsUtility.EnsureFolder(folder);

            var existingByType = LoadCubeDataByType(folder);
            var created = 0;
            var updated = 0;
            var skipped = 0;

            foreach (var entry in settings.Entries)
            {
                if (!entry.CreateCubeData)
                {
                    skipped++;
                    continue;
                }

                var assetPath = $"{folder}/{entry.CubeAssetName}.asset";
                var cubeData = AssetDatabase.LoadAssetAtPath<CubeData>(assetPath);
                if (cubeData == null && existingByType.TryGetValue(entry.Value, out var matched))
                    cubeData = matched;

                if (cubeData == null)
                {
                    cubeData = ScriptableObject.CreateInstance<CubeData>();
                    AssetDatabase.CreateAsset(cubeData, assetPath);
                    existingByType[entry.Value] = cubeData;
                    created++;
                }
                else
                {
                    updated++;
                }

                ApplyEntryToCubeData(cubeData, entry);
            }

            message = $"CubeData 新建 {created} 更新 {updated} 跳过 {skipped}";
            return true;
        }

        private static CubeData FindCubeDataAsset(BuilderEditorSettings settings, CubeTypeEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.CubeAssetName))
                entry.CubeAssetName = entry.Name;

            var assetPath = $"{settings.CubeDataOutputFolder}/{entry.CubeAssetName}.asset";
            var cubeData = AssetDatabase.LoadAssetAtPath<CubeData>(assetPath);
            if (cubeData != null)
                return cubeData;

            var existingByType = LoadCubeDataByType(settings.CubeDataOutputFolder);
            return existingByType.TryGetValue(entry.Value, out var matched) ? matched : null;
        }

        private static void ApplyEntryToCubeData(CubeData cubeData, CubeTypeEntry entry)
        {
            var so = new SerializedObject(cubeData);
            so.FindProperty("CubeType").intValue = entry.Value;
            so.FindProperty("CubePrefab").objectReferenceValue = entry.CubePrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cubeData);
        }

        private static Dictionary<int, CubeData> LoadCubeDataByType(string folder)
        {
            var result = new Dictionary<int, CubeData>();
            if (!AssetDatabase.IsValidFolder(folder))
                return result;

            foreach (var guid in AssetDatabase.FindAssets("t:CubeData", new[] { folder }))
            {
                var cubeData = AssetDatabase.LoadAssetAtPath<CubeData>(AssetDatabase.GUIDToAssetPath(guid));
                if (cubeData == null)
                    continue;

                var typeValue = new SerializedObject(cubeData).FindProperty("CubeType").intValue;
                result[typeValue] = cubeData;
            }

            return result;
        }
    }
}

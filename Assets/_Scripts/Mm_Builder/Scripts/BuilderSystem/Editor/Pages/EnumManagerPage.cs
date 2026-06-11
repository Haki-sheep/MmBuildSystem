using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Mm_Budier.Editor
{
    public class EnumManagerPage
    {
        private BuildEditorWindow window;
        private BuilderEditorSettings settings;

        [PropertyOrder(0)]
        [FoldoutGroup("输出设置", expanded: true)]
        [ShowIf(nameof(ShowEnumFolderWarning))]
        [InfoBox("$EnumFolderWarning", InfoMessageType.Warning)]
        private void DrawEnumFolderWarning() { }

        [FoldoutGroup("输出设置")]
        [HorizontalGroup("输出设置/EnumFolder")]
        [LabelText("枚举输出目录"), LabelWidth(110)]
        [ShowInInspector]
        private string EnumOutputFolder
        {
            get => settings?.EnumOutputFolder ?? string.Empty;
            set
            {
                if (settings == null)
                    return;
                settings.EnumOutputFolder = EditorSettingsUtility.NormalizeAssetsPath(value);
                EditorSettingsUtility.MarkDirty(settings);
            }
        }

        [FoldoutGroup("输出设置")]
        [HorizontalGroup("输出设置/EnumFolder", Width = 56)]
        [Button("浏览")]
        private void BrowseEnumOutputFolder() => PickFolder(path => settings.EnumOutputFolder = path, "选择枚举输出目录", settings?.EnumOutputFolder);

        [FoldoutGroup("输出设置")]
        [LabelText("枚举文件名"), LabelWidth(110)]
        [ShowInInspector]
        private string EnumFileName
        {
            get => settings?.EnumFileName ?? string.Empty;
            set
            {
                if (settings == null)
                    return;
                settings.EnumFileName = value;
                EditorSettingsUtility.MarkDirty(settings);
            }
        }

        [FoldoutGroup("输出设置")]
        [ShowIf(nameof(ShowCubeFolderWarning))]
        [InfoBox("$CubeFolderWarning", InfoMessageType.Warning)]
        private void DrawCubeFolderWarning() { }

        [FoldoutGroup("输出设置")]
        [HorizontalGroup("输出设置/CubeFolder")]
        [LabelText("CubeData 输出目录"), LabelWidth(110)]
        [ShowInInspector]
        private string CubeDataOutputFolder
        {
            get => settings?.CubeDataOutputFolder ?? string.Empty;
            set
            {
                if (settings == null)
                    return;
                settings.CubeDataOutputFolder = EditorSettingsUtility.NormalizeAssetsPath(value);
                EditorSettingsUtility.MarkDirty(settings);
            }
        }

        [FoldoutGroup("输出设置")]
        [HorizontalGroup("输出设置/CubeFolder", Width = 56)]
        [Button("浏览")]
        private void BrowseCubeDataOutputFolder() => PickFolder(path => settings.CubeDataOutputFolder = path, "选择 CubeData 输出目录", settings?.CubeDataOutputFolder);

        [FoldoutGroup("输出设置")]
        [LabelText("命名空间"), LabelWidth(110)]
        [ShowInInspector]
        private string OutputNamespace
        {
            get => settings?.Namespace ?? string.Empty;
            set
            {
                if (settings == null)
                    return;
                settings.Namespace = value;
                EditorSettingsUtility.MarkDirty(settings);
            }
        }

        [FoldoutGroup("输出设置")]
        [LabelText("枚举类名"), LabelWidth(110)]
        [ShowInInspector]
        private string EnumClassName
        {
            get => settings?.EnumClassName ?? string.Empty;
            set
            {
                if (settings == null)
                    return;
                settings.EnumClassName = value;
                EditorSettingsUtility.MarkDirty(settings);
            }
        }

        private bool ShowEnumFolderWarning => settings != null && !EditorSettingsUtility.IsValidAssetsFolder(settings.EnumOutputFolder);
        private bool ShowCubeFolderWarning => settings != null && !EditorSettingsUtility.IsValidAssetsFolder(settings.CubeDataOutputFolder);
        private string EnumFolderWarning => $"枚举输出目录无效：{settings?.EnumOutputFolder}\n请点击「浏览」重新选择 Assets 内的文件夹。";
        private string CubeFolderWarning => $"CubeData 输出目录无效：{settings?.CubeDataOutputFolder}\n请点击「浏览」重新选择 Assets 内的文件夹。";

        private void PickFolder(System.Action<string> apply, string title, string current)
        {
            if (!EnsureSettings())
                return;

            var picked = EditorSettingsUtility.PickAssetsFolder(title, current);
            if (picked == null)
                return;

            apply(picked);
            EditorSettingsUtility.Save(settings);
        }

        [PropertyOrder(4)]
        [BoxGroup("添加方块")]
        [HorizontalGroup("添加方块/Row", Width = 0.75f)]
        [LabelText("枚举名")]
        public string newCubeEnumName = string.Empty;

        [HorizontalGroup("添加方块/Row", Width = 0.25f)]
        [Button("添加方块", ButtonSizes.Medium)]
        [GUIColor(0.45f, 0.82f, 0.55f)]
        public void AddCube()
        {
            if (!EnsureSettings())
                return;

            var name = newCubeEnumName?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                EditorUtility.DisplayDialog("添加失败", "请输入枚举名", "确定");
                return;
            }

            if (!EditorSettingsUtility.IsValidEnumName(name))
            {
                EditorUtility.DisplayDialog("添加失败", "枚举名只能包含字母、数字和下划线，且不能以数字开头", "确定");
                return;
            }

            if (settings.Entries.Any(e => e.Name == name))
            {
                EditorUtility.DisplayDialog("添加失败", $"枚举名 {name} 已存在", "确定");
                return;
            }

            var entry = EditorSettingsUtility.TryCreateEntryForName(settings, name);
            if (entry == null)
                return;

            EditorSettingsUtility.SyncEnumNamePresets(settings);
            EditorSettingsUtility.Save(settings);
            newCubeEnumName = string.Empty;
            window?.SelectEntry(entry);
        }

        [PropertyOrder(5)]
        [FoldoutGroup("枚举列表", expanded: true)]
        [InfoBox("请使用上方「添加方块」添加新枚举，不要在此列表点加号。", InfoMessageType.Info)]
        [ShowInInspector]
        [ListDrawerSettings(ShowIndexLabels = false, DraggableItems = false)]
        [OnCollectionChanged(nameof(OnPresetNamesChanged))]
        [HideLabel]
        private List<string> presetEnumNames = new();

        [PropertyOrder(6)]
        [FoldoutGroup("分类管理", expanded: true)]
        [ShowInInspector]
        [ListDrawerSettings(ShowIndexLabels = false, DraggableItems = true)]
        [OnCollectionChanged(nameof(OnPresetGroupsChanged))]
        private List<string> presetGroups = new();

        [PropertyOrder(10)]
        [HorizontalGroup("Actions", Width = 1f / 3f)]
        [Button("从 ECubeType.cs 导入", ButtonSizes.Medium)]
        public void ImportFromEnumFile()
        {
            if (!EnsureSettings() || !EnsureOutputPaths())
                return;

            if (!ECubeTypeParser.TryImportFromFile(settings.EnumOutputPath, out var entries, out var error))
            {
                EditorUtility.DisplayDialog("导入失败", error, "确定");
                return;
            }

            settings.Entries = entries;
            EditorSettingsUtility.SyncCustomGroups(settings);
            EditorSettingsUtility.SyncEnumNamePresets(settings);
            SyncPresetReferences();
            ECubeTypeCodeGenerator.EnrichEntriesFromCubeAssets(settings);
            EditorSettingsUtility.Save(settings);
            window?.ForceMenuTreeRebuild();
            EditorUtility.DisplayDialog("导入成功", $"已导入 {entries.Count} 个条目", "确定");
        }

        [PropertyOrder(10)]
        [HorizontalGroup("Actions", Width = 1f / 3f)]
        [Button("预览代码", ButtonSizes.Medium)]
        public void PreviewCode()
        {
            if (!EnsureSettings() || settings.Entries.Count == 0)
            {
                codePreview = "// 枚举条目为空";
                return;
            }

            codePreview = ECubeTypeCodeGenerator.BuildPreview(settings);
        }

        [PropertyOrder(10)]
        [HorizontalGroup("Actions", Width = 1f / 3f)]
        [Button("生成枚举和 CubeData", ButtonSizes.Medium)]
        [GUIColor(0.45f, 0.82f, 0.55f)]
        public void GenerateAll()
        {
            if (!EnsureSettings() || !EnsureOutputPaths())
                return;

            if (!ECubeTypeCodeGenerator.GenerateAll(settings, out var message))
            {
                EditorUtility.DisplayDialog("生成失败", message, "确定");
                return;
            }

            EditorSettingsUtility.Save(settings);
            window?.ForceMenuTreeRebuild();
            EditorUtility.DisplayDialog("生成成功", message, "确定");
        }

        [PropertyOrder(11)]
        [Button("定位配置资产", ButtonSizes.Medium)]
        public void PingSettingsAsset()
        {
            if (settings != null)
                EditorGUIUtility.PingObject(settings);
        }

        [PropertyOrder(20)]
        [ShowInInspector, ReadOnly, HideLabel]
        [MultiLineProperty(14)]
        private string codePreview;

        public void Bind(BuildEditorWindow editorWindow, BuilderEditorSettings editorSettings)
        {
            window = editorWindow;
            settings = editorSettings;
            EditorSettingsUtility.SyncEnumNamePresets(settings);
            EditorSettingsUtility.SyncCustomGroups(settings);
            ECubeTypeCodeGenerator.EnrichEntriesFromCubeAssets(settings);
            SyncPresetReferences();
        }

        private void SyncPresetReferences()
        {
            if (settings == null)
                return;

            settings.CustomGroups ??= new List<string>();
            settings.EnumNamePresets ??= new List<string>();
            presetGroups = settings.CustomGroups;
            presetEnumNames = settings.EnumNamePresets;
        }

        private void OnPresetGroupsChanged()
        {
            settings.CustomGroups = presetGroups;
            EditorSettingsUtility.MarkDirty(settings);
            window?.ForceMenuTreeRebuild();
        }

        private void OnPresetNamesChanged()
        {
            if (!EnsureSettings())
                return;

            settings.EnumNamePresets = presetEnumNames;

            var names = presetEnumNames
                .Select(n => n?.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            if (names.Count > settings.Entries.Count)
            {
                RevertPresetListFromEntries();
                EditorUtility.DisplayDialog("提示", "请使用上方「添加方块」创建新枚举，不要在此列表点加号。", "确定");
                return;
            }

            foreach (var name in names)
            {
                if (!EditorSettingsUtility.IsValidEnumName(name))
                {
                    RevertPresetListFromEntries();
                    EditorUtility.DisplayDialog("修改失败", "枚举名只能包含字母、数字和下划线，且不能以数字开头", "确定");
                    return;
                }
            }

            if (names.GroupBy(n => n).Any(g => g.Count() > 1))
            {
                RevertPresetListFromEntries();
                EditorUtility.DisplayDialog("修改失败", "枚举名不能重复", "确定");
                return;
            }

            settings.Entries.RemoveAll(e => !names.Contains(e.Name));

            if (settings.Entries.Count != names.Count)
            {
                RevertPresetListFromEntries();
                return;
            }

            for (var i = 0; i < names.Count; i++)
                settings.Entries[i].Name = names[i];

            EditorSettingsUtility.SyncEnumNamePresets(settings);
            SyncPresetReferences();
            EditorSettingsUtility.Save(settings);
            window?.ForceMenuTreeRebuild();
        }

        private void RevertPresetListFromEntries()
        {
            EditorSettingsUtility.SyncEnumNamePresets(settings);
            presetEnumNames = settings.EnumNamePresets;
        }

        private bool EnsureSettings()
        {
            if (settings != null)
                return true;

            EditorUtility.DisplayDialog("配置缺失", "未找到 BuilderEditorSettings", "确定");
            return false;
        }

        private bool EnsureOutputPaths()
        {
            if (EditorSettingsUtility.IsValidAssetsFolder(settings.EnumOutputFolder)
                && EditorSettingsUtility.IsValidAssetsFolder(settings.CubeDataOutputFolder))
                return true;

            EditorUtility.DisplayDialog(
                "输出路径无效",
                "请先在「输出设置」中点击「浏览」，选择 Assets 内的有效文件夹。",
                "确定");
            return false;
        }
    }
}

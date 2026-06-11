using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Mm_Budier.Editor
{
    public class ECubeTypeEntryPage
    {
        private const string NoneGroupLabel = "无分类";

        [HideInInspector] public BuilderEditorSettings Settings;
        [HideInInspector] public CubeTypeEntry Entry;
        [HideInInspector] public Action<CubeTypeEntry> OnMenuStructureChanged;

        [ShowInInspector]
        [Title("@GetTitle()")]
        [BoxGroup("基础")]
        [LabelText("枚举名")]
        [ValueDropdown(nameof(GetNameDropdown))]
        [OnValueChanged(nameof(OnNameChanged))]
        public string EntryName
        {
            get => Entry?.Name ?? string.Empty;
            set { if (Entry != null) Entry.Name = value; }
        }

        [ShowInInspector]
        [BoxGroup("基础")]
        [LabelText("注释")]
        [OnValueChanged(nameof(MarkDirty))]
        public string EntryComment
        {
            get => Entry?.Comment ?? string.Empty;
            set { if (Entry != null) Entry.Comment = value; }
        }

        [ShowInInspector]
        [BoxGroup("基础")]
        [LabelText("分类")]
        [ValueDropdown(nameof(GetGroupDropdown))]
        [OnValueChanged(nameof(OnGroupChanged))]
        public string EntryGroup
        {
            get => Entry?.Group ?? string.Empty;
            set { if (Entry != null) Entry.Group = value ?? string.Empty; }
        }

        [ShowInInspector]
        [BoxGroup("CubeData")]
        [LabelText("创建 CubeData")]
        [OnValueChanged(nameof(MarkDirty))]
        public bool EntryCreateCubeData
        {
            get => Entry?.CreateCubeData ?? false;
            set { if (Entry != null) Entry.CreateCubeData = value; }
        }

        [ShowInInspector]
        [BoxGroup("CubeData")]
        [ShowIf(nameof(EntryCreateCubeData))]
        [LabelText("资产文件名")]
        [OnValueChanged(nameof(MarkDirty))]
        public string EntryCubeAssetName
        {
            get => Entry?.CubeAssetName ?? string.Empty;
            set { if (Entry != null) Entry.CubeAssetName = value; }
        }

        [ShowInInspector]
        [BoxGroup("CubeData")]
        [ShowIf(nameof(EntryCreateCubeData))]
        [LabelText("预制体")]
        [AssetsOnly]
        [OnValueChanged(nameof(OnPrefabChanged))]
        public GameObject EntryCubePrefab
        {
            get => Entry?.CubePrefab;
            set { if (Entry != null) Entry.CubePrefab = value; }
        }

        [Button("删除此条目", ButtonSizes.Medium)]
        [GUIColor(1f, 0.45f, 0.45f)]
        private void RemoveEntry()
        {
            if (Entry == null || Settings == null)
                return;

            if (!EditorUtility.DisplayDialog("删除条目", $"确定删除 {Entry.Name} 吗", "删除", "取消"))
                return;

            var index = Settings.Entries.IndexOf(Entry);
            Settings.Entries.Remove(Entry);
            EditorSettingsUtility.SyncEnumNamePresets(Settings);
            EditorSettingsUtility.Save(Settings);

            CubeTypeEntry next = null;
            if (Settings.Entries.Count > 0)
            {
                var pick = Mathf.Clamp(index, 0, Settings.Entries.Count - 1);
                next = Settings.Entries[pick];
            }

            OnMenuStructureChanged?.Invoke(next);
        }

        private IEnumerable<ValueDropdownItem<string>> GetNameDropdown()
        {
            foreach (var name in EditorSettingsUtility.GetNameOptions(Settings, Entry?.Name))
                yield return new ValueDropdownItem<string>(name, name);
        }

        private IEnumerable<ValueDropdownItem<string>> GetGroupDropdown()
        {
            yield return new ValueDropdownItem<string>(NoneGroupLabel, string.Empty);

            if (Settings == null)
                yield break;

            foreach (var group in EditorSettingsUtility.GetGroupOptions(Settings))
                yield return new ValueDropdownItem<string>(group, group);
        }

        private void OnNameChanged()
        {
            EditorSettingsUtility.SyncEnumNamePresets(Settings);
            MarkDirty();
            OnMenuStructureChanged?.Invoke(Entry);
        }

        private void MarkDirty() => EditorSettingsUtility.MarkDirty(Settings);

        private void OnGroupChanged()
        {
            if (!string.IsNullOrWhiteSpace(EntryGroup))
                EditorSettingsUtility.EnsureCustomGroup(Settings, EntryGroup);

            MarkDirty();
            OnMenuStructureChanged?.Invoke(Entry);
        }

        private void OnPrefabChanged()
        {
            MarkDirty();
            if (ECubeTypeCodeGenerator.TrySyncEntryCubeData(Settings, Entry))
                AssetDatabase.SaveAssets();
        }

        private string GetTitle() => string.IsNullOrWhiteSpace(Entry?.Name) ? "未命名条目" : Entry.Name;
    }
}

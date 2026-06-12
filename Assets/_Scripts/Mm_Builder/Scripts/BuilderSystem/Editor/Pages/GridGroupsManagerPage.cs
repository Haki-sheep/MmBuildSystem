using System.Collections.Generic;
using Mm_Budier;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Mm_Budier.Editor
{
    /// <summary>Mm Builder → 分区管理：编辑分区列表，与场景组件及 grid-groups.json 同步。</summary>
    public class GridGroupsManagerPage
    {
        [TitleGroup("场景目标")]
        [InfoBox("编辑「原点 + 尺寸(格)」，或点分区上的「Scene 两点取格」在 Scene 连点两角。JSON 与方块存档同目录。")]
        [ShowInInspector, LabelText("虚拟网格组件")]
        [OnValueChanged(nameof(OnTargetChanged))]
        private BuilderVirtualGrid targetGrid;

        [TitleGroup("场景目标")]
        [ShowInInspector, ReadOnly, LabelText("配置文件路径")]
        private string ConfigFilePath => GridGroupsConfigIO.GetFilePath();

        [TitleGroup("分区配置")]
        [ShowInInspector, LabelText("启用分区")]
        private bool useGridGroups;

        [TitleGroup("分区配置")]
        [ShowInInspector, LabelText("网格组列表")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
        private List<VirtualGridGroup> groups = new();

        [TitleGroup("操作")]
        [HorizontalGroup("操作/Row1")]
        [Button("从场景读取", ButtonSizes.Medium)]
        private void PullFromScene()
        {
            if (!EnsureTarget())
                return;

            SyncGridUnitSize();
            (useGridGroups, groups) = GridGroupsConfigIO.Capture(targetGrid);
            Debug.Log($"[GridGroups] 已从场景读取 {groups.Count} 个分区");
        }

        [HorizontalGroup("操作/Row1")]
        [Button("应用到场景", ButtonSizes.Medium)]
        private void ApplyToScene()
        {
            if (!EnsureTarget())
                return;

            Undo.RecordObject(targetGrid, "Apply Grid Groups");
            GridGroupsConfigIO.Apply(targetGrid, useGridGroups, groups);
            EditorUtility.SetDirty(targetGrid);
            Debug.Log($"[GridGroups] 已应用到场景 {targetGrid.name}（{groups.Count} 个分区）");
        }

        [HorizontalGroup("操作/Row2")]
        [Button("保存到文件", ButtonSizes.Medium)]
        private void SaveToFile()
        {
            if (!GridGroupsConfigIO.TrySave(useGridGroups, groups, out var error))
            {
                Debug.LogError($"[GridGroups] 保存失败：{error}");
                return;
            }

            EditorUtility.DisplayDialog("分区配置", $"已保存到：\n{GridGroupsConfigIO.GetFilePath()}", "确定");
        }

        [HorizontalGroup("操作/Row2")]
        [Button("从文件加载", ButtonSizes.Medium)]
        private void LoadFromFile()
        {
            if (!GridGroupsConfigIO.TryLoad(out useGridGroups, out groups, out var error))
            {
                Debug.LogWarning($"[GridGroups] 加载失败：{error}");
                EditorUtility.DisplayDialog("分区配置", error, "确定");
                return;
            }

            SyncGridUnitSize();
            Debug.Log($"[GridGroups] 已从文件加载 {groups.Count} 个分区");
        }

        [HorizontalGroup("操作/Row2")]
        [Button("打开存档目录", ButtonSizes.Medium)]
        private void OpenSaveFolder() => GridGroupsConfigIO.RevealInExplorer();

        [HorizontalGroup("操作/Row3")]
        [Button("定位场景组件", ButtonSizes.Medium)]
        private void PingTarget()
        {
            if (!EnsureTarget())
                return;

            Selection.activeObject = targetGrid;
            EditorGUIUtility.PingObject(targetGrid);
        }

        public void Refresh()
        {
            if (targetGrid == null)
                targetGrid = GridGroupsConfigIO.FindFirstVirtualGridInActiveScene();

            SyncGridUnitSize();

            if (targetGrid != null && (groups == null || groups.Count == 0))
                PullFromScene();
        }

        private bool EnsureTarget()
        {
            if (targetGrid != null)
                return true;

            targetGrid = GridGroupsConfigIO.FindFirstVirtualGridInActiveScene();
            if (targetGrid != null)
            {
                SyncGridUnitSize();
                return true;
            }

            EditorUtility.DisplayDialog("分区管理", "请先指定场景中的 BuilderVirtualGrid 组件。", "确定");
            return false;
        }

        private void OnTargetChanged()
        {
            SyncGridUnitSize();
            if (targetGrid != null)
                PullFromScene();
        }

        private void SyncGridUnitSize()
        {
            BuilderVirtualGrid.EditorGridUnitSize = targetGrid != null
                ? Mathf.Max(1, targetGrid.gridUnitSize)
                : 1;
        }
    }
}

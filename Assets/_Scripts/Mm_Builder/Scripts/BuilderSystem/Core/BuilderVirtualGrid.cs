using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BuilderVirtualGrid : MonoBehaviour
{
    [Header("基础设置")]
    [LabelText("单位虚拟网格大小")] public int gridUnitSize;

    [LabelText("虚拟网格X/Z最小坐标")] public int minGridXZ;
    [LabelText("虚拟网格X/Z最大坐标")] public int maxGridXZ = 1;
    [LabelText("虚拟网格Y最小坐标")] public int minGridY = 1;
    [LabelText("虚拟网格Y最大坐标")] public int maxGridY;

    [Header("分区网格")]
    [LabelText("启用分区")]
    public bool useGridGroups;
    [LabelText("网格组列表"), ShowIf("useGridGroups")]
    public List<VirtualGridGroup> gridGroups = new();

    [Header("可视化配置")]
    [LabelText("显示虚拟网格高度")]
    public bool showGridHeight = true;
    [LabelText("高度标签颜色"), ShowIf("showGridHeight")]
    public Color heightLabelColor = Color.white;
    [LabelText("高度标签字号"), ShowIf("showGridHeight"), MinValue(8)]
    public int heightLabelFontSize = 16;
    [LabelText("显示虚拟网格颜色")]
    public bool showGridColor = true;
    [LabelText("显示Y轴竖线颜色"), ShowIf("showGridHeight")]
    public bool showYAxisColor = true;
    [LabelText("虚拟网格颜色"), ShowIf("showGridColor")]
    public Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    [LabelText("Y轴竖线颜色"), ShowIf("showYAxisColor")]
    public Color yAxisColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);


    /// <summary>
    /// 世界转网格坐标
    /// 示例：世界坐标(5.2, 0.8, 5.9) → 网格坐标(5, 0, 5)
    /// </summary>
    /// <param name="worldPos">Unity世界坐标</param>
    /// <param name="clampToBounds">是否限制在地图边界内</param>
    /// <returns>网格坐标</returns>
    public Vector3Int WorldToGrid(Vector3 worldPos, bool clampToBounds = true)
    {
        var cell = WorldToCell(worldPos, gridUnitSize);

        if (clampToBounds)
        {
            cell.x = Mathf.Clamp(cell.x, minGridXZ, maxGridXZ);
            cell.y = Mathf.Clamp(cell.y, minGridY, maxGridY);
            cell.z = Mathf.Clamp(cell.z, minGridXZ, maxGridXZ);
        }

        return cell;
    }

    /// <summary>
    /// 世界坐标 → 格索引（左闭区间，与 Gizmo / 放置逻辑一致）
    /// </summary>
    public static Vector3Int WorldToCell(Vector3 worldPos, int gridUnitSize)
    {
        var unit = gridUnitSize > 0 ? gridUnitSize : 1;
        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x / unit),
            Mathf.FloorToInt(worldPos.y / unit),
            Mathf.FloorToInt(worldPos.z / unit));
    }

    /// <summary>
    /// 网格转世界坐标
    /// 示例：网格坐标(5, 0, 5) → 世界坐标(5.5, 0.5, 5.5)
    /// </summary>
    /// <param name="gridPos">网格坐标</param>
    /// <returns>网格中心的世界坐标</returns>
    public Vector3 GridToWorldCenter(Vector3Int gridPos)
    {
        // 网格左下角世界坐标 + 半个网格单元（居中）
        float worldX = gridPos.x * gridUnitSize + gridUnitSize / 2f;
        float worldY = gridPos.y * gridUnitSize + gridUnitSize / 2f;
        float worldZ = gridPos.z * gridUnitSize + gridUnitSize / 2f;

        return new Vector3(worldX, worldY, worldZ);
    }

    // 左闭右开 [min, max)，与 VirtualGridGroup.Contains 一致
    public bool ValidBoundary(Vector3Int gridPos)
    {
        if (gridPos.x < minGridXZ || gridPos.y < minGridY || gridPos.z < minGridXZ)
        {
            return false;
        }
        if (gridPos.x >= maxGridXZ || gridPos.y >= maxGridY || gridPos.z >= maxGridXZ)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// 放置校验用：启用分区时必须在某一允许的分区内，否则走全局 ValidBoundary
    /// </summary>
    public bool ValidPlacementCell(Vector3Int gridPos)
    {
        if (!useGridGroups || gridGroups == null || gridGroups.Count == 0)
            return ValidBoundary(gridPos);

        foreach (var group in gridGroups)
        {
            if (group != null && group.allowPlacement && group.Contains(gridPos))
                return true;
        }
        return false;
    }

    #region 编辑器可视化（Scene视图画网格线）
#if UNITY_EDITOR
    public static int EditorGridUnitSize { get; set; } = 1;
#endif

    private void OnValidate()
    {
#if UNITY_EDITOR
        EditorGridUnitSize = Mathf.Max(1, gridUnitSize);
#endif
        ValidateConfig();
    }

    /// <summary>
    /// 在Scene视图画网格线
    /// </summary>
    private void OnDrawGizmos()
    {
        if (gridUnitSize <= 0)
            return;

        float planeYOffset = gridUnitSize * 0.02f; // 略高于地面，减少与 Scene 地面重叠时的闪烁

        // 全局网格（未启用分区 或 与分区同时显示作参考）
        if (!useGridGroups && maxGridXZ >= minGridXZ && maxGridY >= minGridY)
            DrawGlobalGridGizmos(planeYOffset);

        // 分区网格：与下方可视化配置同一套逻辑
        if (useGridGroups && gridGroups != null)
        {
            foreach (var group in gridGroups)
            {
                group?.DrawGizmo(
                    gridUnitSize, showGridColor, showGridHeight, showYAxisColor,
                    gridColor, yAxisColor, planeYOffset,
                    heightLabelColor, heightLabelFontSize);
            }
        }
    }

    private void DrawGlobalGridGizmos(float planeYOffset)
    {
        GridGizmoDraw.DrawRegionBounds(
            minGridXZ, maxGridXZ, minGridY, maxGridY, minGridXZ, maxGridXZ,
            gridUnitSize, planeYOffset,
            showGridColor, showGridHeight, showYAxisColor,
            gridColor, yAxisColor,
            showGridHeight ? maxGridY - minGridY : 0,
            heightLabelColor, heightLabelFontSize);
    }
    #endregion

    #region 私有工具方法
    /// <summary>
    /// 校验配置合法性，避免错误配置导致逻辑异常
    /// </summary>
    private void ValidateConfig()
    {
        if (gridUnitSize <= 0)
        {
            Debug.LogError("[GridWorld] 网格单元大小不能≤0，已重置为1f", this);
            gridUnitSize = 1;
        }

        if (maxGridXZ < minGridXZ)
        {
            Debug.LogError("[GridWorld] X/Z最大边界不能小于最小边界，已交换值", this);
            (minGridXZ, maxGridXZ) = (maxGridXZ, minGridXZ);
        }

        if (maxGridY < minGridY)
        {
            Debug.LogError("[GridWorld] Y最大边界不能小于最小边界，已交换值", this);
            (minGridY, maxGridY) = (maxGridY, minGridY);
        }
    }
    #endregion
}

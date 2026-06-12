using Sirenix.OdinInspector;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 可放置分区：用「原点格 + 尺寸格」描述一个轴对齐长方体区域。
/// 格坐标边界为左闭右开 [origin, origin + size)，与 ValidBoundary / Contains 一致。
/// </summary>
[Serializable]
public class VirtualGridGroup
{
    [LabelText("编号")] public string id;
    [LabelText("允许放置")] public bool allowPlacement = true;

    /// <summary>分区最小角格坐标（含）。XZ 由 Scene 两点取格决定，Y 通常表示楼层/高度层。</summary>
    [LabelText("原点(格)")] public Vector3Int originCell;

    /// <summary>各轴占用格数，至少为 1。max = origin + size（不含）。</summary>
    [LabelText("尺寸(格)"), MinValue(1)] public Vector3Int sizeCells = Vector3Int.one;

    /// <summary>左闭右开边界，供 Contains 与 Gizmo 共用。</summary>
    public void GetGridBounds(out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ)
    {
        minX = originCell.x;
        maxX = originCell.x + Mathf.Max(1, sizeCells.x);
        minY = originCell.y;
        maxY = originCell.y + Mathf.Max(1, sizeCells.y);
        minZ = originCell.z;
        maxZ = originCell.z + Mathf.Max(1, sizeCells.z);
    }

    public bool Contains(Vector3Int gridPos)
    {
        GetGridBounds(out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ);
        return gridPos.x >= minX && gridPos.x < maxX
            && gridPos.y >= minY && gridPos.y < maxY
            && gridPos.z >= minZ && gridPos.z < maxZ;
    }

    public VirtualGridGroup Clone()
    {
        return new VirtualGridGroup
        {
            id = id,
            allowPlacement = allowPlacement,
            originCell = originCell,
            sizeCells = new Vector3Int(
                Mathf.Max(1, sizeCells.x),
                Mathf.Max(1, sizeCells.y),
                Mathf.Max(1, sizeCells.z)),
        };
    }

    public void DrawGizmo(
        int gridUnitSize,
        bool showGridColor,
        bool showGridHeight,
        bool showYAxisColor,
        Color gridColor,
        Color yAxisColor,
        float planeYOffset,
        Color heightLabelColor,
        int heightLabelFontSize)
    {
        GetGridBounds(out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ);
        GridGizmoDraw.DrawRegionBounds(
            minX, maxX, minY, maxY, minZ, maxZ,
            gridUnitSize, planeYOffset,
            showGridColor, showGridHeight, showYAxisColor,
            gridColor, yAxisColor,
            showGridHeight ? sizeCells.y : 0,
            heightLabelColor, heightLabelFontSize,
            id);
    }
}

/// <summary>
/// 分区/全局网格 Gizmo 线框（Editor Scene 视图）
/// </summary>
public static class GridGizmoDraw
{
    public static void DrawRegionBounds(
        int minX, int maxX, int minY, int maxY, int minZ, int maxZ,
        float unitSize, float planeYOffset,
        bool showGridColor, bool showGridHeight, bool showYAxisColor,
        Color gridColor, Color yAxisColor,
        int heightInCells,
        Color heightLabelColor,
        int heightLabelFontSize,
        string regionLabel = null)
    {
        if (unitSize <= 0 || maxX <= minX || maxZ <= minZ || maxY <= minY)
            return;

        if (showGridColor)
        {
            Gizmos.color = gridColor;
            if (showGridHeight)
            {
                // 立体线框：覆盖 origin.y 到 origin.y + size.y 整段高度
                var center = new Vector3(
                    (minX + maxX) * 0.5f * unitSize,
                    (minY + maxY) * 0.5f * unitSize,
                    (minZ + maxZ) * 0.5f * unitSize);
                var size = new Vector3(
                    (maxX - minX) * unitSize,
                    (maxY - minY) * unitSize,
                    (maxZ - minZ) * unitSize);
                Gizmos.DrawWireCube(center, size);
            }
            else
            {
                // 仅画底面矩形，Y 取 originCell.y 对应的世界高度
                DrawFloorRect(minX, maxX, minZ, maxZ, minY * unitSize + planeYOffset, unitSize);
            }
        }

        if (showGridHeight && showYAxisColor)
        {
            // 四角竖线，标出分区在 Y 方向的跨度
            Gizmos.color = yAxisColor;
            DrawCornerPillars(minX, maxX, minY, maxY, minZ, maxZ, unitSize);
        }

        if (showGridHeight && heightInCells > 0)
        {
            DrawHeightLabel(
                minX, maxX, minY, maxY, minZ, maxZ, unitSize,
                heightInCells, heightLabelColor, heightLabelFontSize, regionLabel);
        }
    }

#if UNITY_EDITOR
    private static GUIStyle heightLabelStyle;

    private static void DrawHeightLabel(
        int minX, int maxX, int minY, int maxY, int minZ, int maxZ,
        float unitSize, int heightInCells,
        Color color, int fontSize, string regionLabel)
    {
        var labelPos = new Vector3(
            (minX + maxX) * 0.5f * unitSize,
            maxY * unitSize + unitSize * 0.2f,
            maxZ * unitSize + unitSize * 0.15f);

        heightLabelStyle ??= new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
        heightLabelStyle.fontSize = fontSize;
        heightLabelStyle.normal.textColor = color;

        var text = string.IsNullOrWhiteSpace(regionLabel)
            ? heightInCells.ToString()
            : $"{regionLabel}\n{heightInCells}";

        Handles.Label(labelPos, text, heightLabelStyle);
    }
#endif

    private static void DrawFloorRect(int minX, int maxX, int minZ, int maxZ, float worldY, float unitSize)
    {
        float x0 = minX * unitSize;
        float x1 = maxX * unitSize;
        float z0 = minZ * unitSize;
        float z1 = maxZ * unitSize;

        Gizmos.DrawLine(new Vector3(x0, worldY, z0), new Vector3(x1, worldY, z0));
        Gizmos.DrawLine(new Vector3(x1, worldY, z0), new Vector3(x1, worldY, z1));
        Gizmos.DrawLine(new Vector3(x1, worldY, z1), new Vector3(x0, worldY, z1));
        Gizmos.DrawLine(new Vector3(x0, worldY, z1), new Vector3(x0, worldY, z0));
    }

    private static void DrawCornerPillars(
        int minX, int maxX, int minY, int maxY, int minZ, int maxZ, float unitSize)
    {
        float y0 = minY * unitSize;
        float y1 = maxY * unitSize;
        float x0 = minX * unitSize;
        float x1 = maxX * unitSize;
        float z0 = minZ * unitSize;
        float z1 = maxZ * unitSize;

        Gizmos.DrawLine(new Vector3(x0, y0, z0), new Vector3(x0, y1, z0));
        Gizmos.DrawLine(new Vector3(x1, y0, z0), new Vector3(x1, y1, z0));
        Gizmos.DrawLine(new Vector3(x0, y0, z1), new Vector3(x0, y1, z1));
        Gizmos.DrawLine(new Vector3(x1, y0, z1), new Vector3(x1, y1, z1));
    }
}

using UnityEngine;

namespace Mm_Budier.Editor
{
    /// <summary>
    /// Editor 专用：把 Scene 两次点击的格坐标写入分区。
    /// 默认只改 XZ，保留已有 originCell.y；两点 Y 不同时才重算高度层。
    /// </summary>
    internal static class VirtualGridGroupEditorUtility
    {
        public static void SetFromTwoCells(VirtualGridGroup group, Vector3Int cellA, Vector3Int cellB)
        {
            if (group == null)
                return;

            var min = Vector3Int.Min(cellA, cellB);
            var max = Vector3Int.Max(cellA, cellB);

            // XZ 取对角包围盒；Y 先沿用配置里的楼层，避免 Scene 取格误改高度
            group.originCell = new Vector3Int(min.x, group.originCell.y, min.z);

            // 两点不在同一 Y 层时，才用点击结果覆盖 origin.y 与 size.y
            if (cellA.y != cellB.y)
            {
                group.originCell = new Vector3Int(min.x, min.y, min.z);
                group.sizeCells.y = max.y - min.y + 1;
            }

            group.sizeCells.x = Mathf.Max(1, max.x - min.x + 1);
            group.sizeCells.z = Mathf.Max(1, max.z - min.z + 1);
            group.sizeCells.y = Mathf.Max(1, group.sizeCells.y);
        }
    }
}

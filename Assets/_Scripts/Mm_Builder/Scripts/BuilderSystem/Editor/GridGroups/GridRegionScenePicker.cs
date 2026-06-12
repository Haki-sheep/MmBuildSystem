using UnityEditor;
using UnityEngine;

namespace Mm_Budier.Editor
{
    /// <summary>
    /// Scene 视图两点取格：在水平面（Y = originCell.y × gridUnitSize）上射线求交，
    /// 不依赖 Ground Collider，楼层由 originCell.y 或第一次点击的格 Y 决定。
    /// </summary>
    internal static class GridRegionScenePicker
    {
        private static VirtualGridGroup targetGroup;
        private static int gridUnitSize = 1;
        private static int pickStep;
        private static Vector3Int firstCell;
        /// <summary>取格射线与之相交的水平面世界 Y</summary>
        private static float pickPlaneY;

        public static bool IsActive => targetGroup != null;

        public static void Begin(VirtualGridGroup group, int unitSize)
        {
            if (group == null)
                return;

            targetGroup = group;
            gridUnitSize = Mathf.Max(1, unitSize);
            pickStep = 0;
            // 取格前先定好楼层平面；后续第一次点击可能改 pickPlaneY
            pickPlaneY = group.originCell.y * gridUnitSize;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            SceneView.RepaintAll();
            Debug.Log("[GridGroups] Scene 取格：点击第一格（Esc 取消）");
        }

        public static void Cancel()
        {
            if (!IsActive)
                return;

            targetGroup = null;
            pickStep = 0;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.RepaintAll();
        }

        private static void OnSceneGUI(SceneView view)
        {
            if (!IsActive)
                return;

            Handles.BeginGUI();
            var rect = new Rect(10f, 10f, 400f, 24f);
            var msg = pickStep == 0
                ? "分区取格 1/2：点击 Scene 设置第一个角（Esc 取消）"
                : "分区取格 2/2：点击 Scene 设置对角（Esc 取消）";
            EditorGUI.HelpBox(rect, msg, MessageType.Info);
            Handles.EndGUI();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Cancel();
                Event.current.Use();
                return;
            }

            if (pickStep == 1)
            {
                if (TryPickCell(Event.current.mousePosition, out var hoverCell))
                    DrawPreviewRect(firstCell, hoverCell);
            }

            if (Event.current.type != EventType.MouseDown || Event.current.button != 0 || Event.current.alt)
                return;

            if (GUIUtility.hotControl != 0)
                return;

            if (!TryPickCell(Event.current.mousePosition, out var cell))
                return;

            Event.current.Use();

            if (pickStep == 0)
            {
                firstCell = cell;
                pickPlaneY = cell.y * gridUnitSize;
                pickStep = 1;
                Debug.Log($"[GridGroups] 第一格 {firstCell}，请点击对角");
            }
            else
            {
                VirtualGridGroupEditorUtility.SetFromTwoCells(targetGroup, firstCell, cell);
                Debug.Log($"[GridGroups] 完成 origin={targetGroup.originCell} size={targetGroup.sizeCells}");
                Cancel();
            }

            view.Repaint();
        }

        private static bool TryPickCell(Vector2 guiMouse, out Vector3Int cell)
        {
            cell = default;
            var ray = HandleUtility.GUIPointToWorldRay(guiMouse);
            var plane = new Plane(Vector3.up, new Vector3(0f, pickPlaneY, 0f));
            if (!plane.Raycast(ray, out var dist))
                return false;

            cell = BuilderVirtualGrid.WorldToCell(ray.GetPoint(dist), gridUnitSize);
            return true;
        }

        private static void DrawPreviewRect(Vector3Int a, Vector3Int b)
        {
            var min = Vector3Int.Min(a, b);
            var max = Vector3Int.Max(a, b);
            var unit = gridUnitSize;
            var y = min.y * unit + unit * 0.02f;

            Handles.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            var x0 = min.x * unit;
            var x1 = (max.x + 1) * unit; // 预览矩形右/后边对齐「不含」边界
            var z0 = min.z * unit;
            var z1 = (max.z + 1) * unit;

            Handles.DrawLine(new Vector3(x0, y, z0), new Vector3(x1, y, z0));
            Handles.DrawLine(new Vector3(x1, y, z0), new Vector3(x1, y, z1));
            Handles.DrawLine(new Vector3(x1, y, z1), new Vector3(x0, y, z1));
            Handles.DrawLine(new Vector3(x0, y, z1), new Vector3(x0, y, z0));
        }
    }
}

#if UNITY_EDITOR
using Mm_Budier.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>Inspector 中为每个 VirtualGridGroup 提供 Scene 两点取格入口。</summary>
public class VirtualGridGroupDrawer : OdinValueDrawer<VirtualGridGroup>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        CallNextDrawer(label);

        EditorGUILayout.Space(4f);
        if (GUILayout.Button("Scene 两点取格", GUILayout.Height(24f)))
        {
            GridRegionScenePicker.Begin(ValueEntry.SmartValue, BuilderVirtualGrid.EditorGridUnitSize);
        }

        EditorGUILayout.HelpBox("Scene 连续点击两格对角设定 XZ；高度用「尺寸 Y」。Esc 取消。", MessageType.None);
    }
}
#endif

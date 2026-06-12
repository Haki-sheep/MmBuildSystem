using Sirenix.OdinInspector;
using UnityEditor;

namespace Mm_Budier.Editor
{
    // 建造系统配置页
    public class BuilderSettingsPage
    {
        [InlineEditor(InlineEditorModes.FullEditor)]
        [HideLabel]
        [ShowInInspector]
        private BuilderSystemSetting config;

        public void LoadConfig()
        {
            config = AssetDatabase.LoadAssetAtPath<BuilderSystemSetting>(BuilderSystemSetting.AssetPath);
        }
    }
}

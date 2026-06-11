using Sirenix.OdinInspector;
using UnityEditor;

namespace Mm_Budier.Editor
{
    // 建造系统配置页
    public class BuilderSettingsPage
    {
        public const string ConfigAssetPath = "Assets/_Scripts/Mm_Builder/Scripts/Data/So/Config/BuiderRuntimeSettings.asset";

        [InlineEditor(InlineEditorModes.FullEditor)]
        [HideLabel]
        [ShowInInspector]
        private BuilderSystemSetting config;

        public void LoadConfig()
        {
            config = AssetDatabase.LoadAssetAtPath<BuilderSystemSetting>(ConfigAssetPath);
        }
    }
}

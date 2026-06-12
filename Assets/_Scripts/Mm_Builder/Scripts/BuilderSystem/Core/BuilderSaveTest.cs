using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 存档测试：挂到场景任意物体，Play 后在 Inspector 点按钮测试 Save / Load
    /// </summary>
    public class BuilderSaveTest : MonoBehaviour
    {
        [InfoBox("需在 Play 模式下使用。\n" +
                 "1. BuilderSystemSetting 里先「扫描并注册所有方块数据」\n" +
                 "2. Play → 摆几个方块 → 点【保存】\n" +
                 "3. 继续摆/拆几个 → 点【加载】→ 会清场并按存档还原\n" +
                 "4. 不 Stop Play 即可观察：加载会覆盖当前场景里的摆放状态")]
        [SerializeField]
        private BuilderSystem builderSystem;

        [ShowInInspector, ReadOnly, LabelText("存档路径")]
        private string SaveFilePathDisplay
        {
            get
            {
                if (!Application.isPlaying)
                    return "(进入 Play 后显示)";

                var setting = System?.builderSetting;
                if (setting == null)
                    return "(未绑定 BuilderSystemSetting)";

                var folder = string.IsNullOrWhiteSpace(setting.saveFolderName)
                    ? "BuilderSystemData"
                    : setting.saveFolderName;
                return Path.Combine(Application.persistentDataPath, folder, "build.json");
            }
        }

        private BuilderSystem System => builderSystem != null ? builderSystem : BuilderSystem.Instance;

        [Button("保存", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.55f)]
        private void TestSave()
        {
            if (!EnsurePlaying()) return;
            System.RegisterAllCubeData();
            System.SaveBuildData();
        }

        [Button("加载", ButtonSizes.Large), GUIColor(0.55f, 0.75f, 1f)]
        private void TestLoad()
        {
            if (!EnsurePlaying()) return;
            System.LoadBuildData();
        }

        [Button("注册方块反查表", ButtonSizes.Medium)]
        private void TestRegister()
        {
            if (!EnsurePlaying()) return;
            System.RegisterAllCubeData();
            Debug.Log("[SaveTest] 已从 allCubeDataList 注册到运行时反查表");
        }

        [Button("打开存档目录", ButtonSizes.Medium)]
        private void OpenSaveFolder()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[SaveTest] 请先进入 Play 模式");
                return;
            }

            var dir = Path.GetDirectoryName(SaveFilePathDisplay);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                Debug.LogWarning("[SaveTest] 存档目录尚不存在，请先保存一次");
                return;
            }

            Application.OpenURL(dir);
        }

        private bool EnsurePlaying()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[SaveTest] 请先进入 Play 模式");
                return false;
            }

            if (System == null)
            {
                Debug.LogError("[SaveTest] 找不到 BuilderSystem");
                return false;
            }

            if (System.builderSetting == null)
            {
                Debug.LogError("[SaveTest] BuilderSystem 未绑定 builderSetting");
                return false;
            }

            return true;
        }
    }
}

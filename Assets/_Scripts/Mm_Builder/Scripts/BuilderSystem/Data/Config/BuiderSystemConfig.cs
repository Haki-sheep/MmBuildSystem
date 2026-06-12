using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Mm_Budier
{
    [CreateAssetMenu(fileName = "BuilderSystemSetting", menuName = "Mm_Builder/BuilderSystemSetting")]
    public class BuilderSystemSetting : SerializedScriptableObject
    {
        public const string AssetPath = "Assets/_Scripts/Mm_Builder/Scripts/BuilderSystem/So/Config/BuiderRuntimeSettings.asset";

        private static BuilderSystemSetting instance;

        public static BuilderSystemSetting Instance
        {
            get
            {
                if (instance == null)
                {
#if UNITY_EDITOR
                    instance = UnityEditor.AssetDatabase.LoadAssetAtPath<BuilderSystemSetting>(AssetPath);
#endif
                }
                return instance;
            }
        }


        [Header("射线检测设置")] //此处可替换为其他检测方式 只需要提供相同参数即可
        [TitleGroup("射线检测设置"),LabelText("射线检测方块层级"), SerializeField]
        public LayerMask cubeLayer;
        [TitleGroup("射线检测设置"),LabelText("射线检测最大距离"), SerializeField]
        public float raycastMaxDistance;
        [TitleGroup("射线检测设置"),LabelText("地面层级"), SerializeField]
        public LayerMask groundLayer;
        [TitleGroup("射线检测设置"),LabelText("最大命中缓存"), SerializeField]
        public RaycastHit[] raycastHits = new RaycastHit[16];


        [Header("预览材质")]
        [TitleGroup("预览材质"),LabelText("可放置预览材质"), SerializeField]
        public Material preTrueMaterial;
        [TitleGroup("预览材质"),LabelText("不可放置预览材质"), SerializeField]
        public Material preFalseMaterial;

        [TitleGroup("数据保存设置")]
        [LabelText("存档子目录名（运行时拼到 persistentDataPath 下）"), SerializeField]
        public string saveFolderName = "BuilderSystemData";

        [TitleGroup("数据保存设置"), LabelText("所有方块数据"), SerializeField]
        public List<CubeData> allCubeDataList = new();

#if UNITY_EDITOR
        /// <summary>
        /// 扫描工程内所有 CubeData 资产 写入列表 供存读档反查预制体
        /// </summary>
        [TitleGroup("数据保存设置")]
        [Button("扫描并注册所有方块数据", ButtonSizes.Medium)]
        private void RegisterAllCubeData()
        {
            allCubeDataList.Clear();
            var seenTypes = new HashSet<ECubeType>();

            var guids = AssetDatabase.FindAssets("t:CubeData");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<CubeData>(path);
                if (data == null)
                    continue;

                if (!seenTypes.Add(data.CubeType))
                {
                    Debug.LogWarning($"[注册] 方块类型 {data.CubeType} 重复，已跳过：{path}");
                    continue;
                }

                allCubeDataList.Add(data);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"[注册] 完成，共注册 {allCubeDataList.Count} 种方块");
        }
#endif
    }
}

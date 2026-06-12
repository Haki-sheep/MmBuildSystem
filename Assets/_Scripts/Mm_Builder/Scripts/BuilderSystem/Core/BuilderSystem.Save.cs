using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>方块存档与分区 JSON 的路径约定（persistentDataPath 下）。</summary>
    public static class BuilderSavePaths
    {
        public const string DefaultFolderName = "BuilderSystemData";
        public const string BuildFileName = "build.json";
        public const string GridGroupsFileName = "grid-groups.json";

        public static string ResolveFolderName(string saveFolderName)
        {
            return string.IsNullOrWhiteSpace(saveFolderName) ? DefaultFolderName : saveFolderName;
        }

        public static string GetSaveDirectory(string saveFolderName)
        {
            return Path.Combine(Application.persistentDataPath, ResolveFolderName(saveFolderName));
        }

        public static string GetBuildFilePath(string saveFolderName)
        {
            return Path.Combine(GetSaveDirectory(saveFolderName), BuildFileName);
        }

        public static string GetGridGroupsFilePath(string saveFolderName)
        {
            return Path.Combine(GetSaveDirectory(saveFolderName), GridGroupsFileName);
        }
    }

    /// <summary>
    /// 存档结构体 
    /// </summary>
    [System.Serializable]
    public struct CubeSaveEntry
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ECubeType type;
        public int x;
        public int y;
        public int z;
        public int rotationSteps;
    }

    public partial class BuilderSystem
    {
        /// <summary>
        /// 方块数据字典
        /// </summary>
        private Dictionary<ECubeType, CubeData> cubeRegisteredDataDict = new();

        public void RegisterAllCubeData()
        {
            cubeRegisteredDataDict.Clear();
            foreach (var cubeData in builderSetting.allCubeDataList)
            {
                if (cubeData == null) continue;
                cubeRegisteredDataDict[cubeData.CubeType] = cubeData;
            }
        }

        public void RegisterCubeData(ECubeType cubeType, CubeData cubeData)
        {
            cubeRegisteredDataDict[cubeData.CubeType] = cubeData;
        }

        public void RemoveCubeData(ECubeType cubeType)
        {
            cubeRegisteredDataDict.Remove(cubeType);
        }

        private string SaveDirectory => BuilderSavePaths.GetSaveDirectory(builderSetting.saveFolderName);

        private string SaveFilePath => BuilderSavePaths.GetBuildFilePath(builderSetting.saveFolderName);

        /// <summary>
        /// 保存方块数据    
        /// </summary>
        public void SaveBuildData()
        {
            var seen = new HashSet<PlacedCube>();
            var entries = new List<CubeSaveEntry>();

            foreach (var placedCube in runtimeCubeDataDict.Values)
            {
                // 去重 比如一个方块占了多个格子 只保存一条数据
                if (!seen.Add(placedCube)) continue;

                // 保存数据
                entries.Add(new CubeSaveEntry
                {
                    type = placedCube.data.CubeType,
                    x = placedCube.origin.x,
                    y = placedCube.origin.y,
                    z = placedCube.origin.z,
                    rotationSteps = placedCube.rotationSteps,
                });
            }

            string json = JsonConvert.SerializeObject(entries,Formatting.Indented);
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"[Save] 已保存 {entries.Count} 个方块 -> {SaveFilePath}");
        }

        /// <summary>
        /// 加载方块数据
        /// </summary>
        public void LoadBuildData()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.LogWarning($"[Load] 没有存档文件 {SaveFilePath}");
                return;
            }

            string json = File.ReadAllText(SaveFilePath);
            var entries = JsonConvert.DeserializeObject<List<CubeSaveEntry>>(json);
            if (entries == null) return;

            // 反查表为空时先注册
            if (cubeRegisteredDataDict.Count == 0)
                RegisterAllCubeData();

            // 读档是整体还原 先清掉当前世界 避免占格冲突
            ClearAllCubes();

            int restored = 0;
            foreach (var e in entries)
            {
                var origin = new Vector3Int(e.x, e.y, e.z);

                // 用类型反查到 CubeData 拿到预制体
                if (!cubeRegisteredDataDict.TryGetValue(e.type, out var cubeData) || cubeData == null)
                {
                    Debug.LogWarning($"[Load] 找不到类型 {e.type} 对应的 CubeData，跳过 {origin}");
                    continue;
                }

                // 摆放方块到场景
                if (CubePlacementInfo.TryCreatePltInfo(origin, cubeData, virtualGrid, out var placement))
                {
                    HandlePlaceCube(placement, cubeData, e.rotationSteps);
                    restored++;
                }
            }

            Debug.Log($"[Load] 已还原 {restored} 个方块 <- {SaveFilePath}");
        }

        /// <summary>
        /// 清空当前所有已放置方块 销毁物体并清空字典
        /// </summary>
        private void ClearAllCubes()
        {
            var seen = new HashSet<PlacedCube>();
            foreach (var placedCube in runtimeCubeDataDict.Values)
            {
                // 多格方块共享同一实例 只销毁一次
                if (seen.Add(placedCube) && placedCube.spawnedObj != null)
                    Destroy(placedCube.spawnedObj);
            }
            runtimeCubeDataDict.Clear();
        }
    }
}

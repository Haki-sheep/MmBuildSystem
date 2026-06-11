using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Mm_Budier
{

    /// <summary>
    /// 存档结构体 
    /// </summary>
    [System.Serializable]
    public struct CubeSaveEntry
    {
        public ECubeType type;
        public int x;
        public int y;
        public int z;
    }

    public partial class BuilderSystem
    {
        /// <summary>
        /// 方块数据字典
        /// </summary>
        private Dictionary<ECubeType, CubeData> cubeRegisteredDataDict = new();

        public void RegisterAllCubeData()
        {
            foreach (var cubeData in builderSetting.allCubeDataDict)
            {
                RegisterCubeData(cubeData.Key, cubeData.Value);
            }
        }

        public void RegisterCubeData(ECubeType cubeType, CubeData cubeData)
        {
            cubeRegisteredDataDict.Add(cubeData.CubeType, cubeData);
        }

        public void RemoveCubeData(ECubeType cubeType)
        {
            cubeRegisteredDataDict.Remove(cubeType);
        }

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
                });
            }

            string json = JsonConvert.SerializeObject(entries);
            File.WriteAllText(builderSetting.SavePath, json);
            Debug.Log($"[Save] 已保存 {entries.Count} 个方块 -> {builderSetting.SavePath}");
        }

        /// <summary>
        /// 加载方块数据
        /// </summary>
        public void LoadBuildData()
        {
            if (!File.Exists(builderSetting.SavePath))
            {
                Debug.LogWarning($"[Load] 没有存档文件 {builderSetting.SavePath}");
                return;
            }

            string json = File.ReadAllText(builderSetting.SavePath);
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
                    HandlePlaceCube(placement, cubeData);
                    restored++;
                }
            }

            Debug.Log($"[Load] 已还原 {restored} 个方块 <- {builderSetting.SavePath}");
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

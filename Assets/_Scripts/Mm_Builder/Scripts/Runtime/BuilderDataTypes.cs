using System.Collections.Generic;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 一次放置的完整描述
    /// 此结构体用于计算放置信息 做上下文传递（临时、值类型、不可变）
    /// </summary>
    public readonly struct CubePlacementInfo
    {
        // 要放置的方块的占格起始格
        public Vector3Int OriginPoint { get; }
        // 逻辑占格尺寸
        public Vector3Int CubePrefabSize { get; }
        // 几何中心
        public Vector3 CubeWorldCenter { get; }
        // 世界包围盒
        public Bounds CubeWorldBounds { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="origin">放置起始格</param>
        /// <param name="cellSize">逻辑占格尺寸</param>
        /// <param name="gridUnitSize">网格单元大小</param>
        public CubePlacementInfo(Vector3Int origin, Vector3Int cellSize, float gridUnitSize)
        {
            OriginPoint = origin;
            CubePrefabSize = cellSize;

            float unitSize = gridUnitSize;
            // 动态计算包围盒大小和位置
            var minCorner = new Vector3(origin.x, origin.y, origin.z) * unitSize;
            var extent = new Vector3(cellSize.x, cellSize.y, cellSize.z) * unitSize;
            CubeWorldCenter = minCorner + extent * 0.5f;
            CubeWorldBounds = new Bounds(CubeWorldCenter, extent);
        }

        /// <summary>
        /// 由目标格构建放置描述
        /// </summary>
        /// <param name="targetPoint">占格起始格 射线算出的要放置的第一格</param>
        public static bool TryCreatePltInfo(Vector3Int targetPoint,
                                     CubeData cubeData,
                                     VirtualGrid grid,
                                     out CubePlacementInfo placement)
        {
            placement = default;
            if (cubeData?.CubePrefab == null || grid == null)
                return false;

            // 获取预制体占格尺寸
            var cellSize = cubeData.GetCubePrefabSizeInt();
            // targetPoint就是起始格 比如x维度 targetPoint=0 cellSize=2 那么将会占0和1两个格子
            placement = new CubePlacementInfo(targetPoint, cellSize, grid.gridUnitSize);
            return true;
        }


        /// <summary>
        /// 写入占用的所有格子
        /// </summary>
        public void FillOccupiedCells(List<Vector3Int> output)
        {
            output.Clear();
            for (int x = 0; x < CubePrefabSize.x; x++)
                for (int y = 0; y < CubePrefabSize.y; y++)
                    for (int z = 0; z < CubePrefabSize.z; z++)
                        output.Add(new Vector3Int(OriginPoint.x + x, OriginPoint.y + y, OriginPoint.z + z));
        }
    }

    /// <summary>
    /// 已放置的方块
    /// 本类作为一条数据 做持久化存储
    /// 只存起始格 origin，占用的所有格按 data 的尺寸现算（破坏/存档时）
    /// </summary>
    public class PlacedCube
    {
        public CubeData data;
        public GameObject spawnedObj;
        public Vector3Int origin;

        public PlacedCube(CubeData data, GameObject spawnedObj, Vector3Int origin)
        {
            this.data = data;
            this.spawnedObj = spawnedObj;
            this.origin = origin;
        }
    }

}

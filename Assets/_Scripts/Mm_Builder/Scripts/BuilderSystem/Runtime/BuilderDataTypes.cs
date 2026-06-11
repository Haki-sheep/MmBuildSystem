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
                                     BuilderVirtualGrid grid,
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
        public void FillOccupiedCells(List<Vector3Int> output, int rotationSteps = 0)
        {
            FillOccupiedCells(OriginPoint, CubePrefabSize, rotationSteps, output);
        }

        /// <summary>
        /// 按 Y 轴旋转步数(每步90°) 写入占用的所有格子
        /// </summary>
        public static void FillOccupiedCells(Vector3Int origin, Vector3Int cellSize, int rotationSteps, List<Vector3Int> output)
        {
            output.Clear();
            rotationSteps = ((rotationSteps % 4) + 4) % 4;

            for (int x = 0; x < cellSize.x; x++)
                for (int y = 0; y < cellSize.y; y++)
                    for (int z = 0; z < cellSize.z; z++)
                        output.Add(origin + RotateOffsetY(new Vector3Int(x, y, z), rotationSteps));
        }

        private static Vector3Int RotateOffsetY(Vector3Int local, int steps)
        {
            var v = local;
            for (int i = 0; i < steps; i++)
                v = new Vector3Int(v.z, v.y, -v.x);
            return v;
        }

        /// <summary>
        /// 根据旋转后的实际占格，计算世界空间几何中心
        /// </summary>
        public Vector3 GetWorldCenter(int rotationSteps, float gridUnitSize, List<Vector3Int> tempCells)
        {
            return GetWorldCenter(OriginPoint, CubePrefabSize, rotationSteps, gridUnitSize, tempCells);
        }

        public static Vector3 GetWorldCenter(
            Vector3Int origin,
            Vector3Int cellSize,
            int rotationSteps,
            float gridUnitSize,
            List<Vector3Int> tempCells)
        {
            GetWorldBounds(origin, cellSize, rotationSteps, gridUnitSize, tempCells, out var center, out _);
            return center;
        }

        /// <summary>
        /// 根据旋转后的实际占格，计算世界空间 AABB
        /// </summary>
        public Bounds GetWorldBounds(int rotationSteps, float gridUnitSize, List<Vector3Int> tempCells)
        {
            GetWorldBounds(OriginPoint, CubePrefabSize, rotationSteps, gridUnitSize, tempCells, out var center, out var size);
            return new Bounds(center, size);
        }

        public static void GetWorldBounds(
            Vector3Int origin,
            Vector3Int cellSize,
            int rotationSteps,
            float gridUnitSize,
            List<Vector3Int> tempCells,
            out Vector3 center,
            out Vector3 size)
        {
            FillOccupiedCells(origin, cellSize, rotationSteps, tempCells);

            int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue, maxZ = int.MinValue;
            foreach (var cell in tempCells)
            {
                if (cell.x < minX) minX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.z < minZ) minZ = cell.z;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y > maxY) maxY = cell.y;
                if (cell.z > maxZ) maxZ = cell.z;
            }

            var minWorld = new Vector3(minX, minY, minZ) * gridUnitSize;
            var maxWorld = new Vector3(maxX + 1, maxY + 1, maxZ + 1) * gridUnitSize;
            center = (minWorld + maxWorld) * 0.5f;
            size = maxWorld - minWorld;
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
        /// <summary> Y 轴旋转步数，每步 90° </summary>
        public int rotationSteps;

        public PlacedCube(CubeData data, GameObject spawnedObj, Vector3Int origin, int rotationSteps = 0)
        {
            this.data = data;
            this.spawnedObj = spawnedObj;
            this.origin = origin;
            this.rotationSteps = rotationSteps;
        }
    }

}

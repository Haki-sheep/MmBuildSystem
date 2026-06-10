using System.Collections.Generic;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 一次放置的完整描述
    /// targetCell为占格起始格 方块向正X正Y正Z延伸CellSize格
    /// 预制体pivot在几何中心时使用WorldCenter摆放
    /// </summary>
    public readonly struct CubePlacementInfo
    {
        // 要放置的方块的占格起始格
        public Vector3Int OriginPoint { get; }
        // 逻辑占格尺寸
        public Vector3Int CellSize { get; }
        // 世界几何中心 给Transform.position用 因为预制体pivot在中心
        public Vector3 WorldCenter { get; }
        // 世界包围盒
        public Bounds WorldBounds { get; }

        public bool IsUnit => CellSize == Vector3Int.one;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="origin">最小角</param>
        /// <param name="cellSize">逻辑占格尺寸</param>
        /// <param name="gridUnitSize">网格单元大小</param>
        public CubePlacementInfo(Vector3Int origin, Vector3Int cellSize, float gridUnitSize)
        {
            OriginPoint = origin;
            CellSize = cellSize;

            float unitSize = gridUnitSize;
            //占格区域左下角世界坐标
            var minCorner = new Vector3(origin.x, origin.y, origin.z) * unitSize;
            var extent = new Vector3(cellSize.x, cellSize.y, cellSize.z) * unitSize;
            //pivot在中心 位置要设在占格区域正中而不是左下角
            WorldCenter = minCorner + extent * 0.5f;
            WorldBounds = new Bounds(WorldCenter, extent);
        }

        /// <summary>
        /// 由目标格构建放置描述
        /// </summary>
        /// <param name="targetCell">占格起始格 射线算出的要放置的第一格</param>
        public static bool TryCreate(Vector3Int targetCell,
                                     CubeData cubeData,
                                     VirtualGrid grid,
                                     out CubePlacementInfo placement)
        {
            placement = default;
            if (cubeData?.CubePrefab == null || grid == null)
                return false;

            var cellSize = GetCellSize(cubeData);
            // targetCell就是起始格 比如X维 targetCell=0 cellSize=2 占0和1
            placement = new CubePlacementInfo(targetCell, cellSize, grid.gridUnitSize);
            return true;
        }

        /// <summary>
        /// 从预制体scale读取逻辑占格
        /// </summary>
        /// <param name="cubeData">方块数据</param>
        /// <returns>逻辑占格尺寸</returns>
        public static Vector3Int GetCellSize(CubeData cubeData)
        {
            var scale = cubeData.CubePrefab.transform.localScale;
            return new Vector3Int(
                Mathf.Max(1, Mathf.RoundToInt(scale.x)),
                Mathf.Max(1, Mathf.RoundToInt(scale.y)),
                Mathf.Max(1, Mathf.RoundToInt(scale.z)));
        }

        /// <summary>
        /// 写入占用的所有格子
        /// </summary>
        public void FillOccupiedCells(List<Vector3Int> output)
        {
            output.Clear();
            for (int x = 0; x < CellSize.x; x++)
                for (int y = 0; y < CellSize.y; y++)
                    for (int z = 0; z < CellSize.z; z++)
                        output.Add(new Vector3Int(OriginPoint.x + x, OriginPoint.y + y, OriginPoint.z + z));
        }
    }
}

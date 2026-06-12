using System;
using System.Collections.Generic;
using Mm_Budier;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mm_Budier
{

    [RequireComponent(typeof(BuilderVirtualGrid))]
    public partial class BuilderSystem : MonoBehaviour
    {
        [Header("必要组件")]
        [LabelText("虚拟网格组件")] public BuilderVirtualGrid virtualGrid;

        [LabelText("相机组件")] public Camera mainCamera;//做第一人称的话用这个
        [LabelText("玩家碰撞体组件")] public Collider playerCollider;

        [Header("场景根节点")]
        [LabelText("方块根节点")] public Transform cubeRoot;
        [LabelText("预览方块根节点")] public Transform preViewRoot;

        [LabelText("配置文件")]
        public BuilderSystemSetting builderSetting;

        [Header("输入")]
        [LabelText("逆时针旋转(90°)")] public Key rotateCounterClockwiseKey = Key.Q;
        [LabelText("顺时针旋转(90°)")] public Key rotateClockwiseKey = Key.E;

        [Header("Debug")]
        [LabelText("当前活跃方块类型")] public CubeData activeCubeData;

        /// <summary>
        /// 运行时状态：UI 打开时屏蔽射线检测与放置
        /// 由 UI 在开关时调用 SetUIOpen 维护
        /// </summary>
        [NonSerialized] public bool isUIOpen;
        public void SetUIOpen(bool open) => isUIOpen = open;

        /// <summary>
        /// 运行时总方块字典
        /// </summary>
        private Dictionary<Vector3Int, PlacedCube> runtimeCubeDataDict = new();

        /// <summary>
        /// 占格校验临时列表
        /// </summary>
        private readonly List<Vector3Int> occupiedList = new(8);

        /// <summary>
        /// 当前预览/放置的 Y 轴旋转步数（每步 90°）
        /// </summary>
        private int placementRotationSteps;


        /// <summary>
        /// 单例
        /// </summary>
        private static BuilderSystem instance;
        public static BuilderSystem Instance { get => instance; private set => instance = value; }

        // 放置偏移量 防止穿模
        public const float PLACEMENT_EPSILON = 0.01f;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            virtualGrid ??= GetComponent<BuilderVirtualGrid>();
            mainCamera ??= Camera.main;
        }

        void Start()
        {
            InitPreView();
        }

        void Update()
        {
            HandleBuildInteraction();
        }


        #region 公共函数
        /// <summary>
        /// 设置当前要放置的方块类型
        /// </summary>
        public void TryPlaceCube(CubeData cubeData)
        {
            if (activeCubeData != cubeData)
            {
                activeCubeData = cubeData;
                placementRotationSteps = 0;
            }
        }

        /// <summary>
        /// 每帧处理建造交互：拆除 / 旋转 / 预览 / 放置
        /// </summary>
        private void HandleBuildInteraction()
        {
            if (isUIOpen)
            {
                HidePreView();
                return;
            }

            if (!TryGetPlacementHit(out var hit))
            {
                HidePreView();
                return;
            }

            // 右键：拆除准心指向的方块（无需手持物品）
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                TryBreakAtHit(hit);
                return;
            }

            if (activeCubeData == null)
            {
                HidePreView();
                return;
            }

            HandleRotationInput();

            var targetCell = GetTargetCell(hit);
            if (!CubePlacementInfo.TryCreatePltInfo(targetCell, activeCubeData, virtualGrid, out var placement))
            {
                HidePreView();
                return;
            }

            bool canPlace = ValidatePlacement(placement, placementRotationSteps);
            HandlePreview(placement, activeCubeData, canPlace, placementRotationSteps);

            if (canPlace && Mouse.current.leftButton.wasPressedThisFrame)
                HandlePlaceCube(placement, activeCubeData, placementRotationSteps);
        }

        /// <summary>
        /// 破坏方块
        /// </summary>
        public void TryBreakCube(Vector3 worldPos)
        {
            var gridPos = virtualGrid.WorldToGrid(worldPos, clampToBounds: false);
            BreakCube(gridPos);
        }

        private void TryBreakAtHit(RaycastHit hit)
        {
            var eps = virtualGrid.gridUnitSize * PLACEMENT_EPSILON;
            // 向命中面内侧偏移，取到方块占格
            var insidePoint = hit.point - hit.normal * eps;
            var gridPos = virtualGrid.WorldToGrid(insidePoint, clampToBounds: false);
            BreakCube(gridPos);
        }

        #endregion


        #region 射线检测

        /// <summary>
        /// 从屏幕准心发射射线 忽略玩家自身碰撞体
        /// </summary>
        private bool TryGetPlacementHit(out RaycastHit hit)
        {
            hit = default;
            if (mainCamera == null)
                return false;

            // 从屏幕中心发射射线
            var ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
            var hits = builderSetting.raycastHits;
            var mask = builderSetting.groundLayer | builderSetting.cubeLayer;

            // 射线检测
            int hitCount = Physics.RaycastNonAlloc(ray,  // 射线
                                                   hits,  // 碰撞体数组
                                                   builderSetting.raycastMaxDistance,  // 最大距离
                                                   mask,  // 层掩码
                                                   QueryTriggerInteraction.Ignore);  // 触发器交互

            if (hitCount <= 0)
                return false;

            float closestDist = float.MaxValue;
            bool found = false;

            // 获取最近的碰撞体
            for (int i = 0; i < hitCount; i++)
            {
                var col = hits[i].collider;
                if (col == null || IsPlayerCollider(col))
                    continue;

                if (hits[i].distance < closestDist)
                {
                    closestDist = hits[i].distance;
                    hit = hits[i];
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// 判断是否是玩家碰撞体
        /// </summary>
        /// <param name="col">碰撞体</param>
        /// <returns>是否是玩家碰撞体</returns>
        private bool IsPlayerCollider(Collider col)
        {
            if (playerCollider == null)
                return false;

            var t = col.transform;
            return t == playerCollider.transform || t.IsChildOf(playerCollider.transform);
        }

        /// <summary>
        /// 命中面外侧空格 作为占格起始格
        /// </summary>
        private Vector3Int GetTargetCell(RaycastHit hit)
        {
            // 向外偏移 防止穿模 = 1* 0.01f
            var eps = virtualGrid.gridUnitSize * PLACEMENT_EPSILON;
            // 向命中点的法线反方向偏移一点点 防止抖动
            var hitPoint = hit.point - hit.normal * eps;
            // 世界转网格坐标
            var hitCell = virtualGrid.WorldToGrid(hitPoint, clampToBounds: false);
            // 网格坐标 + 法线方向(一般是单位向量) 得到目标格
            return hitCell + Vector3Int.RoundToInt(hit.normal);
        }

        #endregion

        #region 处理输入
        private void HandleRotationInput()
        {
            if (WasKeyPressed(rotateCounterClockwiseKey))
                placementRotationSteps = (placementRotationSteps + 3) % 4;
            else if (WasKeyPressed(rotateClockwiseKey))
                placementRotationSteps = (placementRotationSteps + 1) % 4;
        }

        private static bool WasKeyPressed(Key key)
        {
            if (Keyboard.current == null) return false;
            var control = Keyboard.current[key];
            return control != null && control.wasPressedThisFrame;
        }
        #endregion

        #region 放置销毁

        /// <summary>
        /// 校验占格边界 占用 玩家碰撞
        /// </summary>
        private bool ValidatePlacement(CubePlacementInfo placement, int rotationSteps)
        {
            placement.FillOccupiedCells(occupiedList, rotationSteps);

            // 遍历占格列表
            foreach (var cell in occupiedList)
            {
                //验证是否超出边界
                if (!virtualGrid.ValidPlacementCell(cell))
                    return false;

                //验证是否已有方块
                if (runtimeCubeDataDict.ContainsKey(cell))
                    return false;
            }

            //验证是否与玩家碰撞体重叠（按旋转后的实际占格算包围盒）
            if (playerCollider != null)
            {
                var bounds = placement.GetWorldBounds(rotationSteps, virtualGrid.gridUnitSize, occupiedList);
                if (playerCollider.bounds.Intersects(bounds))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 放置方块
        /// </summary>
        /// <param name="placement">放置信息</param>
        /// <param name="cubeData">方块数据</param>
        /// <returns>创建的方块物体</returns>
        private void HandlePlaceCube(CubePlacementInfo placement, CubeData cubeData, int rotationSteps)
        {
            var spawnedObj = TryCreatCube(placement, cubeData, rotationSteps);
            var runtimeData = new PlacedCube(cubeData, spawnedObj, placement.OriginPoint, rotationSteps);
            spawnedObj.GetComponent<CubeBehaviour>()?.OnPlaced(runtimeData);
            HandleDataToCache(placement, runtimeData, rotationSteps);
        }

        /// <summary>
        /// 创建方块
        /// </summary>
        /// <param name="placement">放置信息</param>
        /// <param name="cubeData">方块数据</param>
        /// <returns>创建的方块物体</returns>
        private GameObject TryCreatCube(CubePlacementInfo placement, CubeData cubeData, int rotationSteps)
        {
            var prefab = cubeData.CubePrefab;
            if (!prefab)
            {
                print("得不到预制体" + cubeData.CubePrefab);
                return null;
            }

            var rotation = Quaternion.Euler(0f, rotationSteps * 90f, 0f);
            var worldCenter = placement.GetWorldCenter(rotationSteps, virtualGrid.gridUnitSize, occupiedList);
            var spawnedObj = GameObject.Instantiate(
                prefab,
                worldCenter,
                rotation,
                cubeRoot);
            spawnedObj.layer = builderSetting.cubeLayer;
            return spawnedObj;
        }

        /// <summary>
        /// 保存数据到内存
        /// </summary>
        /// <param name="placement">放置信息</param>
        /// <param name="placedData">已放置方块记录</param>
        private void HandleDataToCache(CubePlacementInfo placement, PlacedCube placedData, int rotationSteps)
        {
            placement.FillOccupiedCells(occupiedList, rotationSteps);
            foreach (var pos in occupiedList)
                runtimeCubeDataDict.Add(pos, placedData);
        }

        /// <summary>
        /// 破坏方块
        /// </summary>
        private void BreakCube(Vector3Int gridPos)
        {
            if (!runtimeCubeDataDict.TryGetValue(gridPos, out var data)) return;

            data.spawnedObj.GetComponent<CubeBehaviour>()?.OnRemoved();

            //销毁方块物体
            if (data.spawnedObj != null) Destroy(data.spawnedObj);


            // 单位方块只占一格 直接移除
            if (data.data.IsUnit)
            {
                runtimeCubeDataDict.Remove(gridPos);
                return;
            }


            CubePlacementInfo.FillOccupiedCells(
                data.origin,
                data.data.GetCubePrefabSizeInt(),
                data.rotationSteps,
                occupiedList);
            foreach (var pos in occupiedList)
            {
                if (runtimeCubeDataDict.ContainsKey(pos)) runtimeCubeDataDict.Remove(pos);
            }
        }
        #endregion


    }
}

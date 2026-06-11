using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 预览模块 全局持有一个临时方块 动态切换网格材质位置旋转缩放
    /// </summary>
    public partial class BuilderSystem
    {
        //预览方块
        private GameObject preObj;
        //预览方块网格渲染器
        private MeshRenderer preMeshRenderer;
        //预览方块网格过滤器
        private MeshFilter preMeshFilter;
        //当前预览方块数据
        private CubeData currentPreCubeData;
        
        /// <summary>
        /// 初始化预览方块
        /// </summary>
        private void InitPreView()
        {
            //创建预览方块
            preObj = new GameObject("PreViewObj");
            preObj.transform.SetParent(preViewRoot, false);
            //添加网格过滤器和渲染器
            preMeshFilter = preObj.AddComponent<MeshFilter>();
            preMeshRenderer = preObj.AddComponent<MeshRenderer>();
            //禁用阴影投射与接收
            preMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            preMeshRenderer.receiveShadows = false;

            if (builderSetting?.preTrueMaterial != null)
                preMeshRenderer.sharedMaterial = builderSetting.preTrueMaterial;

            preObj.SetActive(false);
        }

        /// <summary>
        /// 隐藏预览方块
        /// </summary>
        private void HidePreView()
        {
            if (preObj != null)
                preObj.SetActive(false);

            currentPreCubeData = null;
        }

        /// <summary>
        /// 更新预览方块
        /// </summary>
        private void HandlePreview(CubePlacementInfo placement, CubeData cubeData, bool canPlace, int rotationSteps)
        {
            if (cubeData?.CubePrefab == null || builderSetting == null)
            {
                HidePreView();
                return;
            }

            //获取源网格过滤器
            var srcFilter = cubeData.CubePrefab.GetComponent<MeshFilter>();
            if (srcFilter == null)
            {
                HidePreView();
                return;
            }

            preObj.SetActive(true);

            //方块类型变化时更新网格
            if (currentPreCubeData != cubeData)
            {
                currentPreCubeData = cubeData;
                preMeshFilter.sharedMesh = srcFilter.sharedMesh;
            }

            // 位置按旋转后的实际占格中心对齐，避免绕未旋转中心转导致歪出网格
            preObj.transform.position = placement.GetWorldCenter(rotationSteps, virtualGrid.gridUnitSize, occupiedList);
            preObj.transform.rotation = Quaternion.Euler(0f, rotationSteps * 90f, 0f);
            preObj.transform.localScale = cubeData.CubePrefab.transform.localScale;

            //按能否放置切换预览材质
            preMeshRenderer.sharedMaterial = canPlace ? builderSetting.preTrueMaterial : builderSetting.preFalseMaterial;
        }
    }
}

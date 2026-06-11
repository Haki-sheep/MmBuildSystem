using UnityEngine;
using Sirenix.OdinInspector;

namespace Mm_Budier
{


    [CreateAssetMenu(fileName = "NewCube", menuName = "Mm_Builder/CubeData")]
    public class CubeData : SerializedScriptableObject
    {
        [LabelText("类型")]
        public ECubeType CubeType;
        [LabelText("预制体")]
        public GameObject CubePrefab;
        public bool IsUnit => GetCubePrefabSizeInt() == Vector3Int.one;

        /// <summary>
        /// 获取预制体占格尺寸(整数)
        /// </summary>
        /// <returns>占格尺寸</returns>
        public Vector3Int GetCubePrefabSizeInt()
        {
            return new Vector3Int(
             Mathf.Max(1, Mathf.RoundToInt(CubePrefab.transform.localScale.x)),
             Mathf.Max(1, Mathf.RoundToInt(CubePrefab.transform.localScale.y)),
             Mathf.Max(1, Mathf.RoundToInt(CubePrefab.transform.localScale.z)));
        }

    }

}

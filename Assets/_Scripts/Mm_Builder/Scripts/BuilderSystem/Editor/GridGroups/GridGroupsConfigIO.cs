using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mm_Budier.Editor
{
    /// <summary>
    /// 分区 grid-groups.json 读写，以及场景 BuilderVirtualGrid 与页签数据之间的拷贝。
    /// </summary>
    internal static class GridGroupsConfigIO
    {
        [Serializable]
        private class GridGroupsFile
        {
            public bool useGridGroups;
            public List<VirtualGridGroup> groups = new();
        }

        public static string GetFilePath()
        {
            var setting = BuilderSystemSetting.Instance;
            var folderName = setting != null ? setting.saveFolderName : BuilderSavePaths.DefaultFolderName;
            return BuilderSavePaths.GetGridGroupsFilePath(folderName);
        }

        public static (bool useGridGroups, List<VirtualGridGroup> groups) Capture(BuilderVirtualGrid grid)
        {
            var groups = new List<VirtualGridGroup>();
            if (grid?.gridGroups != null)
            {
                foreach (var group in grid.gridGroups)
                {
                    if (group != null)
                        groups.Add(group.Clone());
                }
            }

            return (grid != null && grid.useGridGroups, groups);
        }

        public static void Apply(BuilderVirtualGrid grid, bool useGridGroups, List<VirtualGridGroup> groups)
        {
            if (grid == null)
                return;

            grid.useGridGroups = useGridGroups;
            grid.gridGroups ??= new List<VirtualGridGroup>();
            grid.gridGroups.Clear();

            if (groups == null)
                return;

            foreach (var group in groups)
            {
                if (group != null)
                    grid.gridGroups.Add(group.Clone());
            }
        }

        public static BuilderVirtualGrid FindFirstVirtualGridInActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return null;

            foreach (var root in scene.GetRootGameObjects())
            {
                var grid = root.GetComponentInChildren<BuilderVirtualGrid>(true);
                if (grid != null)
                    return grid;
            }

            return null;
        }

        public static bool TryLoad(out bool useGridGroups, out List<VirtualGridGroup> groups, out string error)
        {
            useGridGroups = false;
            groups = new List<VirtualGridGroup>();
            error = null;
            var path = GetFilePath();

            if (!File.Exists(path))
            {
                error = $"文件不存在：{path}";
                return false;
            }

            try
            {
                var file = JsonConvert.DeserializeObject<GridGroupsFile>(File.ReadAllText(path));
                if (file == null)
                {
                    error = "JSON 解析结果为空";
                    return false;
                }

                useGridGroups = file.useGridGroups;
                groups = file.groups ?? new List<VirtualGridGroup>();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TrySave(bool useGridGroups, List<VirtualGridGroup> groups, out string error)
        {
            error = null;
            var path = GetFilePath();
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var file = new GridGroupsFile
                {
                    useGridGroups = useGridGroups,
                    groups = groups ?? new List<VirtualGridGroup>(),
                };
                File.WriteAllText(path, JsonConvert.SerializeObject(file, Formatting.Indented));
                Debug.Log($"[GridGroups] 已保存 {file.groups.Count} 个分区 -> {path}");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static void RevealInExplorer()
        {
            var path = GetFilePath();
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
                return;

            Directory.CreateDirectory(directory);
            EditorUtility.RevealInFinder(path);
        }
    }
}

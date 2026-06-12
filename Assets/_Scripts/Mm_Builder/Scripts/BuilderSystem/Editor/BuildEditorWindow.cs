using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Mm_Budier.Editor
{
    public class BuildEditorWindow : OdinMenuEditorWindow
    {
        private BuilderEditorSettings settings;
        private EnumManagerPage enumManagerPage;
        private BuilderSettingsPage builderSettingsPage;
        private GridGroupsManagerPage gridGroupsManagerPage;

        [MenuItem("Tools/MmBuilderSsytem/BuildEditorWindow")]
        private static void Open()
        {
            var window = GetWindow<BuildEditorWindow>();
            window.titleContent = new GUIContent("Mm Builder");
            window.minSize = new Vector2(900f, 560f);
            window.Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureInitialized();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            EnsureInitialized();

            var tree = new OdinMenuTree(supportsMultiSelect: false)
            {
                DefaultMenuStyle = { IconSize = 24f },
                Config =
                {
                    DrawSearchToolbar = true,
                    SearchToolbarHeight = 28,
                },
            };

            tree.Add("枚举管理器", enumManagerPage, EditorIcons.HamburgerMenu);
            BuildEnumEntryMenu(tree);
            tree.Add("建造系统设置", builderSettingsPage, EditorIcons.SettingsCog);
            tree.Add("分区管理", gridGroupsManagerPage, EditorIcons.GridBlocks);

            tree.EnumerateTree()
                .Where(x => x.Value is CubeTypeGroupPage)
                .AddIcons(EditorIcons.Folder);

            tree.EnumerateTree()
                .Where(x => x.Value is ECubeTypeEntryPage)
                .AddIcons(EditorIcons.Tag);

            return tree;
        }

        protected override void OnBeginDrawEditors()
        {
            var selected = MenuTree?.Selection.FirstOrDefault();
            if (selected == null)
                return;

            SirenixEditorGUI.BeginHorizontalToolbar();
            {
                GUILayout.Label(selected.Name, SirenixGUIStyles.BoldLabel);
                GUILayout.FlexibleSpace();

                if (SirenixEditorGUI.ToolbarButton("导入"))
                    enumManagerPage.ImportFromEnumFile();

                if (SirenixEditorGUI.ToolbarButton("生成"))
                    enumManagerPage.GenerateAll();
            }
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        public new void ForceMenuTreeRebuild() => base.ForceMenuTreeRebuild();

        public void SelectEntry(CubeTypeEntry target) => ReselectEntry(target);

        private void EnsureInitialized()
        {
            settings = EditorSettingsUtility.LoadOrCreate();
            enumManagerPage ??= new EnumManagerPage();
            builderSettingsPage ??= new BuilderSettingsPage();
            gridGroupsManagerPage ??= new GridGroupsManagerPage();
            enumManagerPage.Bind(this, settings);
            builderSettingsPage.LoadConfig();
            gridGroupsManagerPage.Refresh();
        }

        private void BuildEnumEntryMenu(OdinMenuTree tree)
        {
            if (settings == null)
                return;

            foreach (var group in EditorSettingsUtility.GetMenuGroups(settings))
            {
                tree.Add($"枚举管理器/条目/{group}", new CubeTypeGroupPage
                {
                    Settings = settings,
                    GroupName = group,
                });
            }

            if (settings.Entries == null)
                return;

            foreach (var entry in settings.Entries)
            {
                var page = new ECubeTypeEntryPage
                {
                    Settings = settings,
                    Entry = entry,
                    OnMenuStructureChanged = ReselectEntry,
                };

                var label = string.IsNullOrWhiteSpace(entry.Name) ? "未命名" : entry.Name;
                var menuPath = string.IsNullOrWhiteSpace(entry.Group)
                    ? $"枚举管理器/条目/{label}"
                    : $"枚举管理器/条目/{entry.Group}/{label}";

                tree.Add(menuPath, page);
            }
        }

        private void ReselectEntry(CubeTypeEntry target)
        {
            ForceMenuTreeRebuild();

            if (target == null)
            {
                if (settings?.Entries is { Count: > 0 })
                    target = settings.Entries[0];
                else
                {
                    TrySelectMenuItemWithObject(enumManagerPage);
                    return;
                }
            }

            var item = MenuTree.EnumerateTree()
                .FirstOrDefault(x => x.Value is ECubeTypeEntryPage p && p.Entry == target);

            if (item == null)
            {
                TrySelectMenuItemWithObject(enumManagerPage);
                return;
            }

            MenuTree.Selection.Clear();
            MenuTree.Selection.Add(item);
        }
    }
}

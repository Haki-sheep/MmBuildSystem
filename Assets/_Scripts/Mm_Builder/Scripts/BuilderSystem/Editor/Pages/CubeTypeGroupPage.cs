using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_Budier.Editor
{
    public class CubeTypeGroupPage
    {
        [HideInInspector] public BuilderEditorSettings Settings;
        [HideInInspector] public string GroupName;

        [ShowInInspector, ReadOnly, HideLabel]
        [Title("@GroupName", "@GetSubtitle()", TitleAlignments.Centered)]
        [MultiLineProperty(4)]
        private string Info => "将条目的「分类」设为此项后，条目会显示在左侧此分类下。";

        private string GetSubtitle()
        {
            var count = Settings?.Entries?.Count(e => e.Group == GroupName) ?? 0;
            return count > 0 ? $"{count} 个条目" : "暂无条目";
        }
    }
}

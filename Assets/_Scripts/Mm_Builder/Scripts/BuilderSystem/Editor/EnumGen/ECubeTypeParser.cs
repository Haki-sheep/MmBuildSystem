using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Mm_Budier.Editor
{
    // 从 ECubeType cs 解析枚举条目
    internal static class ECubeTypeParser
    {
        private static readonly Regex MemberRegex = new(
            @"^(\w+)\s*(?:=\s*(\d+))?\s*,\s*(?://\s*(.*))?$",
            RegexOptions.Compiled);

        public static bool TryImportFromFile(string filePath, out List<CubeTypeEntry> entries, out string error)
        {
            entries = new List<CubeTypeEntry>();
            error = null;

            if (!File.Exists(filePath))
            {
                error = $"文件不存在 {filePath}";
                return false;
            }

            entries = Parse(File.ReadAllText(filePath));
            if (entries.Count == 0)
            {
                error = "未能解析出枚举条目";
                return false;
            }

            return true;
        }

        public static List<CubeTypeEntry> Parse(string content)
        {
            var entries = new List<CubeTypeEntry>();
            var currentGroup = string.Empty;
            var autoValue = 0;

            foreach (var rawLine in content.Split('\n'))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith("//") && !line.Contains(","))
                {
                    currentGroup = line[2..].Trim();
                    continue;
                }

                var match = MemberRegex.Match(line);
                if (!match.Success)
                    continue;

                var name = match.Groups[1].Value;
                var value = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : autoValue;

                entries.Add(new CubeTypeEntry
                {
                    Name = name,
                    Value = value,
                    Comment = match.Groups[3].Success ? match.Groups[3].Value.Trim() : string.Empty,
                    Group = currentGroup,
                    CreateCubeData = name != "Air",
                    CubeAssetName = name,
                });

                autoValue = value + 1;
            }

            return entries;
        }
    }
}

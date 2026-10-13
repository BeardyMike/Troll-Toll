using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;

namespace TrollToll
{
    public static class CreatureManager
    {
        public static ConfigEntry<string> IgnoredCreatures;
        public static ConfigEntry<string> EnforcedThreatCreatures;
        public static ConfigEntry<string> CustomCreatureRadii;
        public static ConfigEntry<bool> BossesAlwaysThreat;
        public static ConfigEntry<bool> IgnoreBoars;
        public static ConfigEntry<bool> IgnoreNecks;
        public static ConfigEntry<bool> IgnoreGreylings;
        public static ConfigEntry<bool> IgnoreBats;
        public static ConfigEntry<bool> IgnoreSkeletons;

        private static readonly HashSet<string> _parsedIgnored = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _parsedEnforced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, float> _parsedCustomRadii = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public static float MaxCustomRadius { get; private set; } = 30f;

        public static void Initialize(ConfigFile config)
        {
            IgnoredCreatures = config.Bind("6 - Creature Controls", "IgnoredCreatures", "Deer, Seagull, Crow",
                "Comma-separated list of creature names or prefabs that NEVER trigger threat or consume combat stamina (e.g. 'Greyling, Neck, Boar, Skeleton'). Matches display name or prefab name.");

            EnforcedThreatCreatures = config.Bind("6 - Creature Controls", "EnforcedThreatCreatures", "Troll, Abomination, StoneGolem, Gjall, SeekerSoldier",
                "Comma-separated list of creature names or prefabs that ALWAYS trigger threat when within range, regardless of alert status (e.g. 'Troll, Abomination, Gjall').");

            CustomCreatureRadii = config.Bind("6 - Creature Controls", "CustomCreatureRadii", "Troll:50, Abomination:40, Gjall:50",
                "Creature-specific threat detection radius in 'CreatureName:Distance' format (e.g. 'Troll:50, Greyling:10'). Overrides biome and global detection radius.");

            BossesAlwaysThreat = config.Bind("6 - Creature Controls", "BossesAlwaysThreat", true,
                "If true, any active boss in detection range is always treated as an active threat.");

            IgnoreBoars = config.Bind("6 - Creature Controls", "IgnoreBoars", false,
                "Quick toggle: If true, wild Boars never trigger threat or interrupt free stamina.");

            IgnoreNecks = config.Bind("6 - Creature Controls", "IgnoreNecks", false,
                "Quick toggle: If true, Necks never trigger threat or interrupt free stamina.");

            IgnoreGreylings = config.Bind("6 - Creature Controls", "IgnoreGreylings", false,
                "Quick toggle: If true, Greylings never trigger threat or interrupt free stamina.");

            IgnoreBats = config.Bind("6 - Creature Controls", "IgnoreBats", false,
                "Quick toggle: If true, Bats never trigger threat or interrupt free stamina.");

            IgnoreSkeletons = config.Bind("6 - Creature Controls", "IgnoreSkeletons", false,
                "Quick toggle: If true, Skeletons never trigger threat or interrupt free stamina.");

            IgnoredCreatures.SettingChanged += (s, e) => RefreshCache();
            EnforcedThreatCreatures.SettingChanged += (s, e) => RefreshCache();
            CustomCreatureRadii.SettingChanged += (s, e) => RefreshCache();

            RefreshCache();
        }

        public static void RefreshCache()
        {
            _parsedIgnored.Clear();
            if (!string.IsNullOrWhiteSpace(IgnoredCreatures.Value))
            {
                string[] parts = IgnoredCreatures.Value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    string clean = CleanName(part);
                    if (!string.IsNullOrEmpty(clean))
                    {
                        _parsedIgnored.Add(clean);
                    }
                }
            }

            _parsedEnforced.Clear();
            if (!string.IsNullOrWhiteSpace(EnforcedThreatCreatures.Value))
            {
                string[] parts = EnforcedThreatCreatures.Value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    string clean = CleanName(part);
                    if (!string.IsNullOrEmpty(clean))
                    {
                        _parsedEnforced.Add(clean);
                    }
                }
            }

            _parsedCustomRadii.Clear();
            float maxR = 30f;
            if (!string.IsNullOrWhiteSpace(CustomCreatureRadii.Value))
            {
                string[] parts = CustomCreatureRadii.Value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    string[] sub = part.Split(':');
                    if (sub.Length == 2 && float.TryParse(sub[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r))
                    {
                        string clean = CleanName(sub[0]);
                        if (!string.IsNullOrEmpty(clean))
                        {
                            _parsedCustomRadii[clean] = r;
                            if (r > maxR) maxR = r;
                        }
                    }
                }
            }
            MaxCustomRadius = maxR;
        }

        public static string CleanName(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }
            return sb.ToString();
        }

        public static List<string> GetCandidateNames(Character character)
        {
            var list = new List<string>(4);
            if (character == null) return list;

            // 1. Prefab name
            if (character.gameObject != null)
            {
                string prefab = global::Utils.GetPrefabName(character.gameObject);
                if (!string.IsNullOrEmpty(prefab))
                {
                    list.Add(CleanName(prefab));
                }
            }

            // 2. Hover name
            try
            {
                string hover = character.GetHoverName();
                if (!string.IsNullOrEmpty(hover))
                {
                    list.Add(CleanName(hover));
                }
            }
            catch { }

            // 3. m_name
            if (!string.IsNullOrEmpty(character.m_name))
            {
                string internalName = CleanName(character.m_name);
                list.Add(internalName);
                if (internalName.StartsWith("enemy"))
                {
                    list.Add(internalName.Substring(5));
                }
            }

            // 4. GameObject name stripped of (Clone)
            if (character.gameObject != null)
            {
                string goName = character.gameObject.name.Replace("(Clone)", "");
                list.Add(CleanName(goName));
            }

            return list;
        }

        public static bool IsCreatureIgnored(Character character)
        {
            if (character == null) return false;

            var candidates = GetCandidateNames(character);

            // Check quick toggles
            if (IgnoreBoars.Value && candidates.Contains("boar")) return true;
            if (IgnoreNecks.Value && candidates.Contains("neck")) return true;
            if (IgnoreGreylings.Value && candidates.Contains("greyling")) return true;
            if (IgnoreBats.Value && candidates.Contains("bat")) return true;
            if (IgnoreSkeletons.Value && candidates.Contains("skeleton")) return true;

            // Check configured ignored list
            for (int i = 0; i < candidates.Count; i++)
            {
                if (_parsedIgnored.Contains(candidates[i]))
                    return true;
            }

            return false;
        }

        public static bool IsCreatureEnforcedThreat(Character character)
        {
            if (character == null) return false;

            if (BossesAlwaysThreat.Value && character.IsBoss())
                return true;

            var candidates = GetCandidateNames(character);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (_parsedEnforced.Contains(candidates[i]))
                    return true;
            }

            return false;
        }

        public static float GetCustomRadius(Character character, float defaultRadius)
        {
            if (character == null) return defaultRadius;

            var candidates = GetCandidateNames(character);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (_parsedCustomRadii.TryGetValue(candidates[i], out float radius))
                    return radius;
            }

            return defaultRadius;
        }
    }
}

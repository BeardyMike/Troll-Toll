using System.Collections.Generic;
using BepInEx.Configuration;

namespace TrollToll
{
    public static class BiomeManager
    {
        public class BiomeSetting
        {
            public ConfigEntry<bool> Enabled;
            public ConfigEntry<float> DetectionRadius;
        }

        private static readonly Dictionary<Heightmap.Biome, BiomeSetting> _biomeSettings = new Dictionary<Heightmap.Biome, BiomeSetting>();

        public static void Initialize(ConfigFile config)
        {
            _biomeSettings.Clear();

            RegisterBiome(config, Heightmap.Biome.Meadows, "Meadows", 30f);
            RegisterBiome(config, Heightmap.Biome.BlackForest, "BlackForest", 30f);
            RegisterBiome(config, Heightmap.Biome.Swamp, "Swamp", 30f);
            RegisterBiome(config, Heightmap.Biome.Mountain, "Mountain", 30f);
            RegisterBiome(config, Heightmap.Biome.Plains, "Plains", 30f);
            RegisterBiome(config, Heightmap.Biome.Mistlands, "Mistlands", 30f);
            RegisterBiome(config, Heightmap.Biome.AshLands, "Ashlands", 30f);
            RegisterBiome(config, Heightmap.Biome.DeepNorth, "DeepNorth", 30f);
            RegisterBiome(config, Heightmap.Biome.Ocean, "Ocean", 30f);
        }

        private static void RegisterBiome(ConfigFile config, Heightmap.Biome biome, string displayName, float defaultRadius)
        {
            var setting = new BiomeSetting
            {
                Enabled = config.Bind("5 - Biome Controls", $"{displayName}_Enabled", true,
                    $"Enable Troll-Toll stamina mechanics in the {displayName} biome."),
                DetectionRadius = config.Bind("5 - Biome Controls", $"{displayName}_DetectionRadius", defaultRadius,
                    $"Threat detection radius in meters for the {displayName} biome. Set to -1 to use global DetectionRadius.")
            };

            _biomeSettings[biome] = setting;
        }

        public static bool IsBiomeModEnabled(Heightmap.Biome biome)
        {
            biome = GetPrimaryBiome(biome);
            if (_biomeSettings.TryGetValue(biome, out var setting))
            {
                return setting.Enabled.Value;
            }
            return true;
        }

        public static float GetBiomeDetectionRadius(Heightmap.Biome biome)
        {
            biome = GetPrimaryBiome(biome);
            if (_biomeSettings.TryGetValue(biome, out var setting))
            {
                float radius = setting.DetectionRadius.Value;
                if (radius > 0f)
                {
                    return radius;
                }
            }
            return TrollTollPlugin.DetectionRadius.Value;
        }

        /// <summary>
        /// Converts potential composite flags into the primary underlying biome.
        /// </summary>
        public static Heightmap.Biome GetPrimaryBiome(Heightmap.Biome biome)
        {
            if (_biomeSettings.ContainsKey(biome))
                return biome;

            // Ordered from latest / specific biomes down to common biomes
            if ((biome & Heightmap.Biome.AshLands) != 0) return Heightmap.Biome.AshLands;
            if ((biome & Heightmap.Biome.Mistlands) != 0) return Heightmap.Biome.Mistlands;
            if ((biome & Heightmap.Biome.DeepNorth) != 0) return Heightmap.Biome.DeepNorth;
            if ((biome & Heightmap.Biome.Plains) != 0) return Heightmap.Biome.Plains;
            if ((biome & Heightmap.Biome.Mountain) != 0) return Heightmap.Biome.Mountain;
            if ((biome & Heightmap.Biome.Swamp) != 0) return Heightmap.Biome.Swamp;
            if ((biome & Heightmap.Biome.BlackForest) != 0) return Heightmap.Biome.BlackForest;
            if ((biome & Heightmap.Biome.Meadows) != 0) return Heightmap.Biome.Meadows;
            if ((biome & Heightmap.Biome.Ocean) != 0) return Heightmap.Biome.Ocean;

            return Heightmap.Biome.None;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace TrollToll
{
    public class BossInfo
    {
        public Heightmap.Biome Biome { get; }
        public string BossName { get; }
        public string GlobalKey { get; }
        public int Tier { get; }

        public BossInfo(Heightmap.Biome biome, string bossName, string globalKey, int tier)
        {
            Biome = biome;
            BossName = bossName;
            GlobalKey = globalKey;
            Tier = tier;
        }
    }

    public static class BossReliefManager
    {
        public static readonly List<BossInfo> Bosses = new List<BossInfo>
        {
            new BossInfo(Heightmap.Biome.Meadows, "Eikthyr", "defeated_eikthyr", 1),
            new BossInfo(Heightmap.Biome.BlackForest, "The Elder", "defeated_gdking", 2),
            new BossInfo(Heightmap.Biome.Swamp, "Bonemass", "defeated_bonemass", 3),
            new BossInfo(Heightmap.Biome.Mountain, "Moder", "defeated_dragon", 4),
            new BossInfo(Heightmap.Biome.Plains, "Yagluth", "defeated_goblinking", 5),
            new BossInfo(Heightmap.Biome.Mistlands, "The Queen", "defeated_queen", 6),
            new BossInfo(Heightmap.Biome.AshLands, "Fader", "defeated_fader", 7)
        };

        private static readonly Dictionary<Heightmap.Biome, BossInfo> _biomeBossLookup = new Dictionary<Heightmap.Biome, BossInfo>();

        static BossReliefManager()
        {
            foreach (var boss in Bosses)
            {
                _biomeBossLookup[boss.Biome] = boss;
            }
        }

        public static BossInfo GetBossForBiome(Heightmap.Biome biome)
        {
            // Normalize biome if composite flag
            biome = BiomeManager.GetPrimaryBiome(biome);
            if (_biomeBossLookup.TryGetValue(biome, out var boss))
            {
                return boss;
            }
            return null;
        }

        /// <summary>
        /// Checks if a global key has been set in the current world (e.g. boss defeated).
        /// </summary>
        public static bool IsBossDefeated(string globalKey)
        {
            if (string.IsNullOrEmpty(globalKey) || ZoneSystem.instance == null)
                return false;

            return ZoneSystem.instance.GetGlobalKey(globalKey);
        }

        /// <summary>
        /// Returns total number of known progression bosses defeated in the world.
        /// </summary>
        public static int GetTotalDefeatedBossCount()
        {
            int count = 0;
            for (int i = 0; i < Bosses.Count; i++)
            {
                if (IsBossDefeated(Bosses[i].GlobalKey))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Calculates the stamina relief fraction [0.0 to 1.0] for the specified biome.
        /// Defeating this biome's boss unlocks base relief (default 25%), and each additional
        /// boss defeated beyond that biome grants further relief (default +25% per additional boss),
        /// granting 100% relief after 4 bosses for Meadows.
        /// </summary>
        public static float GetReliefFraction(Heightmap.Biome biome)
        {
            if (!TrollTollPlugin.BossReliefEnabled.Value)
                return 0f;

            biome = BiomeManager.GetPrimaryBiome(biome);

            // Handle Ocean biome
            if (biome == Heightmap.Biome.Ocean)
            {
                if (!TrollTollPlugin.OceanBossRelief.Value)
                    return 0f;

                int totalBosses = GetTotalDefeatedBossCount();
                if (totalBosses <= 0)
                    return 0f;

                float oceanBase = TrollTollPlugin.BossReliefBasePercent.Value;
                float oceanAdd = TrollTollPlugin.BossReliefPerAdditionalBossPercent.Value;
                float oceanCap = TrollTollPlugin.BossReliefCapPercent.Value;
                float oceanTotal = oceanBase + ((totalBosses - 1) * oceanAdd);
                return Mathf.Clamp(oceanTotal, 0f, oceanCap) / 100f;
            }

            BossInfo boss = GetBossForBiome(biome);
            if (boss == null)
            {
                return 0f;
            }

            // The biome's boss MUST be defeated to unlock relief in this biome
            if (!IsBossDefeated(boss.GlobalKey))
            {
                return 0f;
            }

            float baseRelief = TrollTollPlugin.BossReliefBasePercent.Value;
            float perAdditional = TrollTollPlugin.BossReliefPerAdditionalBossPercent.Value;
            float cap = TrollTollPlugin.BossReliefCapPercent.Value;

            int additionalBossesCount = 0;
            string mode = TrollTollPlugin.BossReliefCalculationMode.Value;

            if (mode == "AllDefeatedBosses")
            {
                for (int i = 0; i < Bosses.Count; i++)
                {
                    if (Bosses[i] != boss && IsBossDefeated(Bosses[i].GlobalKey))
                    {
                        additionalBossesCount++;
                    }
                }
            }
            else // Default: "ProgressionTier"
            {
                // Only bosses of higher-tier biomes count as "beyond" this biome
                for (int i = 0; i < Bosses.Count; i++)
                {
                    if (Bosses[i].Tier > boss.Tier && IsBossDefeated(Bosses[i].GlobalKey))
                    {
                        additionalBossesCount++;
                    }
                }
            }

            float totalReliefPercent = baseRelief + (additionalBossesCount * perAdditional);
            float clampedPercent = Mathf.Clamp(totalReliefPercent, 0f, cap);
            return clampedPercent / 100f;
        }

        /// <summary>
        /// Generates a status summary string for debug or UI notifications.
        /// </summary>
        public static string GetStatusSummary(Heightmap.Biome biome)
        {
            biome = BiomeManager.GetPrimaryBiome(biome);
            BossInfo boss = GetBossForBiome(biome);
            float relief = GetReliefFraction(biome) * 100f;
            int totalBosses = GetTotalDefeatedBossCount();

            if (boss == null)
            {
                return $"Biome: {biome} | Relief: {relief:0}% | World Bosses Defeated: {totalBosses}";
            }

            bool defeated = IsBossDefeated(boss.GlobalKey);
            return $"Biome: {biome} | Boss: {boss.BossName} (Defeated: {defeated}) | Relief: {relief:0}% | World Bosses Defeated: {totalBosses}";
        }
    }
}

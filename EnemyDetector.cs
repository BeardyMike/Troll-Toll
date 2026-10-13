using System.Collections.Generic;
using UnityEngine;

namespace TrollToll
{
    public static class EnemyDetector
    {
        private static float _lastScanTime = -10f;
        private static bool _cachedHasThreat = false;
        private static Vector3 _lastPlayerPosition = Vector3.zero;

        /// <summary>
        /// Calculates the stamina discount fraction [0.0 to 1.0] for the local player.
        /// Returns 1.0 for 100% discount (zero stamina used), or a fraction (e.g. 0.25 to 0.75) for Boss Relief.
        /// </summary>
        public static float GetStaminaDiscount(Player player)
        {
            if (player == null || player != Player.m_localPlayer)
                return 0f;

            if (!TrollTollPlugin.ModEnabled.Value)
                return 0f;

            // Swimming restriction
            if (!TrollTollPlugin.AllowSwimmingStaminaDiscount.Value && player.IsSwimming())
                return 0f;

            // Encumbered restriction
            if (!TrollTollPlugin.AllowEncumberedStaminaDiscount.Value && player.IsEncumbered())
                return 0f;

            Heightmap.Biome currentBiome = player.GetCurrentBiome();

            // Threat check
            bool hasThreat = HasThreatNearby(player);

            if (!hasThreat)
            {
                // Out of combat: check if this biome has Troll-Toll enabled
                if (BiomeManager.IsBiomeModEnabled(currentBiome))
                {
                    return 1.0f; // 100% discount out of combat
                }

                // If biome disabled for Troll-Toll, fall back to boss relief if unlocked
                return BossReliefManager.GetReliefFraction(currentBiome);
            }

            // In combat / threat present: evaluate Boss Relief system
            return BossReliefManager.GetReliefFraction(currentBiome);
        }

        /// <summary>
        /// Backward-compatible check for full (100%) stamina discount.
        /// </summary>
        public static bool IsStaminaDiscountActive(Player player)
        {
            return GetStaminaDiscount(player) >= 1.0f;
        }

        /// <summary>
        /// Checks if any valid threat or creature is within detection radius.
        /// Uses cached results between scan intervals to maintain high framerate.
        /// </summary>
        public static bool HasThreatNearby(Player player)
        {
            if (player == null)
                return false;

            float currentTime = Time.time;
            Vector3 playerPos = player.transform.position;

            // Return cached result if within update interval and player hasn't moved significantly (> 3m)
            if (currentTime - _lastScanTime < TrollTollPlugin.UpdateInterval.Value &&
                (playerPos - _lastPlayerPosition).sqrMagnitude < 9f)
            {
                return _cachedHasThreat;
            }

            _lastScanTime = currentTime;
            _lastPlayerPosition = playerPos;
            _cachedHasThreat = ScanForThreats(player, playerPos);
            return _cachedHasThreat;
        }

        private static bool ScanForThreats(Player player, Vector3 playerPosition)
        {
            // 1. Raid / active event check
            if (TrollTollPlugin.EnforceDuringRaids.Value && RandEventSystem.HaveActiveEvent())
            {
                return true;
            }

            Heightmap.Biome currentBiome = player.GetCurrentBiome();

            // 2. Biome-specific check: if Troll-Toll is disabled for this biome, treat as threat present
            if (!BiomeManager.IsBiomeModEnabled(currentBiome))
            {
                return true;
            }

            // 3. Characters iteration
            List<Character> allCharacters = Character.GetAllCharacters();
            if (allCharacters == null)
                return false;

            float baseRadius = BiomeManager.GetBiomeDetectionRadius(currentBiome);
            float maxScanRadius = Mathf.Max(baseRadius, CreatureManager.MaxCustomRadius);
            float maxSqrRadius = maxScanRadius * maxScanRadius;

            for (int i = 0; i < allCharacters.Count; i++)
            {
                Character character = allCharacters[i];

                if (character == null || character == player || character.IsDead())
                    continue;

                float sqrDist = (character.transform.position - playerPosition).sqrMagnitude;
                if (sqrDist > maxSqrRadius)
                    continue;

                // Other players PvP check
                if (character.IsPlayer())
                {
                    if (TrollTollPlugin.EnableInPVP.Value)
                    {
                        Player otherPlayer = character as Player;
                        if (otherPlayer != null && player.IsPVPEnabled() && otherPlayer.IsPVPEnabled())
                        {
                            if (sqrDist <= baseRadius * baseRadius)
                            {
                                return true;
                            }
                        }
                    }
                    continue;
                }

                // Creature-by-creature checks:
                // 1. Ignored creatures list / toggles
                if (CreatureManager.IsCreatureIgnored(character))
                {
                    continue;
                }

                // 2. Creature-specific detection radius check
                float creatureRadius = CreatureManager.GetCustomRadius(character, baseRadius);
                if (sqrDist > creatureRadius * creatureRadius)
                {
                    continue;
                }

                // 3. Enforced threat creatures (e.g. Trolls, Abominations, Gjall, active Bosses)
                if (CreatureManager.IsCreatureEnforcedThreat(character))
                {
                    return true;
                }

                // 4. Peaceful wildlife if configured
                if (TrollTollPlugin.IncludePassiveCreatures.Value)
                {
                    return true;
                }

                // 5. Hostile relationship check
                if (BaseAI.IsEnemy(player, character))
                {
                    if (TrollTollPlugin.RequireAlertedOnly.Value)
                    {
                        BaseAI baseAI = character.GetBaseAI();
                        if (baseAI != null && (baseAI.IsAlerted() || baseAI.HaveTarget()))
                        {
                            return true;
                        }
                        continue;
                    }

                    return true;
                }

                // 6. Fallback for untamed creatures actively alerted or targeting player
                BaseAI ai = character.GetBaseAI();
                if (ai != null && (ai.IsAlerted() || ai.HaveTarget()))
                {
                    if (!character.IsTamed())
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

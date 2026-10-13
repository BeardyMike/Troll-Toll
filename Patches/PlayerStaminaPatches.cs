using HarmonyLib;

namespace TrollToll.Patches
{
    /// <summary>
    /// Intercepts stamina deductions on the player.
    /// Supports both full (100%) out-of-combat discounts and partial (25%-75%) Boss Relief discounts.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
    public static class Player_UseStamina_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Player __instance, ref float v)
        {
            // Allow stamina gains or zero-cost calls
            if (v <= 0f) return true;

            // Only apply to the local player
            if (__instance != Player.m_localPlayer) return true;

            float discount = EnemyDetector.GetStaminaDiscount(__instance);

            // 100% discount (zero stamina used)
            if (discount >= 1.0f)
            {
                return false;
            }

            // Fractional discount (e.g. Boss Relief 25% to 75%)
            if (discount > 0f)
            {
                v *= (1.0f - discount);
                return true;
            }

            // Normal stamina consumption
            return true;
        }
    }

    /// <summary>
    /// Ensures the player is not blocked from sprinting, jumping, sneaking, attacking, or building
    /// when their stamina is depleted if full discount is active or if current stamina covers the reduced cost.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.HaveStamina))]
    public static class Player_HaveStamina_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Player __instance, ref bool __result, float amount)
        {
            if (__instance != Player.m_localPlayer) return true;

            float discount = EnemyDetector.GetStaminaDiscount(__instance);

            // Full discount: always allow stamina actions
            if (discount >= 1.0f)
            {
                __result = true;
                return false;
            }

            // Fractional discount: check if current stamina meets the discounted requirement
            if (discount > 0f)
            {
                float discountedRequired = amount * (1.0f - discount);
                if (__instance.GetStamina() >= discountedRequired)
                {
                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }
}

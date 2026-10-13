using HarmonyLib;

namespace TrollToll.Patches
{
    [HarmonyPatch(typeof(Chat), "SendText")]
    public static class Chat_SendText_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Chat __instance, Talker.Type type, string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            string trimmed = text.Trim();
            if (trimmed.Equals("/trolltoll", System.StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("/tt", System.StringComparison.OrdinalIgnoreCase))
            {
                if (Player.m_localPlayer != null)
                {
                    var biome = Player.m_localPlayer.GetCurrentBiome();
                    string summary = BossReliefManager.GetStatusSummary(biome);
                    float discount = EnemyDetector.GetStaminaDiscount(Player.m_localPlayer) * 100f;
                    bool threat = EnemyDetector.HasThreatNearby(Player.m_localPlayer);

                    string response = $"<color=orange>[Troll-Toll]</color> {summary}\nThreat Nearby: {threat} | Active Stamina Discount: {discount:0}%";
                    Chat.instance.AddString(response);
                }
                else
                {
                    Chat.instance.AddString("<color=orange>[Troll-Toll]</color> Local player not active.");
                }

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Player), "UpdateBiome")]
    public static class Player_UpdateBiome_Patch
    {
        private static Heightmap.Biome _previousBiome = Heightmap.Biome.None;

        [HarmonyPostfix]
        public static void Postfix(Player __instance, float dt)
        {
            if (__instance != Player.m_localPlayer)
                return;

            if (!TrollTollPlugin.ShowBiomeEntryMessage.Value || !TrollTollPlugin.ModEnabled.Value)
                return;

            Heightmap.Biome currentBiome = __instance.GetCurrentBiome();
            if (currentBiome != _previousBiome)
            {
                _previousBiome = currentBiome;
                if (currentBiome != Heightmap.Biome.None && MessageHud.instance != null)
                {
                    float relief = BossReliefManager.GetReliefFraction(currentBiome) * 100f;
                    string msg = relief > 0f
                        ? $"{currentBiome}: {relief:0}% Boss Stamina Relief"
                        : $"{currentBiome}: 0% Boss Relief";
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, msg);
                }
            }
        }
    }
}

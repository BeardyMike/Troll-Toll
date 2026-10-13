using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace TrollToll
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class TrollTollPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.beardymike.trolltoll";
        public const string PluginName = "Troll-Toll";
        public const string PluginVersion = "0.20.4";
        public const string PluginAuthor = "BeardyMike";

        // Section 1 - General
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<float> DetectionRadius;

        // Section 2 - Detection
        public static ConfigEntry<bool> IncludePassiveCreatures;
        public static ConfigEntry<bool> RequireAlertedOnly;
        public static ConfigEntry<bool> EnforceDuringRaids;
        public static ConfigEntry<bool> EnableInPVP;

        // Section 3 - Restrictions
        public static ConfigEntry<bool> AllowSwimmingStaminaDiscount;
        public static ConfigEntry<bool> AllowEncumberedStaminaDiscount;

        // Section 4 - Performance
        public static ConfigEntry<float> UpdateInterval;

        // Section 7 - Boss Relief System
        public static ConfigEntry<bool> BossReliefEnabled;
        public static ConfigEntry<float> BossReliefBasePercent;
        public static ConfigEntry<float> BossReliefPerAdditionalBossPercent;
        public static ConfigEntry<float> BossReliefCapPercent;
        public static ConfigEntry<string> BossReliefCalculationMode;
        public static ConfigEntry<bool> OceanBossRelief;
        public static ConfigEntry<bool> ShowBiomeEntryMessage;

        private Harmony _harmony;

        private void Awake()
        {
            // 1 - General
            ModEnabled = Config.Bind("1 - General", "Enabled", true,
                "Master switch to enable or disable Troll-Toll.");

            DetectionRadius = Config.Bind("1 - General", "DetectionRadius", 30f,
                "Default distance in meters to search for nearby threats. Inside this radius, stamina costs apply normally unless reduced by Boss Relief.");

            // 2 - Detection
            IncludePassiveCreatures = Config.Bind("2 - Detection", "IncludePassiveCreatures", false,
                "If true, peaceful wildlife (like deer and birds) also triggers normal stamina usage.");

            RequireAlertedOnly = Config.Bind("2 - Detection", "RequireAlertedOnly", false,
                "If true, nearby enemies only trigger stamina consumption once they are actively alerted (spotted you) or targeting someone.");

            EnforceDuringRaids = Config.Bind("2 - Detection", "EnforceDuringRaids", true,
                "If true, stamina is always consumed normally whenever a base raid / random event is in progress.");

            EnableInPVP = Config.Bind("2 - Detection", "EnableInPVP", true,
                "If true, other players with PvP enabled in range will trigger normal stamina costs.");

            // 3 - Restrictions
            AllowSwimmingStaminaDiscount = Config.Bind("3 - Restrictions", "AllowSwimmingStaminaDiscount", false,
                "If true, swimming out of combat will consume zero stamina. Keep false for vanilla ocean balance.");

            AllowEncumberedStaminaDiscount = Config.Bind("3 - Restrictions", "AllowEncumberedStaminaDiscount", false,
                "If true, walking while encumbered out of combat will consume zero stamina.");

            // 4 - Performance
            UpdateInterval = Config.Bind("4 - Performance", "UpdateInterval", 0.2f,
                "How often (in seconds) the mod scans for nearby creatures. Caching scans prevents frame rate drops.");

            // 5 - Biome Controls
            BiomeManager.Initialize(Config);

            // 6 - Creature Controls
            CreatureManager.Initialize(Config);

            // 7 - Boss Relief System
            BossReliefEnabled = Config.Bind("7 - Boss Relief System", "BossReliefEnabled", true,
                "Defeating the boss of a biome grants permanent stamina cost relief in that biome, scaling stronger as additional bosses are defeated.");

            BossReliefBasePercent = Config.Bind("7 - Boss Relief System", "BossReliefBasePercent", 25f,
                new ConfigDescription("Base stamina relief percentage (stamina cost discount) granted in a biome immediately upon defeating its boss. (Recommended: 25% - 75%).",
                new AcceptableValueRange<float>(0f, 100f)));

            BossReliefPerAdditionalBossPercent = Config.Bind("7 - Boss Relief System", "BossReliefPerAdditionalBossPercent", 25f,
                new ConfigDescription("Additional stamina relief percentage granted in a conquered biome for each additional boss defeated beyond that biome. E.g. at 25% base + 25% per additional, 4 bosses total grants 100% relief in Meadows.",
                new AcceptableValueRange<float>(0f, 100f)));

            BossReliefCapPercent = Config.Bind("7 - Boss Relief System", "BossReliefCapPercent", 100f,
                new ConfigDescription("Maximum stamina relief percentage attainable in any biome (100% = zero stamina consumed even during combat).",
                new AcceptableValueRange<float>(0f, 100f)));

            BossReliefCalculationMode = Config.Bind("7 - Boss Relief System", "BossReliefCalculationMode", "ProgressionTier",
                "ProgressionTier: Only bosses defeated in higher-tier biomes grant additional relief (e.g. Elder/Bonemass/Moder increase Meadows relief). AllDefeatedBosses: Any other defeated boss in the world grants additional relief.");

            OceanBossRelief = Config.Bind("7 - Boss Relief System", "OceanBossRelief", true,
                "If true, Ocean biome receives stamina relief scaling with total world bosses defeated.");

            ShowBiomeEntryMessage = Config.Bind("7 - Boss Relief System", "ShowBiomeEntryMessage", false,
                "If true, displays a brief top-left notification showing your current biome's Boss Relief stamina discount upon entering a new biome.");

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            Logger.LogInfo($"{PluginName} v{PluginVersion} by {PluginAuthor} loaded successfully.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}

using BepInEx.Configuration;

namespace BreakContact.Configs
{
	internal class BreakContactConfig
	{
		public static ConfigEntry<bool> EnableMod { get; private set; }
		public static ConfigEntry<bool> EnableQuestPresenceDetector { get; private set; }
		public static ConfigEntry<int> QuestPresenceIndicatorMaxDistance { get; private set; }
		public static ConfigEntry<bool> EnableFriendlyNpcIndicator { get; private set; }
		public static ConfigEntry<int> FriendlyNpcIndicatorMaxDistance { get; private set; }
		public static ConfigEntry<int> WorldMarkerDotSize { get; private set; }
		public static ConfigEntry<bool> FriendlyNpcIndicatorDebugLogging { get; private set; }
		public static ConfigEntry<bool> EnableWhiteFlareTrainSummon { get; private set; }
		public static ConfigEntry<float> WhiteFlareTrainResponseMaxMinutes { get; private set; }
		public static ConfigEntry<bool> EnableSmokeOccluders { get; private set; }
		public static ConfigEntry<int> SmokeOccluderDensity { get; private set; }
		public static ConfigEntry<float> SmokeOccluderRadiusMultiplier { get; private set; }
		public static ConfigEntry<bool> SmokeOccluderDebugVisuals { get; private set; }
		public static ConfigEntry<bool> ReplaceM18SmokeVisual { get; private set; }
		public static ConfigEntry<int> M18RussianPlumeCount { get; private set; }
		public static ConfigEntry<float> M18RussianPlumeScale { get; private set; }
		public static ConfigEntry<float> M18RussianSmokeAmount { get; private set; }
		public static ConfigEntry<float> M18RussianSmokeBuildSpeed { get; private set; }
		public static ConfigEntry<string> M18RussianSmokeColorHex { get; private set; }
		public static ConfigEntry<float> M18SmokeOccluderRadiusMultiplier { get; private set; }
		public static ConfigEntry<bool> EnableBreakContactSmoke { get; private set; }
		public static ConfigEntry<float> BreakContactSmokeChance { get; private set; }
		public static ConfigEntry<float> BreakContactSmokeCooldown { get; private set; }
		public static ConfigEntry<float> BreakContactSmokeThrowDistance { get; private set; }
		public static ConfigEntry<bool> BreakContactSmokeAllowEmergencyToss { get; private set; }
		public static ConfigEntry<bool> BreakContactSmokeDebugLogging { get; private set; }
		private const string GeneralSettings = "General Settings";

		public static void Init(ConfigFile config)
		{
			EnableMod = config.Bind(GeneralSettings, "Enable Mod", true,
				new ConfigDescription("Enable or disable Break Contact. Requires the game to be restarted.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 100 }));

			EnableQuestPresenceDetector = config.Bind(GeneralSettings, "Enable Quest Presence Detector", true,
				new ConfigDescription("Show a world-space marker above nearby loose quest items. Requires restart.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 95 }));

			EnableFriendlyNpcIndicator = config.Bind(GeneralSettings, "Enable Friendly NPC Indicator", true,
				new ConfigDescription("Show a light blue marker above friendly NPCs.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 94 }));

			QuestPresenceIndicatorMaxDistance = config.Bind(GeneralSettings, "Quest Presence Indicator Max Distance", 25,
				new ConfigDescription("Distance in meters where quest objective markers fade to fully transparent.",
				new AcceptableValueRange<int>(1, 100),
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 93 }));

			FriendlyNpcIndicatorMaxDistance = config.Bind(GeneralSettings, "Friendly NPC Indicator Max Distance", 100,
				new ConfigDescription("Distance in meters where friendly NPC markers fade to fully transparent.",
				new AcceptableValueRange<int>(1, 100),
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 92 }));

			WorldMarkerDotSize = config.Bind(GeneralSettings, "World Marker Dot Size", 10,
				new ConfigDescription("Diameter in screen pixels for quest objective and friendly NPC markers.",
				new AcceptableValueRange<int>(4, 64),
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 91 }));

			FriendlyNpcIndicatorDebugLogging = config.Bind(GeneralSettings, "Friendly NPC Indicator Debug Logging", false,
				new ConfigDescription("Log friendly NPC marker candidate counts and relation decisions.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 90 }));

			EnableWhiteFlareTrainSummon = config.Bind(GeneralSettings, "White Flare Train Summon", true,
				new ConfigDescription("Summon map trains when the local player fires a successful white flare.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 91 }));

			WhiteFlareTrainResponseMaxMinutes = config.Bind(GeneralSettings, "White Flare Train Response Max Minutes", 3f,
				new ConfigDescription("Maximum randomized train response delay in minutes after a successful white flare. The actual delay is random from 0 to this value.",
				new AcceptableValueRange<float>(0f, 10f),
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 90 }));

			EnableSmokeOccluders = config.Bind(GeneralSettings, "Enable Smoke Occluders", true,
				new ConfigDescription("Spawn invisible foliage occluders inside active smoke grenades so bots lose sight through smoke.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 90 }));

			SmokeOccluderDensity = config.Bind(GeneralSettings, "Smoke Occluder Density", 16,
				new ConfigDescription("How many invisible AI sight occluders to spawn per active smoke grenade.",
				new AcceptableValueRange<int>(4, 48),
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 89 }));

			SmokeOccluderRadiusMultiplier = config.Bind(GeneralSettings, "Smoke Occluder Radius Multiplier", 1f,
				new ConfigDescription("Scales the invisible AI sight occluder field around active smoke grenades.",
				new AcceptableValueRange<float>(0.25f, 2.5f),
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 88 }));

			SmokeOccluderDebugVisuals = config.Bind(GeneralSettings, "Smoke Occluder Debug Visuals", false,
				new ConfigDescription("Show translucent green cylinders for active smoke AI occluders.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 87 }));

			ReplaceM18SmokeVisual = config.Bind(GeneralSettings, "Replace M18 Smoke Visual", true,
				new ConfigDescription("Replace the M18 grenade's vanilla smoke visual with larger Russian-style smoke plumes.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 80 }));

			M18RussianPlumeCount = config.Bind(GeneralSettings, "M18 Russian Smoke Plume Count", 1,
				new ConfigDescription("How many Russian-style smoke plume instances to spawn for M18 smoke grenades.",
				new AcceptableValueRange<int>(1, 8),
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 79 }));

			M18RussianPlumeScale = config.Bind(GeneralSettings, "M18 Russian Smoke Plume Scale", 2.5f,
				new ConfigDescription("Scales the Russian-style smoke plumes used for M18 smoke grenades.",
				new AcceptableValueRange<float>(0.5f, 4f),
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 78 }));

			M18RussianSmokeAmount = config.Bind(GeneralSettings, "M18 Russian Smoke Amount", 1f,
				new ConfigDescription("Multiplies particle count, emission rate, and particle size in the M18 Russian-style replacement.",
				new AcceptableValueRange<float>(0.5f, 8f),
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 77 }));

			M18RussianSmokeBuildSpeed = config.Bind(GeneralSettings, "M18 Russian Smoke Build Speed", 3f,
				new ConfigDescription("How aggressively the M18 Russian-style replacement prewarms and fills in after ignition.",
				new AcceptableValueRange<float>(1f, 6f),
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 76 }));

			M18RussianSmokeColorHex = config.Bind(GeneralSettings, "M18 Russian Smoke Color Hex", "#FFFFFF",
				new ConfigDescription("Hex color tint for the M18 Russian-style smoke replacement. Use #RRGGBB or #RRGGBBAA; #FFFFFF preserves the original Russian smoke color.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 75 }));

			M18SmokeOccluderRadiusMultiplier = config.Bind(GeneralSettings, "M18 Smoke Occluder Radius Multiplier", 1.6f,
				new ConfigDescription("Extra radius multiplier for invisible AI sight occluders on M18 smoke grenades only.",
				new AcceptableValueRange<float>(0.5f, 3.5f),
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 74 }));

			EnableBreakContactSmoke = config.Bind(GeneralSettings, "Enable Break Contact Smoke", true,
				new ConfigDescription("When SAIN is installed, bots that enter SAIN's Retreat decision will try to throw a smoke grenade to break contact.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 70 }));

			BreakContactSmokeChance = config.Bind(GeneralSettings, "Break Contact Smoke Chance", 1f,
				new ConfigDescription("Chance that a bot with smoke will throw one when entering SAIN's Retreat decision.",
				new AcceptableValueRange<float>(0f, 1f),
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 69 }));

			BreakContactSmokeCooldown = config.Bind(GeneralSettings, "Break Contact Smoke Cooldown", 45f,
				new ConfigDescription("Per-bot cooldown in seconds between break contact smoke attempts.",
				new AcceptableValueRange<float>(5f, 180f),
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 68 }));

			BreakContactSmokeThrowDistance = config.Bind(GeneralSettings, "Break Contact Smoke Throw Distance", 7f,
				new ConfigDescription("Maximum distance in meters for the break contact smoke target, placed between the bot and its enemy.",
				new AcceptableValueRange<float>(2f, 20f),
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 67 }));

			BreakContactSmokeAllowEmergencyToss = config.Bind(GeneralSettings, "Break Contact Smoke Allow Emergency Toss", true,
				new ConfigDescription("If no clean smoke trajectory is found, allow a short low-power toss so the break contact smoke still happens.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 66 }));

			BreakContactSmokeDebugLogging = config.Bind(GeneralSettings, "Break Contact Smoke Debug Logging", false,
				new ConfigDescription("Log SAIN break contact smoke attempts and rejection reasons.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = true, Order = 65 }));

		}
	}
}

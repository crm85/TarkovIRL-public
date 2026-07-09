using System;
using BepInEx;
using Comfort.Common;
using EFT;
using BreakContact.Configs;

namespace BreakContact
{
	[BepInPlugin("com.breakcontact.mod", "Break Contact", "0.1")]
	[BepInDependency("RealismMod", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("me.sol.sain", BepInDependency.DependencyFlags.SoftDependency)]
	public class BreakContactPlugin : BaseUnityPlugin
	{
		public void Awake()
		{
			BreakContactConfig.Init(Config);
			if (!BreakContactConfig.EnableMod.Value) return;

			if (BreakContactConfig.EnableQuestPresenceDetector.Value || BreakContactConfig.EnableFriendlyNpcIndicator.Value)
			{
				QuestPresenceDetector.SetLogger(Logger);
				FriendlyNpcIndicator.SetLogger(Logger);
				EnablePatchSafe("QuestPresencePlayerPatch", () => new QuestPresencePlayerPatch().Enable());
			}
			if (BreakContactConfig.EnableFriendlyNpcIndicator.Value)
			{
				FriendlyNpcIndicator.Ensure();
			}
			if (BreakContactConfig.EnableQuestPresenceDetector.Value)
			{
				EnablePatchSafe("QuestPresenceLootItemPatch", () => new QuestPresenceLootItemPatch().Enable());
			}
			if (RealismAquapepsPurification.HasRealismFoodPoisoningSupport())
			{
				EnablePatchSafe("RealismAquapepsDrinkCombinePatch", () => new RealismAquapepsDrinkCombinePatch().Enable());
				EnablePatchSafe("RealismAquapepsContextCombinePatch", () => new RealismAquapepsContextCombinePatch().Enable());
				EnablePatchSafe("RealismAquapepsPoisoningPatch", () => new RealismAquapepsPoisoningPatch().Enable());
			}
			EnablePatchSafe("TrainSummonPatch", () => new TrainSummonPatch().Enable());
			EnablePatchSafe("BaseGrenadeHandsControllerCreateGrenadePatch", () => new BaseGrenadeHandsControllerCreateGrenadePatch().Enable());
			EnablePatchSafe("GrenadeInitPatch", () => new GrenadeInitPatch().Enable());
			EnablePatchSafe("SmokeGrenadeInitPatch", () => new SmokeGrenadeInitPatch().Enable());
			EnablePatchSafe("SmokeGrenadeSetEmissionPatch", () => new SmokeGrenadeSetEmissionPatch().Enable());
			EnablePatchSafe("SmokeGrenadeExplosionPatch", () => new SmokeGrenadeExplosionPatch().Enable());
			EnablePatchSafe("M18SmokeEffectSuppressionPatch", () => new M18SmokeEffectSuppressionPatch().Enable());
			EnablePatchSafe("M18SmokeEmissionSuppressionPatch", () => new M18SmokeEmissionSuppressionPatch().Enable());
			EnablePatchSafe("M18SmokeEmissionStartSuppressionPatch", () => new M18SmokeEmissionStartSuppressionPatch().Enable());
			EnablePatchSafe("M18SmokeEmissionRendererSuppressionPatch", () => new M18SmokeEmissionRendererSuppressionPatch().Enable());
			EnablePatchSafe("SainBreakContactSmokePatch", SainBreakContactSmokePatch.TryEnable);
		}

		private void EnablePatchSafe(string patchName, Action enable)
		{
			try
			{
				enable();
			}
			catch (Exception ex)
			{
				Logger.LogWarning($"BreakContact patch {patchName} failed to enable; continuing without it. {ex}");
			}
		}

		public static bool InGame()
		{
			return Singleton<GameWorld>.Instance?.MainPlayer?.HealthController.IsAlive == true
				&& !(Singleton<GameWorld>.Instance.MainPlayer is HideoutPlayer);
		}
	}
}

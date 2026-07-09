using BepInEx;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.HealthSystem;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BreakContact
{
	public static class RealismAquapepsPurification
	{
		private const string AquapepsTemplateId = "6389c6c7dbfd5e4b95197e68";
		private const string DrinkParentId = "5448e8d64bdc2dce718b4568";
		private const float PurifiedToxinChanceMultiplier = 0.2f;
		private const string StateFileName = "BreakContact-AquapepsPurifiedDrinks.txt";

		private static readonly HashSet<string> PurifiedDrinkIds = new HashSet<string>();
		private static bool _loadedState;
		private static Type _realismApplyItemStashPatchType;

		public static Type GetRealismApplyItemStashPatchType()
		{
			if (_realismApplyItemStashPatchType != null) return _realismApplyItemStashPatchType;

			_realismApplyItemStashPatchType = Type.GetType("RealismMod.ApplyItemStashPatch, RealismMod", false);
			return _realismApplyItemStashPatchType;
		}

		public static bool HasRealismFoodPoisoningSupport()
		{
			Type patchType = GetRealismApplyItemStashPatchType();
			if (patchType == null) return false;

			return AccessTools.Method(patchType, "TryDoPoisoning", new[] { typeof(HealthControllerClass), typeof(FoodDrinkItemClass) }) != null;
		}

		public static bool TryGetPurificationPair(Item source, Item target, out Item aquapeps, out FoodDrinkItemClass drink)
		{
			aquapeps = null;
			drink = null;

			if (IsAquapeps(source) && IsDrink(target))
			{
				aquapeps = source;
				drink = (FoodDrinkItemClass)target;
			}
			else if (IsAquapeps(target) && IsDrink(source))
			{
				aquapeps = target;
				drink = (FoodDrinkItemClass)source;
			}

			return aquapeps != null && drink != null && !IsPurified(drink) && HasToxinChance(drink);
		}

		public static bool IsPurified(FoodDrinkItemClass drink)
		{
			EnsureStateLoaded();
			return drink != null && !string.IsNullOrEmpty(drink.Id) && PurifiedDrinkIds.Contains(drink.Id);
		}

		public static void MarkPurified(FoodDrinkItemClass drink)
		{
			if (drink == null || string.IsNullOrEmpty(drink.Id)) return;

			EnsureStateLoaded();
			if (!PurifiedDrinkIds.Add(drink.Id)) return;

			SaveState();
			drink.UpdateAttributes();
		}

		public static bool TryDoReducedPoisoning(HealthControllerClass healthController, FoodDrinkItemClass drink, out bool poisoned)
		{
			poisoned = false;
			if (!IsPurified(drink)) return false;

			GClass2823.GClass2848.GClass2849 toxinBuff = GetToxinBuff(drink);
			if (toxinBuff == null) return false;

			float reducedChance = toxinBuff.Chance * PurifiedToxinChanceMultiplier;
			if (reducedChance > 0f && UnityEngine.Random.Range(0, 100) < reducedChance * 100f)
			{
				float energyLoss = UnityEngine.Random.Range(toxinBuff.Chance * 250f, toxinBuff.Chance * 500f);
				energyLoss = Mathf.Clamp(energyLoss, 2.5f, 90f);
				float hydrationLoss = UnityEngine.Random.Range(toxinBuff.Chance * 250f, toxinBuff.Chance * 500f);
				hydrationLoss = Mathf.Clamp(hydrationLoss, 2.5f, 90f);
				healthController.ChangeEnergy(0f - energyLoss);
				healthController.ChangeHydration(0f - hydrationLoss);
				PlayFoodPoisoningSound();
				poisoned = true;
			}

			return true;
		}

		private static bool IsAquapeps(Item item)
		{
			return item?.StringTemplateId == AquapepsTemplateId;
		}

		private static bool IsDrink(Item item)
		{
			return item is FoodDrinkItemClass && item.Template?.Parent?._id == DrinkParentId;
		}

		private static bool HasToxinChance(FoodDrinkItemClass drink)
		{
			return GetToxinBuff(drink) != null;
		}

		private static GClass2823.GClass2848.GClass2849 GetToxinBuff(FoodDrinkItemClass drink)
		{
			return drink?.HealthEffectsComponent?.BuffSettings?
				.FirstOrDefault(buff => buff.BuffType == EStimulatorBuffType.UnknownToxin && buff.Chance > 0f);
		}

		private static void PlayFoodPoisoningSound()
		{
			try
			{
				Type pluginType = Type.GetType("RealismMod.Plugin, RealismMod", false);
				object audioController = AccessTools.Field(pluginType, "RealismAudioController")?.GetValue(null);
				object clipsObject = AccessTools.Field(audioController?.GetType(), "FoodPoisoningSfx")?.GetValue(audioController);
				if (!(clipsObject is IDictionary clips) || clips.Count == 0) return;

				AudioClip[] audioClips = clips.Values.OfType<AudioClip>().ToArray();
				if (audioClips.Length == 0) return;

				Singleton<GUISounds>.Instance.PlaySound(audioClips[UnityEngine.Random.Range(0, audioClips.Length)], false, false, 0.5f);
			}
			catch
			{
				// Missing audio should not block purification or consumption.
			}
		}

		private static void EnsureStateLoaded()
		{
			if (_loadedState) return;

			_loadedState = true;
			try
			{
				string path = GetStatePath();
				if (!File.Exists(path)) return;

				foreach (string line in File.ReadAllLines(path))
				{
					string itemId = line.Trim();
					if (!string.IsNullOrEmpty(itemId))
						PurifiedDrinkIds.Add(itemId);
				}
			}
			catch
			{
				PurifiedDrinkIds.Clear();
			}
		}

		private static void SaveState()
		{
			try
			{
				string path = GetStatePath();
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				File.WriteAllLines(path, PurifiedDrinkIds.OrderBy(id => id).ToArray());
			}
			catch
			{
				// Purification still works for this session even if local persistence fails.
			}
		}

		private static string GetStatePath()
		{
			return Path.Combine(BepInEx.Paths.ConfigPath, StateFileName);
		}
	}

	public class RealismAquapepsDrinkCombinePatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(TraderControllerClass), "ExecutePossibleAction", new[]
			{
				typeof(ItemContextAbstractClass),
				typeof(Item),
				typeof(bool),
				typeof(bool)
			});
		}

		[PatchPrefix]
		[HarmonyPriority(Priority.First)]
		private static bool Prefix(ItemContextAbstractClass itemContext, Item targetItem, bool simulate, ref GStruct454 __result)
		{
			return TryPurify(itemContext?.Item, targetItem, simulate, ref __result);
		}

		public static bool TryPurify(Item source, Item target, bool simulate, ref GStruct454 result)
		{
			if (!RealismAquapepsPurification.HasRealismFoodPoisoningSupport()) return true;
			if (!RealismAquapepsPurification.TryGetPurificationPair(source, target, out Item aquapeps, out FoodDrinkItemClass drink)) return true;
			if (aquapeps.CurrentAddress == null) return true;

			GStruct455<GClass3205> removeResult = aquapeps.CurrentAddress.Remove(aquapeps, simulate);
			if (removeResult.Failed)
			{
				result = removeResult;
				return false;
			}

			if (!simulate)
				RealismAquapepsPurification.MarkPurified(drink);

			result = new GStruct454(removeResult.Value);
			return false;
		}
	}

	public class RealismAquapepsContextCombinePatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(TraderControllerClass), "ExecutePossibleAction", new[]
			{
				typeof(ItemContextAbstractClass),
				typeof(ItemContextAbstractClass),
				typeof(ItemAddress),
				typeof(bool),
				typeof(bool)
			});
		}

		[PatchPrefix]
		[HarmonyPriority(Priority.First)]
		private static bool Prefix(ItemContextAbstractClass itemContext, ItemContextAbstractClass targetItemContext, bool simulate, ref GStruct454 __result)
		{
			return RealismAquapepsDrinkCombinePatch.TryPurify(itemContext?.Item, targetItemContext?.Item, simulate, ref __result);
		}
	}

	public class RealismAquapepsPoisoningPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(RealismAquapepsPurification.GetRealismApplyItemStashPatchType(), "TryDoPoisoning", new[]
			{
				typeof(HealthControllerClass),
				typeof(FoodDrinkItemClass)
			});
		}

		[PatchPrefix]
		[HarmonyPriority(Priority.First)]
		private static bool Prefix(HealthControllerClass hc, FoodDrinkItemClass foodClass, ref bool __result)
		{
			if (!RealismAquapepsPurification.TryDoReducedPoisoning(hc, foodClass, out bool poisoned)) return true;

			__result = poisoned;
			return false;
		}
	}
}

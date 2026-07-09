using System;
using System.Linq;
using System.Reflection;
using BreakContact.Configs;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.Hideout;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BreakContact
{
	internal sealed class InstantCraftingPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(ProduceView), "method_3");
		}

		[PatchPrefix]
		[HarmonyPriority(Priority.First)]
		private static bool Prefix(ProduceView __instance)
		{
			if (!BreakContactConfig.EnableInstantCrafting.Value)
				return true;

			try
			{
				return InstantCrafting.HandleStartCraft(__instance);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Break Contact instant crafting fuel check failed; allowing vanilla start. " + ex);
				return true;
			}
		}
	}

	internal sealed class InstantCraftingRecipeViewPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(ProduceView), "method_2");
		}

		[PatchPostfix]
		private static void Postfix(ProduceView __instance)
		{
			if (!BreakContactConfig.EnableInstantCrafting.Value)
				return;

			InstantCrafting.UpdateFuelCostDisplay(__instance);
		}
	}

	internal sealed class InstantCraftingStartButtonPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(ProduceView), "method_4");
		}

		[PatchPostfix]
		private static void Postfix(ProduceView __instance)
		{
			if (!BreakContactConfig.EnableInstantCrafting.Value)
				return;

			InstantCrafting.UpdateStartButton(__instance);
		}
	}

	internal static class InstantCrafting
	{
		private const float FuelEpsilon = 0.0001f;
		private static readonly FieldInfo StartButtonField = AccessTools.Field(typeof(ProduceView), "_startButton");
		private static readonly FieldInfo ExpectedTimeField = AccessTools.Field(typeof(ProduceView), "_expectedTime");

		public static bool HandleStartCraft(ProduceView view)
		{
			GClass2202 scheme = view?.Scheme;
			GClass2193 producer = view?.Producer;
			if (!IsInstantCraftingView(view, out scheme, out producer))
				return true;

			if (!view.Boolean_0 || !producer.CanStartByProductionCount())
				return true;

			if (!HasEnoughFuelOrServerCanValidate(scheme))
				return false;

			_ = Singleton<HideoutClass>.Instance.StartSingleProduction(scheme, delegate
			{
				view.OnStartProducing?.Invoke(scheme._id);
			});
			return false;
		}

		public static void UpdateFuelCostDisplay(ProduceView view)
		{
			if (!IsInstantCraftingView(view, out GClass2202 scheme, out _))
				return;

			object expectedTime = ExpectedTimeField?.GetValue(view);
			if (expectedTime == null)
				return;

			SetText(expectedTime, "Fuel\n" + FormatFuel(GetFuelRequirement(scheme.productionTime)));
		}

		public static void UpdateStartButton(ProduceView view)
		{
			if (!IsInstantCraftingView(view, out GClass2202 scheme, out GClass2193 producer))
				return;

			DefaultUIButton startButton = StartButtonField?.GetValue(view) as DefaultUIButton;
			if (startButton == null)
				return;

			bool hasCompletedItems = producer.CompleteItemsStorage.GetItemsCount(scheme._id) > 0;
			bool isProducing = producer.ProducingItems.ContainsKey(scheme._id);
			bool canStartInstantCraft = view.Boolean_0
				&& producer.CanStartByProductionCount()
				&& !hasCompletedItems
				&& !isProducing;

			if (!canStartInstantCraft)
				return;

			startButton.SetDisabledTooltip(string.Empty);
			startButton.Interactable = true;
			((Component)startButton).gameObject.SetActive(true);
		}

		private static bool IsInstantCraftingView(ProduceView view, out GClass2202 scheme, out GClass2193 producer)
		{
			scheme = view?.Scheme;
			producer = view?.Producer;

			return scheme != null
				&& producer != null
				&& !scheme.continuous;
		}

		private static void SetText(object textComponent, string text)
		{
			textComponent.GetType()
				.GetProperty("text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				?.SetValue(textComponent, text, null);
		}

		private static bool HasEnoughFuelOrServerCanValidate(GClass2202 scheme)
		{
			float requiredFuel = GetFuelRequirement(scheme.productionTime);
			if (requiredFuel <= FuelEpsilon)
				return true;

			if (!TryGetAvailableGeneratorFuel(out float availableFuel))
			{
				Debug.LogWarning("Break Contact instant crafting fuel check could not read generator fuel; allowing server-side validation.");
				return true;
			}

			if (availableFuel + FuelEpsilon >= requiredFuel)
			{
				return true;
			}

			NotificationManagerClass.DisplayWarningNotification(
				"Not enough generator fuel for instant craft (" +
				FormatFuel(availableFuel) + "/" + FormatFuel(requiredFuel) + ").");
			return false;
		}

		private static float GetFuelRequirement(float productionTimeSeconds)
		{
			float fuelPerCraftMinute = Mathf.Max(0f, BreakContactConfig.InstantCraftingFuelPerCraftMinute.Value);
			float minimumFuel = Mathf.Max(0f, BreakContactConfig.InstantCraftingMinimumFuel.Value);
			float baseRequirement = Mathf.Max(0f, productionTimeSeconds) / 60f * fuelPerCraftMinute;
			return Mathf.Max(minimumFuel, baseRequirement);
		}

		private static bool TryGetAvailableGeneratorFuel(out float availableFuel)
		{
			availableFuel = 0f;
			bool known = false;

			if (TryGetGeneratorProducerFuel(out float producerFuel))
			{
				availableFuel = Mathf.Max(availableFuel, producerFuel);
				known = true;
			}

			if (TryGetGeneratorProfileFuel(out float profileFuel))
			{
				availableFuel = Mathf.Max(availableFuel, profileFuel);
				known = true;
			}

			return known;
		}

		private static bool TryGetGeneratorProducerFuel(out float availableFuel)
		{
			availableFuel = 0f;

			GClass2196 generatorProducer = GetGeneratorProducer();
			BarterItemItemClass[] fuelItems = generatorProducer?.ResourceConsumer?.UsingItems;
			if (fuelItems == null)
				return false;

			availableFuel = fuelItems.Sum(GetFuelValue);
			return true;
		}

		private static bool TryGetGeneratorProfileFuel(out float availableFuel)
		{
			availableFuel = 0f;

			GClass1972 generatorArea = GetGeneratorAreaInfo();
			if (generatorArea?.Slots == null)
				return false;

			bool inspectedSlots = false;
			foreach (GClass2188 slot in generatorArea.Slots)
			{
				GClass1319[] flatItems = slot?.Items;
				if (flatItems == null || flatItems.Length == 0)
				{
					inspectedSlots = true;
					continue;
				}

				inspectedSlots = true;
				try
				{
					Item slotItem = Singleton<ItemFactoryClass>.Instance.FlatItemsToTree(flatItems).Items.FirstOrDefault().Value;
					availableFuel += GetFuelValue(slotItem);
				}
				catch (Exception ex)
				{
					Debug.LogWarning("Break Contact instant crafting failed to read generator slot fuel. " + ex);
				}
			}

			return inspectedSlots;
		}

		private static GClass1972 GetGeneratorAreaInfo()
		{
			HideoutClass hideout = Singleton<HideoutClass>.Instance;
			GClass1972 areaInfo = hideout?.iSession?.Profile?.Hideout?.Areas?.FirstOrDefault(area => area?.AreaType == EAreaType.Generator);
			if (areaInfo != null)
				return areaInfo;

			areaInfo = hideout?.GClass1972_0?.FirstOrDefault(area => area?.AreaType == EAreaType.Generator);
			return areaInfo ?? hideout?.gclass1972_0;
		}

		private static float GetFuelValue(Item item)
		{
			BarterItemItemClass barterItem = item as BarterItemItemClass;
			ResourceComponent resourceComponent = barterItem?.ResourceHolderComponent ?? item?.GetItemComponent<ResourceComponent>();
			return Mathf.Max(0f, resourceComponent?.Value ?? 0f);
		}

		private static GClass2196 GetGeneratorProducer()
		{
			HideoutClass hideout = Singleton<HideoutClass>.Instance;
			if (hideout?.ProductionController == null)
				return null;

			AreaData generatorArea = null;
			if (hideout.dictionary_0 != null)
				hideout.dictionary_0.TryGetValue(EAreaType.Generator, out generatorArea);

			generatorArea = generatorArea ?? hideout.AreaDatas?.FirstOrDefault(areaData => areaData?.Template?.Type == EAreaType.Generator);
			return generatorArea == null ? null : hideout.ProductionController.GetProducer(generatorArea) as GClass2196;
		}

		private static string FormatFuel(float value)
		{
			return value.ToString("0.#");
		}
	}
}

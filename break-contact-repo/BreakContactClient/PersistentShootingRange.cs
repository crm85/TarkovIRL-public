using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BreakContact.Configs;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.InputSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.Networking;

namespace BreakContact
{
	internal sealed class PersistentShootingRangeEnterPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(HideoutPlayer), nameof(HideoutPlayer.UpdateHideoutPlayerInventory));
		}

		[PatchPrefix]
		private static bool Prefix(HideoutPlayer __instance)
		{
			if (!ShootingRangePersistence.CanUsePersistentRange)
				return true;

			if (__instance == null || __instance.IsUpdateHideoutPlayerInventoryInProgress)
				return true;

			ShootingRangePersistence.BeginEnter(__instance);
			return true;
		}

		[PatchPostfix]
		private static void Postfix(HideoutPlayer __instance, ref Task __result)
		{
			if (!ShootingRangePersistence.UsesPersistentRange(__instance) || __result == null)
				return;

			__result = ShootingRangePersistence.TrackVanillaEnterAsync(__instance, __result);
		}
	}

	internal sealed class PersistentShootingRangeExitPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(HideoutPlayer), nameof(HideoutPlayer.ReleaseShootingRangeInventory));
		}

		[PatchPrefix]
		private static bool Prefix(HideoutPlayer __instance, ref Task __result)
		{
			if (!ShootingRangePersistence.UsesPersistentRange(__instance))
				return true;

			__result = ShootingRangePersistence.ExitAsync(__instance);
			return false;
		}
	}

	internal sealed class PersistentShootingRangeInventoryInputPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(HideoutPlayerOwner), nameof(HideoutPlayerOwner.TranslateInventoryScreenInput));
		}

		[PatchPrefix]
		private static bool Prefix(HideoutPlayerOwner __instance, ECommand command, ref bool __result)
		{
			if (!BreakContactConfig.EnablePersistentShootingRange.Value || !BreakContactConfig.ShootingRangeAllowInventoryInRange.Value)
				return true;

			if (__instance == null || !__instance.InShootingRange || !GClass2168.IsCommand(command, ECommand.ToggleInventory))
				return true;

			HideoutPlayer player = __instance.HideoutPlayer;
			if (player?.OriginalInventory == null)
				return true;

			__instance.ShowInventoryScreen(
				delegate { player.SetInventoryOpened(opened: false); },
				player.HealthController,
				player.OriginalInventory,
				player.AbstractQuestControllerClass,
				player.AbstractAchievementControllerClass,
				player.GClass3695_0,
				player.OriginalInventory.Inventory.Stash,
				EInventoryTab.Unchanged);
			player.SetInventoryOpened(opened: true);
			__result = true;
			return false;
		}
	}

	internal static class ShootingRangePersistence
	{
		private const string PersistRoute = "/breakcontact/shootingrange/persist";
		private const int MaxWaitFrames = 600;

		private static readonly FieldInfo UsesShootingRangeInventoryField = AccessTools.Field(typeof(HideoutPlayer), "bool_1");
		private static readonly HashSet<int> PersistentRangePlayers = new HashSet<int>();
		private static readonly Dictionary<int, RangeSession> RangeSessions = new Dictionary<int, RangeSession>();

		public static bool CanUsePersistentRange
		{
			get
			{
				return BreakContactConfig.EnablePersistentShootingRange.Value && UsesShootingRangeInventoryField != null;
			}
		}

		public static bool UsesPersistentRange(HideoutPlayer player)
		{
			return player != null && PersistentRangePlayers.Contains(player.GetInstanceID());
		}

		public static void BeginEnter(HideoutPlayer player)
		{
			if (player == null)
				return;

			SetPersistentRange(player, active: true);
			RangeSessions.Remove(player.GetInstanceID());
		}

		public static async Task TrackVanillaEnterAsync(HideoutPlayer player, Task vanillaEnterTask)
		{
			try
			{
				await vanillaEnterTask;

				if (UsesPersistentRange(player))
				{
					TrackRangeSession(player);
				}
			}
			catch (Exception ex)
			{
				SetPersistentRange(player, active: false);
				RangeSessions.Remove(player.GetInstanceID());
				Debug.LogWarning("Break Contact persistent shooting range tracking failed; falling back to vanilla cleanup. " + ex);
				throw;
			}
		}

		public static async Task ExitAsync(HideoutPlayer player)
		{
			if (player == null)
				return;

			while (player.IsUpdateHideoutPlayerInventoryInProgress)
			{
				await Task.Yield();
			}

			player.IsUpdateHideoutPlayerInventoryInProgress = true;

			try
			{
				(player.HandsController as IFirearmHandsController)?.SetTriggerPressed(pressed: false);
				player.FastForwardCurrentOperations();

				if (!(player.HandsController is EFT.Player.EmptyHandsController))
				{
					if (player.ProceduralWeaponAnimation.IsGrenadeLauncher && player.HandsController is EFT.Player.FirearmController firearmController)
					{
						firearmController.ToggleLauncher();
						await Task.Yield();
						player.FastForwardCurrentOperations();
						await Task.Yield();
					}

					bool handsAreEmpty = false;
					await player.method_163();
					player.SetEmptyHands(delegate { handsAreEmpty = true; });

					if (player.IsInPatrol)
					{
						player.FastForwardCurrentOperations();
					}
					else
					{
						int waitFrames = 0;
						while (!handsAreEmpty && waitFrames++ < MaxWaitFrames)
						{
							if (player.PointOfView != EPointOfView.FirstPerson)
							{
								player.FastForwardCurrentOperations();
							}

							await Task.Yield();
						}
					}
				}

				RangePersistenceRequest snapshot = BuildPersistenceRequest(player);
				bool persisted = await TryPersistRangeStateAsync(player, snapshot);
				HashSet<string> keepLootIds = persisted ? new HashSet<string>() : snapshot.keepLootIds;

				if (!persisted && snapshot.returnedRootIds.Length > 0)
				{
					NotificationManagerClass.DisplayWarningNotification(
						$"Could not return {snapshot.returnedRootIds.Length} shooting range item(s). Pick them up before leaving the hideout.");
				}

				ClearShootingRangeEquipment(player.ShootingRangeInventory);
				CleanupWorldLoot(player, keepLootIds);
				player.MovementContext.RestorePreviousPatrol();
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Break Contact persistent shooting range exit failed. " + ex);
			}
			finally
			{
				player.IsUpdateHideoutPlayerInventoryInProgress = false;
				SetShootingRangeModeFlag(player, value: false);
				SetPersistentRange(player, active: false);
				RangeSessions.Remove(player.GetInstanceID());
			}
		}

		private static void TrackRangeSession(HideoutPlayer player)
		{
			InventoryEquipment rangeEquipment = player?.ShootingRangeInventory?.Inventory?.Equipment;
			InventoryEquipment originalEquipment = player?.OriginalInventory?.Inventory?.Equipment;
			if (rangeEquipment == null || originalEquipment == null)
				return;

			RangeSession session = new RangeSession();
			IEnumerable<Slot> originalSlots = GClass3168.GetAllSlots(originalEquipment);
			foreach (Slot rangeSlot in GClass3168.GetAllSlots(rangeEquipment))
			{
				Slot originalSlot = originalSlots.FirstOrDefault(slot => slot.ID == rangeSlot.ID);
				TrackItemTree(rangeSlot.ContainedItem, originalSlot?.ContainedItem, session);
			}

			RangeSessions[player.GetInstanceID()] = session;
		}

		private static void ClearShootingRangeEquipment(InventoryController rangeController)
		{
			if (rangeController?.Inventory?.Equipment == null)
				return;

			foreach (Slot slot in GClass3168.GetAllSlots(rangeController.Inventory.Equipment).ToArray())
			{
				if (slot.ContainedItem == null)
					continue;

				GStruct455<GClass3202> removeResult = InteractionsHandlerClass.RemoveWithoutRestrictions(slot.ContainedItem, rangeController);
				if (removeResult.Failed)
				{
					Debug.LogError(removeResult.Error);
				}
			}
		}

		private static RangePersistenceRequest BuildPersistenceRequest(HideoutPlayer player)
		{
			RangeSession session = GetRangeSession(player);
			Item[] equipmentRoots = GetEquipmentRootItems(player?.ShootingRangeInventory).ToArray();
			LootItem[] looseLoot = BreakContactConfig.ShootingRangeReturnDroppedItems.Value
				? GetOwnedLooseLoot(player)
				: Array.Empty<LootItem>();
			Item[] looseRoots = looseLoot
				.Select(loot => loot?.ItemOwner?.RootItem)
				.Where(item => item != null)
				.ToArray();
			Item[] allRangeRoots = DistinctById(equipmentRoots.Concat(looseRoots)).ToArray();
			HashSet<string> liveRangeIds = new HashSet<string>(
				allRangeRoots.SelectMany(root => GClass3176.GetAllItems(root)).Where(item => item != null).Select(item => item.Id));
			HashSet<string> liveOriginalIds = new HashSet<string>(
				liveRangeIds.Select(id => session.GetOriginalId(id)).Where(id => !string.IsNullOrEmpty(id)));

			GClass1319[] changedItems = Singleton<ItemFactoryClass>.Instance.TreeToFlatItems(allRangeRoots)
				.Select(item => RemapChangedItem(item, session))
				.Where(item => item != null)
				.ToArray();

			GClass1319[] returnedItems = Singleton<ItemFactoryClass>.Instance.TreeToFlatItems(looseRoots);
			string[] returnedRootIds = looseRoots
				.Select(item => session.GetOriginalId(item.Id))
				.Where(id => !string.IsNullOrEmpty(id))
				.ToArray();

			return new RangePersistenceRequest
			{
				sessionId = player?.ProfileId,
				changedItems = changedItems,
				removedItemIds = session.OriginalItemIds.Where(id => !liveOriginalIds.Contains(id)).ToArray(),
				returnedItems = returnedItems,
				returnedRootIds = returnedRootIds,
				messageText = "Returned items left in your shooting range.",
				keepLootIds = new HashSet<string>(looseLoot.Where(loot => !string.IsNullOrEmpty(loot.ItemId)).Select(loot => loot.ItemId))
			};
		}

		private static RangeSession GetRangeSession(HideoutPlayer player)
		{
			if (player != null && RangeSessions.TryGetValue(player.GetInstanceID(), out RangeSession session))
				return session;

			return new RangeSession();
		}

		private static GClass1319 RemapChangedItem(GClass1319 rangeItem, RangeSession session)
		{
			if (rangeItem == null)
				return null;

			string originalId = session.GetOriginalId(GetMongoIdString(rangeItem._id));
			if (string.IsNullOrEmpty(originalId))
				return null;

			GClass1319 changedItem = new GClass1319
			{
				_id = new MongoID(originalId),
				_tpl = rangeItem._tpl,
				slotId = rangeItem.slotId,
				location = rangeItem.location,
				upd = rangeItem.upd
			};

			string originalParentId = rangeItem.parentId.HasValue
				? session.GetOriginalId(GetMongoIdString(rangeItem.parentId.Value))
				: null;
			if (!string.IsNullOrEmpty(originalParentId))
			{
				changedItem.parentId = new MongoID(originalParentId);
			}

			return changedItem;
		}

		private static IEnumerable<Item> GetEquipmentRootItems(InventoryController controller)
		{
			Slot[] slots = controller?.Inventory?.Equipment?.Slots;
			if (slots == null)
				yield break;

			foreach (Slot slot in slots)
			{
				if (slot?.ContainedItem != null)
				{
					yield return slot.ContainedItem;
				}
			}
		}

		private static IEnumerable<Item> DistinctById(IEnumerable<Item> items)
		{
			HashSet<string> seenIds = new HashSet<string>();
			foreach (Item item in items)
			{
				if (item == null || string.IsNullOrEmpty(item.Id) || !seenIds.Add(item.Id))
					continue;

				yield return item;
			}
		}

		private static string GetMongoIdString(MongoID id)
		{
			return id;
		}

		private static void TrackItemTree(Item rangeItem, Item originalItem, RangeSession session)
		{
			if (rangeItem == null || originalItem == null || session == null)
				return;

			if (rangeItem.TemplateId != originalItem.TemplateId)
				return;

			if (!string.IsNullOrEmpty(rangeItem.Id) && !string.IsNullOrEmpty(originalItem.Id))
			{
				session.RangeToOriginalItemIds[rangeItem.Id] = originalItem.Id;
				session.OriginalItemIds.Add(originalItem.Id);
			}

			if (!(rangeItem is GClass3050 rangeCollection) || !(originalItem is GClass3050 originalCollection))
				return;

			foreach (EFT.InventoryLogic.IContainer rangeContainer in rangeCollection.Containers)
			{
				EFT.InventoryLogic.IContainer originalContainer = originalCollection.GetContainer(rangeContainer.ID);
				TrackContainerItems(rangeContainer, originalContainer, session);
			}
		}

		private static void TrackContainerItems(EFT.InventoryLogic.IContainer rangeContainer, EFT.InventoryLogic.IContainer originalContainer, RangeSession session)
		{
			if (rangeContainer == null || originalContainer == null)
				return;

			Item[] rangeItems = rangeContainer.Items.Where(item => item != null).ToArray();
			Item[] originalItems = originalContainer.Items.Where(item => item != null).ToArray();
			int originalIndex = 0;
			for (int rangeIndex = 0; rangeIndex < rangeItems.Length; rangeIndex++)
			{
				Item rangeItem = rangeItems[rangeIndex];
				Item originalItem = null;
				while (originalIndex < originalItems.Length)
				{
					Item candidate = originalItems[originalIndex++];
					if (candidate.TemplateId == rangeItem.TemplateId)
					{
						originalItem = candidate;
						break;
					}
				}

				TrackItemTree(rangeItem, originalItem, session);
			}
		}

		private static LootItem[] GetOwnedLooseLoot(HideoutPlayer player)
		{
			GameWorld gameWorld = player?.GameWorld;
			if (gameWorld == null)
				return Array.Empty<LootItem>();

			return gameWorld.LootList
				.OfType<LootItem>()
				.Where(loot => loot != null
					&& loot.LastOwner != null
					&& loot.LastOwner.ProfileId == player.ProfileId
					&& loot.ItemOwner?.RootItem != null)
				.ToArray();
		}

		private static async Task<bool> TryPersistRangeStateAsync(HideoutPlayer player, RangePersistenceRequest body)
		{
			if (body == null || !body.HasPayload())
				return true;

			try
			{
				string backendUrl = BackendConfigAbstractClass.BackendUrl?.TrimEnd('/');
				if (string.IsNullOrEmpty(backendUrl))
					return false;

				string json = GClass826.ToJson(body);
				byte[] data = Encoding.UTF8.GetBytes(json);

				using (UnityWebRequest request = new UnityWebRequest(backendUrl + PersistRoute, "POST"))
				{
					request.uploadHandler = new UploadHandlerRaw(data);
					request.downloadHandler = new DownloadHandlerBuffer();
					request.timeout = 10;
					request.SetRequestHeader("Content-Type", "application/json");
					request.SetRequestHeader("GClient-RequestId", (++GClass630.NextRequestIndex).ToString());

					if (!string.IsNullOrEmpty(GClass630.AppVersion))
					{
						request.SetRequestHeader("App-Version", GClass630.AppVersion);
					}

					string phpSessionId = GClass630.PhpSessionId;
					if (string.IsNullOrEmpty(phpSessionId))
					{
						phpSessionId = Singleton<HideoutClass>.Instance?.iSession?.GetPhpSessionId();
					}

					if (!string.IsNullOrEmpty(phpSessionId))
					{
						request.SetRequestHeader("Cookie", "PHPSESSID=" + phpSessionId);
					}

					UnityWebRequestAsyncOperation operation = request.SendWebRequest();
					while (!operation.isDone)
					{
						await Task.Yield();
					}

					if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
					{
						Debug.LogWarning($"Break Contact shooting range persist failed: HTTP {(int)request.responseCode} {request.error} {request.downloadHandler?.text}");
						return false;
					}
				}

				if (body.returnedRootIds.Length > 0)
				{
					NotificationManagerClass.DisplayWarningNotification($"Returned {body.returnedRootIds.Length} shooting range item(s) by messenger.");
				}

				return true;
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Break Contact shooting range persist failed. " + ex);
				return false;
			}
		}

		private static void CleanupWorldLoot(HideoutPlayer player, HashSet<string> keepLootIds)
		{
			GameWorld gameWorld = player?.GameWorld;
			if (gameWorld == null)
				return;

			foreach (LootItem loot in gameWorld.LootList.OfType<LootItem>().ToArray())
			{
				if (loot == null)
					continue;

				if (!string.IsNullOrEmpty(loot.ItemId) && keepLootIds.Contains(loot.ItemId))
					continue;

				gameWorld.DestroyLoot(loot);
			}
		}

		private static void SetPersistentRange(HideoutPlayer player, bool active)
		{
			if (player == null)
				return;

			int id = player.GetInstanceID();
			if (active)
			{
				PersistentRangePlayers.Add(id);
			}
			else
			{
				PersistentRangePlayers.Remove(id);
			}
		}

		private static void SetShootingRangeModeFlag(HideoutPlayer player, bool value)
		{
			UsesShootingRangeInventoryField?.SetValue(player, value);
		}

		private sealed class RangeSession
		{
			public readonly Dictionary<string, string> RangeToOriginalItemIds = new Dictionary<string, string>();
			public readonly HashSet<string> OriginalItemIds = new HashSet<string>();

			public string GetOriginalId(string rangeId)
			{
				if (string.IsNullOrEmpty(rangeId))
					return null;

				return RangeToOriginalItemIds.TryGetValue(rangeId, out string originalId) ? originalId : null;
			}
		}

		[Serializable]
		private sealed class RangePersistenceRequest
		{
			public string sessionId;
			public GClass1319[] changedItems = Array.Empty<GClass1319>();
			public string[] removedItemIds = Array.Empty<string>();
			public GClass1319[] returnedItems = Array.Empty<GClass1319>();
			public string[] returnedRootIds = Array.Empty<string>();
			public string messageText;

			[NonSerialized]
			public HashSet<string> keepLootIds = new HashSet<string>();

			public bool HasPayload()
			{
				return changedItems.Length > 0 || removedItemIds.Length > 0 || returnedItems.Length > 0 || returnedRootIds.Length > 0;
			}
		}
	}
}

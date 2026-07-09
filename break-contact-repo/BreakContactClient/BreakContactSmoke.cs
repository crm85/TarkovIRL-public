using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BreakContact.Configs;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BreakContact
{
	public class SainBreakContactSmokePatch : ModulePatch
	{
		private const string SainDecisionManagerTypeName = "SAIN.SAINComponent.Classes.Decision.BotDecisionManager";
		private static bool _warnedRuntimeFailure;

		public static void TryEnable()
		{
			if (AccessTools.TypeByName(SainDecisionManagerTypeName) == null)
			{
				return;
			}

			new SainBreakContactSmokePatch().Enable();
			Debug.Log("[BreakContact] Enabled SAIN break contact smoke patch.");
		}

		protected override MethodBase GetTargetMethod()
		{
			Type decisionManagerType = AccessTools.TypeByName(SainDecisionManagerTypeName);
			return AccessTools.GetDeclaredMethods(decisionManagerType)
				.FirstOrDefault(method => method.Name == "SetDecisions"
					&& method.GetParameters().Length == 3);
		}

		[PatchPrefix]
		private static void Prefix(object __instance, out string __state)
		{
				__state = BreakContactSmokeThrower.GetCurrentDecisionName(__instance);
		}

		[PatchPostfix]
		private static void Postfix(object __instance, string __state)
		{
			try
			{
				BreakContactSmokeThrower.OnSainDecisionUpdated(__instance, __state);
			}
			catch (Exception ex)
			{
				if (_warnedRuntimeFailure) return;

				Debug.LogWarning("[BreakContact] SAIN break contact smoke patch failed at runtime; disabling logs for further failures. " + ex);
				_warnedRuntimeFailure = true;
			}
		}
	}

	internal static class BreakContactSmokeThrower
	{
		private const string RetreatDecisionName = "Retreat";
		private const float SmokeMinThrowDistanceSqr = 0.1f;
		private const float MovementPauseSeconds = 1.35f;
		private const float EmergencyThrowForce = 6f;
		private const float EmergencyThrowAngle = 30f;

		private static readonly AIGreandeAng[] ThrowAngles =
		{
			AIGreandeAng.ang25,
			AIGreandeAng.ang45,
			AIGreandeAng.ang65
		};

		private static readonly Dictionary<BotOwner, float> NextAttemptTimes = new Dictionary<BotOwner, float>();
		private static PropertyInfo _botOwnerProperty;
		private static PropertyInfo _currentDecisionProperty;
		private static float _nextCleanupTime;

		internal static void OnSainDecisionUpdated(object decisionManager, string previousDecision)
		{
			if (!BreakContactConfig.EnableBreakContactSmoke.Value) return;
			if (!BreakContactPlugin.InGame()) return;

			string currentDecision = GetCurrentDecisionName(decisionManager);
			if (!IsRetreatDecision(currentDecision) || IsRetreatDecision(previousDecision))
			{
				return;
			}

			BotOwner botOwner = GetBotOwner(decisionManager);
			if (botOwner == null) return;

			if (TryThrowBreakContactSmoke(botOwner, out string reason))
			{
				LogDebug(botOwner, "forced break contact smoke throw.");
			}
			else
			{
				LogDebug(botOwner, "did not throw break contact smoke: " + reason);
			}
		}

		internal static string GetCurrentDecisionName(object decisionManager)
		{
			if (decisionManager == null) return string.Empty;

			if (_currentDecisionProperty == null)
			{
				_currentDecisionProperty = AccessTools.Property(decisionManager.GetType(), "CurrentCombatDecision");
			}

			object value = _currentDecisionProperty?.GetValue(decisionManager, null);
			return value?.ToString() ?? string.Empty;
		}

		private static BotOwner GetBotOwner(object decisionManager)
		{
			if (decisionManager == null) return null;

			if (_botOwnerProperty == null)
			{
				_botOwnerProperty = AccessTools.Property(decisionManager.GetType(), "BotOwner");
			}

			return _botOwnerProperty?.GetValue(decisionManager, null) as BotOwner;
		}

		private static bool TryThrowBreakContactSmoke(BotOwner botOwner, out string reason)
		{
			CleanupCooldowns();

			if (botOwner == null)
			{
				reason = "missing bot";
				return false;
			}

			if (NextAttemptTimes.TryGetValue(botOwner, out float nextAttemptTime) && nextAttemptTime > Time.time)
			{
				reason = "cooldown";
				return false;
			}

			BotWeaponManager weaponManager = botOwner.WeaponManager;
			if (weaponManager == null || weaponManager.Grenades == null)
			{
				reason = "missing weapon manager";
				return false;
			}

			BotGrenadeController grenades = weaponManager.Grenades;
			if (grenades.ThrowindNow)
			{
				reason = "already throwing";
				return false;
			}

			if (!grenades.HaveGrenadeOfType(ThrowWeapType.smoke_grenade))
			{
				reason = "no smoke grenade";
				return false;
			}

			if (!CanUseHands(botOwner, weaponManager, out reason))
			{
				return false;
			}

			float chance = Mathf.Clamp01(BreakContactConfig.BreakContactSmokeChance.Value);
			if (chance <= 0f || UnityEngine.Random.value > chance)
			{
				SetCooldown(botOwner);
				reason = "chance roll";
				return false;
			}

			grenades.method_1(ThrowWeapType.smoke_grenade);
			if (!grenades.HaveGrenade)
			{
				reason = "failed to select smoke";
				return false;
			}

			Vector3 from = botOwner.Position + BotOwner.STAY_HEIGHT;
			Vector3 target = GetSmokeTarget(botOwner, from);
			if (!grenades.method_8(target))
			{
				SetCooldown(botOwner);
				reason = "friendly near smoke target";
				return false;
			}

			if (!TryCreateThrowData(botOwner, grenades, from, target, out AIGreanageThrowData throwData))
			{
				SetCooldown(botOwner);
				reason = "no trajectory";
				return false;
			}

			throwData.GrenadeType = ThrowWeapType.smoke_grenade;
			if (!grenades.SetThrowData(throwData))
			{
				SetCooldown(botOwner);
				reason = "throw data rejected";
				return false;
			}

			botOwner.StopMove();
			botOwner.Mover.SprintPause(MovementPauseSeconds);
			botOwner.Mover.MovementPause(MovementPauseSeconds, false);

			bool threw = grenades.DoThrow();
			SetCooldown(botOwner);
			reason = threw ? "threw" : "DoThrow returned false";
			return threw;
		}

		private static bool CanUseHands(BotOwner botOwner, BotWeaponManager weaponManager, out string reason)
		{
			if (botOwner.GetPlayer == null || !botOwner.GetPlayer.HealthController.IsAlive)
			{
				reason = "dead";
				return false;
			}

			if (weaponManager.Selector != null && weaponManager.Selector.IsChanging)
			{
				reason = "changing weapon";
				return false;
			}

			if (weaponManager.Reload != null && weaponManager.Reload.Reloading)
			{
				reason = "reloading";
				return false;
			}

			if (botOwner.Medecine != null && botOwner.Medecine.Using)
			{
				reason = "using medicine";
				return false;
			}

			if (botOwner.GetPlayer.HandsController != null
				&& botOwner.GetPlayer.HandsController.IsInInteractionStrictCheck())
			{
				reason = "hands busy";
				return false;
			}

			reason = string.Empty;
			return true;
		}

		private static bool TryCreateThrowData(
			BotOwner botOwner,
			BotGrenadeController grenades,
			Vector3 from,
			Vector3 target,
			out AIGreanageThrowData throwData)
		{
			Vector3[] targets = BuildTargetCandidates(botOwner, target);
			for (int i = 0; i < targets.Length; i++)
			{
				for (int angleIndex = 0; angleIndex < ThrowAngles.Length; angleIndex++)
				{
					AIGreanageThrowData candidate = GClass557.CanThrowGrenade2(
						from,
						targets[i],
						grenades,
						ThrowAngles[angleIndex],
						SmokeMinThrowDistanceSqr);

					if (candidate != null && candidate.CanThrow)
					{
						candidate.GrenadeType = ThrowWeapType.smoke_grenade;
						throwData = candidate;
						return true;
					}
				}
			}

			if (BreakContactConfig.BreakContactSmokeAllowEmergencyToss.Value)
			{
				throwData = CreateEmergencyThrowData(botOwner, from, target);
				return true;
			}

			throwData = null;
			return false;
		}

		private static Vector3[] BuildTargetCandidates(BotOwner botOwner, Vector3 target)
		{
			Vector3 botPosition = botOwner.Position;
			Vector3 flat = target - botPosition;
			flat.y = 0f;

			if (flat.sqrMagnitude < 0.01f)
			{
				return new[]
				{
					target,
					target + Vector3.up * 0.5f
				};
			}

			Vector3 direction = flat.normalized;
			float distance = flat.magnitude;
			Vector3 near = botPosition + direction * Mathf.Max(2f, distance * 0.65f) + Vector3.up * 0.35f;
			Vector3 far = botPosition + direction * Mathf.Min(distance + 2f, BreakContactConfig.BreakContactSmokeThrowDistance.Value) + Vector3.up * 0.35f;

			return new[]
			{
				target,
				target + Vector3.up * 0.5f,
				near,
				far
			};
		}

		private static AIGreanageThrowData CreateEmergencyThrowData(BotOwner botOwner, Vector3 from, Vector3 target)
		{
			Vector3 direction = target - from;
			if (direction.sqrMagnitude < 0.01f)
			{
				direction = botOwner.LookDirection;
			}

			return new AIGreanageThrowData(
				EmergencyThrowAngle,
				EmergencyThrowForce,
				direction,
				from,
				target,
				true,
				ThrowWeapType.smoke_grenade);
		}

		private static Vector3 GetSmokeTarget(BotOwner botOwner, Vector3 from)
		{
			Vector3 botPosition = botOwner.Position;
			if (TryGetEnemyPosition(botOwner, out Vector3 enemyPosition))
			{
				Vector3 toEnemy = enemyPosition - botPosition;
				toEnemy.y = 0f;
				if (toEnemy.sqrMagnitude > 0.01f)
				{
					float distance = toEnemy.magnitude;
					float maxThrowDistance = BreakContactConfig.BreakContactSmokeThrowDistance.Value;
					float targetDistance = Mathf.Clamp(distance * 0.45f, 2f, maxThrowDistance);
					return botPosition + toEnemy.normalized * targetDistance + Vector3.up * 0.35f;
				}
			}

			Vector3 lookDirection = botOwner.LookDirection;
			lookDirection.y = 0f;
			if (lookDirection.sqrMagnitude < 0.01f)
			{
				lookDirection = Vector3.forward;
			}

			return from + lookDirection.normalized * Mathf.Min(4f, BreakContactConfig.BreakContactSmokeThrowDistance.Value);
		}

		private static bool TryGetEnemyPosition(BotOwner botOwner, out Vector3 enemyPosition)
		{
			try
			{
				if (botOwner.Memory != null && botOwner.Memory.GoalEnemy != null)
				{
					enemyPosition = botOwner.Memory.GoalEnemy.CurrPosition;
					if (enemyPosition != Vector3.zero)
					{
						return true;
					}

					enemyPosition = botOwner.Memory.GoalEnemy.EnemyLastPositionReal;
					return enemyPosition != Vector3.zero;
				}
			}
			catch
			{
			}

			enemyPosition = Vector3.zero;
			return false;
		}

		private static bool IsRetreatDecision(string decisionName)
		{
			return string.Equals(decisionName, RetreatDecisionName, StringComparison.Ordinal);
		}

		private static void SetCooldown(BotOwner botOwner)
		{
			NextAttemptTimes[botOwner] = Time.time + Mathf.Max(1f, BreakContactConfig.BreakContactSmokeCooldown.Value);
		}

		private static void CleanupCooldowns()
		{
			if (_nextCleanupTime > Time.time) return;

			_nextCleanupTime = Time.time + 30f;
			List<BotOwner> staleBots = null;
			foreach (BotOwner botOwner in NextAttemptTimes.Keys)
			{
				if (botOwner != null) continue;

				if (staleBots == null)
				{
					staleBots = new List<BotOwner>();
				}

				staleBots.Add(botOwner);
			}

			if (staleBots == null) return;

			for (int i = 0; i < staleBots.Count; i++)
			{
				NextAttemptTimes.Remove(staleBots[i]);
			}
		}

		private static void LogDebug(BotOwner botOwner, string message)
		{
			if (!BreakContactConfig.BreakContactSmokeDebugLogging.Value) return;

			string botName = botOwner != null
				? botOwner.name
				: "unknown bot";
			Debug.Log("[BreakContact] " + botName + " " + message);
		}
	}
}

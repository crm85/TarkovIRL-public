using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BreakContact.Configs;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using Systems.Effects;
using UnityEngine;

namespace BreakContact
{
	public class BaseGrenadeHandsControllerCreateGrenadePatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(
				typeof(Player.BaseGrenadeHandsController),
				"method_10",
				new[] { typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(float) });
		}

		[PatchPostfix]
		private static void Postfix(
			Player.BaseGrenadeHandsController __instance,
			EFT.Grenade __result,
			float prewarm)
		{
			if (__instance == null || __result == null) return;

			SmokeVisualReplacement.TryScheduleM18DelayedReplacement(
				__result,
				__instance.Item,
				prewarm,
				"BaseGrenadeHandsController.method_10");
		}
	}

	public class GrenadeInitPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.GetDeclaredMethods(typeof(EFT.Grenade)).FirstOrDefault(method =>
			{
				if (method.Name != nameof(EFT.Grenade.Init)) return false;

				ParameterInfo[] parameters = method.GetParameters();
				return parameters.Length == 6
					&& parameters[0].ParameterType.Name == "GrenadeSettings"
					&& parameters[1].ParameterType == typeof(string)
					&& parameters[2].ParameterType.Name == "ThrowWeapItemClass"
					&& parameters[3].ParameterType == typeof(float)
					&& parameters[4].ParameterType.Name == "ISharedBallisticsCalculator"
					&& parameters[5].ParameterType == typeof(bool);
			});
		}

		[PatchPostfix]
		private static void Postfix(EFT.Grenade __instance, ThrowWeapItemClass throwWeap, float timeSpent)
		{
			SmokeVisualReplacement.TryScheduleM18DelayedReplacement(__instance, throwWeap, timeSpent, "Grenade.Init");
		}
	}

	public class SmokeGrenadeInitPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.GetDeclaredMethods(typeof(SmokeGrenade)).FirstOrDefault(method =>
			{
				if (method.Name != nameof(SmokeGrenade.Init)) return false;

				ParameterInfo[] parameters = method.GetParameters();
				return parameters.Length == 6
					&& parameters[0].ParameterType.Name == "GrenadeSettings"
					&& parameters[1].ParameterType == typeof(string)
					&& parameters[2].ParameterType.Name == "ThrowWeapItemClass"
					&& parameters[3].ParameterType == typeof(float)
					&& parameters[4].ParameterType.Name == "ISharedBallisticsCalculator"
					&& parameters[5].ParameterType == typeof(bool);
			});
		}

		[PatchPostfix]
		private static void Postfix(SmokeGrenade __instance, float timeSpent)
		{
			if (__instance == null) return;

			SmokeVisualReplacement.LogSmokeHook("Init", __instance);
			SmokeVisualReplacement.TryScheduleM18DelayedReplacement(__instance, __instance.WeaponSource, timeSpent, "SmokeGrenade.Init");
		}
	}

	public class SmokeGrenadeSetEmissionPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(SmokeGrenade), nameof(SmokeGrenade.SetEmission));
		}

		[PatchPostfix]
		private static void Postfix(SmokeGrenade __instance, float time)
		{
			if (__instance == null) return;

			SmokeVisualReplacement.LogSmokeHook("SetEmission", __instance);
			SmokeVisualReplacement.TryScheduleM18DelayedReplacement(__instance, __instance.WeaponSource, time, "SmokeGrenade.SetEmission");
		}
	}

	public class SmokeGrenadeExplosionPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(SmokeGrenade), nameof(SmokeGrenade.OnExplosion));
		}

		[PatchPostfix]
		private static void Postfix(SmokeGrenade __instance)
		{
			if (__instance == null) return;

			SmokeVisualReplacement.LogSmokeHook("OnExplosion", __instance);
			if (BreakContactConfig.EnableSmokeOccluders.Value
				&& __instance.GetComponent<SmokeOcclusionController>() == null)
			{
				__instance.gameObject.AddComponent<SmokeOcclusionController>().Init(__instance);
			}

			SmokeVisualReplacement.TryScheduleM18DelayedReplacement(__instance, __instance.WeaponSource, 0.5f, "SmokeGrenade.OnExplosion");
		}
	}

	public class M18SmokeEffectSuppressionPatch : ModulePatch
	{
		private static bool _loggedSuppression;

		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(Effects), nameof(Effects.EmitGrenade),
				new[] { typeof(string), typeof(Vector3), typeof(Vector3) })
				?? AccessTools.GetDeclaredMethods(typeof(Effects)).FirstOrDefault(method =>
					method.Name == nameof(Effects.EmitGrenade)
					&& IsGrenadeEffectMethod(method));
		}

		[PatchPrefix]
		private static bool Prefix(Effects __instance, string ename, Vector3 position)
		{
			if (!BreakContactConfig.ReplaceM18SmokeVisual.Value) return true;

			SmokeVisualReplacement.LogEmitGrenadeHook(ename, position);
			if (!SmokeVisualReplacement.IsM18SmokeEffect(ename)) return true;

			bool replaced = SmokeVisualReplacement.TrySpawnM18PointVisualReplacement(__instance, position, 0f);
			if (replaced && !_loggedSuppression)
			{
				Debug.Log("[BreakContact] Suppressing vanilla M18 smoke effect; using Russian-style smoke replacement.");
				_loggedSuppression = true;
			}

			return !replaced;
		}

		private static bool IsGrenadeEffectMethod(MethodInfo method)
		{
			ParameterInfo[] parameters = method.GetParameters();
			return (parameters.Length == 3 || parameters.Length == 4)
				&& parameters[0].ParameterType == typeof(string)
				&& parameters[1].ParameterType == typeof(Vector3)
				&& parameters[2].ParameterType == typeof(Vector3);
		}
	}

	public class M18SmokeEmissionSuppressionPatch : ModulePatch
	{
		private static bool _loggedSuppression;

		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(Effects), nameof(Effects.GetEmissionEffect),
				new[] { typeof(string) });
		}

		[PatchPostfix]
		private static void Postfix(string key, ref GrenadeEmission __result)
		{
			if (__result == null) return;
			if (!SmokeVisualReplacement.ShouldSuppressM18VanillaVisual(key)) return;

			if (!_loggedSuppression)
			{
				Debug.Log("[BreakContact] Suppressing vanilla M18 smoke emission; using Russian-style smoke replacement.");
				_loggedSuppression = true;
			}

			M18VanillaSmokeSuppressor.Suppress(__result);
		}
	}

	public class M18SmokeEmissionStartSuppressionPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(GrenadeEmission), nameof(GrenadeEmission.StartEmission),
				new[] { typeof(float) });
		}

		[PatchPrefix]
		private static bool Prefix(GrenadeEmission __instance)
		{
			if (!SmokeVisualReplacement.ShouldSuppressM18VanillaEmission(__instance)) return true;

			M18VanillaSmokeSuppressor.Suppress(__instance);
			return false;
		}
	}

	public class M18SmokeEmissionRendererSuppressionPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(GrenadeEmission), nameof(GrenadeEmission.EnableRenderers));
		}

		[PatchPrefix]
		private static bool Prefix(GrenadeEmission __instance, bool value)
		{
			if (!value) return true;
			if (!SmokeVisualReplacement.ShouldSuppressM18VanillaEmission(__instance)) return true;

			M18VanillaSmokeSuppressor.Suppress(__instance);
			return false;
		}
	}

	internal static class SmokeVisualReplacement
	{
		private sealed class RecentReplacement
		{
			public Vector3 Position;
			public float Time;
		}

		internal const string M18TemplateId = "617aa4dd8166f034d57de9c5";
		internal const string M18SmokeEffectType = "grenade_smoke_m18_green";
		internal const string RussianSmokeLoopEffectType = "grenade_smoke_russian_loop";

		private static readonly HashSet<string> M18SmokeEmissionEffectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private static readonly List<RecentReplacement> RecentReplacements = new List<RecentReplacement>();
		private static string _russianSmokeEmissionKey = RussianSmokeLoopEffectType;
		private static bool _russianSmokeEmissionAvailable;
		private static bool _warnedRussianSmokeUnavailable;
		private static float _nextRussianSmokeProbeTime;
		private static int _grenadeHookDiagnosticCount;
		private static int _smokeHookDiagnosticCount;
		private static int _emitGrenadeDiagnosticCount;

		internal static string RussianSmokeEmissionKey => _russianSmokeEmissionKey;

		internal static bool TryScheduleM18DelayedReplacement(
			EFT.Grenade grenade,
			ThrowWeapItemClass source,
			float elapsedSinceThrow,
			string hook)
		{
			if (!BreakContactConfig.ReplaceM18SmokeVisual.Value || grenade == null) return false;

			source = source ?? grenade.WeaponSource;
			SmokeGrenade smokeGrenade = grenade as SmokeGrenade;
			bool isM18 = IsM18ThrowWeapon(source) || IsM18SmokeGrenade(smokeGrenade);
			LogGrenadeHook(hook, grenade, source, elapsedSinceThrow, isM18);
			if (!isM18) return false;

			RegisterM18SmokeEmissionKey(smokeGrenade);

			M18DelayedSmokeSwapController controller = grenade.GetComponent<M18DelayedSmokeSwapController>();
			if (controller == null)
			{
				controller = grenade.gameObject.AddComponent<M18DelayedSmokeSwapController>();
			}

			controller.Init(grenade, smokeGrenade, elapsedSinceThrow, hook);
			return true;
		}

		internal static bool TryAttachM18VisualReplacement(SmokeGrenade grenade, float prewarm)
		{
			if (!ShouldReplaceM18Visual(grenade)) return false;

			RegisterM18SmokeEmissionKey(grenade);
			if (!TryEnsureRussianSmokeEmission()) return false;

			if (grenade.GetComponent<M18VanillaSmokeSuppressor>() == null)
			{
				grenade.gameObject.AddComponent<M18VanillaSmokeSuppressor>().Init(grenade);
			}

			if (grenade.GetComponent<M18RussianSmokeController>() == null
				&& !HasRecentReplacementNear(grenade.transform.position))
			{
				grenade.gameObject.AddComponent<M18RussianSmokeController>().Init(grenade, prewarm);
				RegisterRecentReplacement(grenade.transform.position);
				Debug.Log("[BreakContact] Attached Russian-style M18 smoke replacement to grenade. template="
					+ GetWeaponSourceTemplateId(grenade.WeaponSource)
					+ " effect=" + grenade.WeaponSource?.ExplosionEffectType
					+ " emission=" + RussianSmokeEmissionKey + ".");
			}

			return true;
		}

		internal static bool TrySpawnM18PointVisualReplacement(Effects effects, Vector3 position, float prewarm)
		{
			if (!BreakContactConfig.ReplaceM18SmokeVisual.Value) return false;
			if (HasRecentReplacementNear(position)) return true;
			if (!TryEnsureRussianSmokeEmission()) return false;

			bool spawned = M18RussianSmokeController.TrySpawnAt(
				effects,
				position,
				90f,
				SmokeGrenadeDefaultRadius(),
				prewarm);
			if (!spawned) return false;

			RegisterRecentReplacement(position);
			Debug.Log("[BreakContact] Spawned direct Russian-style M18 smoke replacement. emission="
				+ RussianSmokeEmissionKey + ".");
			return true;
		}

		internal static bool ShouldReplaceM18Visual(SmokeGrenade grenade)
		{
			return BreakContactConfig.ReplaceM18SmokeVisual.Value
				&& IsM18SmokeGrenade(grenade);
		}

		internal static bool IsM18SmokeGrenade(SmokeGrenade grenade)
		{
			if (grenade?.WeaponSource == null) return false;

			return IsM18ThrowWeapon(grenade.WeaponSource);
		}

		internal static bool IsM18ThrowWeapon(ThrowWeapItemClass source)
		{
			if (source == null) return false;

			return IsM18TemplateId(GetWeaponSourceTemplateId(source))
				|| IsM18SmokeEffect(source.ExplosionEffectType);
		}

		internal static void LogGrenadeHook(
			string hook,
			EFT.Grenade grenade,
			ThrowWeapItemClass source,
			float elapsedSinceThrow,
			bool isM18)
		{
			if (!BreakContactConfig.ReplaceM18SmokeVisual.Value) return;
			if (!isM18 && _grenadeHookDiagnosticCount >= 24) return;

			_grenadeHookDiagnosticCount++;
			Debug.Log("[BreakContact] Grenade hook " + hook
				+ " type=" + grenade?.GetType().FullName
				+ " m18=" + isM18
				+ " profile=" + grenade?.ProfileId
				+ " sourceNull=" + (source == null)
				+ " template=" + GetWeaponSourceTemplateId(source)
				+ " sourceId=" + source?.Id
				+ " name=" + source?.Name
				+ " throwType=" + (source != null ? source.ThrowType.ToString() : "<null>")
				+ " explosion=" + source?.ExplosionEffectType
				+ " elapsed=" + elapsedSinceThrow.ToString("0.00") + ".");
		}

		internal static void LogSmokeHook(string hook, SmokeGrenade grenade)
		{
			if (!BreakContactConfig.ReplaceM18SmokeVisual.Value) return;

			bool isM18 = IsM18SmokeGrenade(grenade);
			if (!isM18 && _smokeHookDiagnosticCount >= 24) return;

			_smokeHookDiagnosticCount++;
			ThrowWeapItemClass source = grenade?.WeaponSource;
			SmokeGrenadeSettings settings = grenade?.SmokeGrenadeSettings_0;
			Debug.Log("[BreakContact] Smoke hook " + hook
				+ " type=" + grenade?.GetType().FullName
				+ " m18=" + isM18
				+ " profile=" + grenade?.ProfileId
				+ " sourceNull=" + (source == null)
				+ " template=" + GetWeaponSourceTemplateId(source)
				+ " sourceId=" + source?.Id
				+ " name=" + source?.Name
				+ " throwType=" + (source != null ? source.ThrowType.ToString() : "<null>")
				+ " explosion=" + source?.ExplosionEffectType
				+ " emission=" + settings?.EmmisionEffect
				+ " emitTime=" + grenade?.EmitTime
				+ " lifeTime=" + grenade?.LifeTime + ".");
		}

		internal static void LogEmitGrenadeHook(string effectName, Vector3 position)
		{
			if (!BreakContactConfig.ReplaceM18SmokeVisual.Value) return;
			if (string.IsNullOrEmpty(effectName)) return;
			if (effectName.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) < 0
				&& effectName.IndexOf("m18", StringComparison.OrdinalIgnoreCase) < 0
				&& _emitGrenadeDiagnosticCount >= 24)
			{
				return;
			}

			if (_emitGrenadeDiagnosticCount >= 48
				&& !IsM18SmokeEffect(effectName)
				&& !LooksLikeM18SmokeEmissionEffect(effectName))
			{
				return;
			}

			_emitGrenadeDiagnosticCount++;
			Debug.Log("[BreakContact] Effects.EmitGrenade effect=" + effectName
				+ " m18Effect=" + IsM18SmokeEffect(effectName)
				+ " position=" + position + ".");
		}

		private static bool IsM18TemplateId(string templateId)
		{
			return string.Equals(templateId, M18TemplateId, StringComparison.OrdinalIgnoreCase);
		}

		private static string GetWeaponSourceTemplateId(ThrowWeapItemClass source)
		{
			if (source == null) return null;
			if (!string.IsNullOrEmpty(source.StringTemplateId)) return source.StringTemplateId;

			return source.TemplateId.ToString();
		}

		internal static bool IsM18SmokeEffect(string effectType)
		{
			return string.Equals(effectType, M18SmokeEffectType, StringComparison.OrdinalIgnoreCase);
		}

		internal static bool IsM18SmokeEmissionEffect(string effectType)
		{
			return !string.IsNullOrEmpty(effectType)
				&& M18SmokeEmissionEffectTypes.Contains(effectType);
		}

		internal static bool LooksLikeM18SmokeEmissionEffect(string effectType)
		{
			return !string.IsNullOrEmpty(effectType)
				&& effectType.IndexOf("m18", StringComparison.OrdinalIgnoreCase) >= 0
				&& effectType.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		internal static bool IsRussianSmokeEmission(GrenadeEmission emission)
		{
			return emission != null
				&& string.Equals(emission.PoolName, RussianSmokeEmissionKey, StringComparison.OrdinalIgnoreCase);
		}

		internal static bool ShouldSuppressM18VanillaVisual(string effectType)
		{
			return BreakContactConfig.ReplaceM18SmokeVisual.Value
				&& (IsM18SmokeEffect(effectType) || IsM18SmokeEmissionEffect(effectType) || LooksLikeM18SmokeEmissionEffect(effectType))
				&& TryEnsureRussianSmokeEmission();
		}

		internal static bool ShouldSuppressM18VanillaEmission(GrenadeEmission emission)
		{
			return emission != null
				&& ShouldSuppressM18VanillaVisual(emission.PoolName);
		}

		private static void RegisterM18SmokeEmissionKey(SmokeGrenade grenade)
		{
			string key = grenade?.SmokeGrenadeSettings_0?.EmmisionEffect;
			if (string.IsNullOrEmpty(key)) return;
			if (!M18SmokeEmissionEffectTypes.Add(key)) return;

			Debug.Log("[BreakContact] Registered M18 vanilla smoke emission key '" + key + "'.");
		}

		internal static float GetApproxSmokeRadius(SmokeGrenade grenade)
		{
			if (grenade == null) return 2f;

			float radius = Mathf.Max(1f, grenade.Radius);
			SmokeGrenadeSettings settings = grenade.SmokeGrenadeSettings_0;
			if (settings != null)
			{
				float settingsRadius = settings._initialRadius * Mathf.Max(0.01f, settings._radiusMultiplier);
				radius = Mathf.Max(radius, settingsRadius);

				SphereCollider area = settings._emissionArea;
				if (area != null)
				{
					Vector3 scale = area.transform.lossyScale;
					float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
					radius = Mathf.Max(radius, area.radius * maxScale);
				}
			}

			return Mathf.Clamp(radius, 1.5f, 12f);
		}

		private static float SmokeGrenadeDefaultRadius()
		{
			return Mathf.Clamp(BreakContactConfig.M18RussianPlumeScale.Value * 2.5f, 2.5f, 12f);
		}

		private static bool HasRecentReplacementNear(Vector3 position)
		{
			PruneRecentReplacements();
			for (int i = 0; i < RecentReplacements.Count; i++)
			{
				if ((RecentReplacements[i].Position - position).sqrMagnitude <= 4f) return true;
			}

			return false;
		}

		private static void RegisterRecentReplacement(Vector3 position)
		{
			PruneRecentReplacements();
			RecentReplacements.Add(new RecentReplacement
			{
				Position = position,
				Time = Time.time
			});
		}

		private static void PruneRecentReplacements()
		{
			float cutoff = Time.time - 2.5f;
			for (int i = RecentReplacements.Count - 1; i >= 0; i--)
			{
				if (RecentReplacements[i].Time < cutoff)
				{
					RecentReplacements.RemoveAt(i);
				}
			}
		}

		internal static float EvaluateSmokeSize(SmokeGrenade grenade, float elapsed)
		{
			if (grenade == null) return 0f;

			float normalTime = grenade.EmitTime > 0f ? elapsed / grenade.EmitTime : 1f;
			SmokeGrenadeSettings settings = grenade.SmokeGrenadeSettings_0;
			if (settings?._sizeOverTime != null && settings._sizeOverTime.length > 0)
			{
				return Mathf.Clamp01(settings._sizeOverTime.Evaluate(normalTime));
			}

			float lifeTime = Mathf.Max(1f, grenade.LifeTime);
			return Mathf.Clamp01(elapsed / Mathf.Min(5f, lifeTime));
		}

		internal static bool TryEnsureRussianSmokeEmission()
		{
			if (_russianSmokeEmissionAvailable) return true;
			if (_nextRussianSmokeProbeTime > Time.realtimeSinceStartup) return false;

			Effects effects = null;
			GrenadeEmission probe = null;
			try
			{
				if (!Singleton<Effects>.Instantiated || Singleton<Effects>.Instance == null)
				{
					return FailRussianSmokeProbe("Effects singleton is not available yet.");
				}

				effects = Singleton<Effects>.Instance;
				string resolvedKey = ResolveRussianSmokeEmissionKey(effects);
				if (string.IsNullOrEmpty(resolvedKey))
				{
					return FailRussianSmokeProbe("No Russian/RDG smoke emission key was found. Available smoke emission keys: " + DescribeSmokeEmissionKeys(effects));
				}

				_russianSmokeEmissionKey = resolvedKey;
				probe = effects.GetEmissionEffect(_russianSmokeEmissionKey);
				if (probe == null)
				{
					return FailRussianSmokeProbe(_russianSmokeEmissionKey + " emission was not found.");
				}

				_russianSmokeEmissionAvailable = true;
				Debug.Log("[BreakContact] Loaded Russian smoke emission '" + _russianSmokeEmissionKey + "' for M18 smoke replacement.");
				return true;
			}
			catch (Exception ex)
			{
				return FailRussianSmokeProbe(ex.Message);
			}
			finally
			{
				if (effects != null && probe != null)
				{
					try
					{
						effects.DisposeEmissionEffect(probe);
					}
					catch (Exception ex)
					{
						Debug.LogWarning("[BreakContact] Failed to return Russian smoke probe to pool: " + ex.Message);
					}
				}
			}
		}

		private static bool FailRussianSmokeProbe(string reason)
		{
			_nextRussianSmokeProbeTime = Time.realtimeSinceStartup + 10f;
			if (!_warnedRussianSmokeUnavailable)
			{
				Debug.LogWarning("[BreakContact] M18 Russian smoke replacement unavailable; vanilla M18 smoke will be used. " + reason);
				_warnedRussianSmokeUnavailable = true;
			}

			return false;
		}

		private static string ResolveRussianSmokeEmissionKey(Effects effects)
		{
			if (HasEmissionKey(effects, RussianSmokeLoopEffectType)) return RussianSmokeLoopEffectType;

			string candidate = FindSmokeEmissionKey(effects, "russian");
			if (!string.IsNullOrEmpty(candidate)) return candidate;

			candidate = FindSmokeEmissionKey(effects, "rdg");
			if (!string.IsNullOrEmpty(candidate)) return candidate;

			return FindSmokeEmissionKey(effects, "rdg2");
		}

		private static bool HasEmissionKey(Effects effects, string key)
		{
			if (effects?.EmissionEffects == null || string.IsNullOrEmpty(key)) return false;

			return effects.EmissionEffects.Any(effect =>
				effect != null && string.Equals(effect.Key, key, StringComparison.OrdinalIgnoreCase));
		}

		private static string FindSmokeEmissionKey(Effects effects, string token)
		{
			if (effects?.EmissionEffects == null || string.IsNullOrEmpty(token)) return null;

			return effects.EmissionEffects
				.Select(effect => effect?.Key)
				.FirstOrDefault(key =>
					!string.IsNullOrEmpty(key)
					&& key.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0
					&& key.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		private static string DescribeSmokeEmissionKeys(Effects effects)
		{
			if (effects?.EmissionEffects == null) return "<none>";

			string[] keys = effects.EmissionEffects
				.Select(effect => effect?.Key)
				.Where(key => !string.IsNullOrEmpty(key) && key.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0)
				.Take(20)
				.ToArray();

			return keys.Length > 0 ? string.Join(", ", keys) : "<none>";
		}
	}

	internal sealed class SmokeOcclusionController : MonoBehaviour
	{
		private sealed class Occluder
		{
			public Transform Transform;
			public CapsuleCollider Collider;
			public Vector2 UnitOffset;
			public float WidthScale;
			public GameObject DebugVisual;
		}

		private const float GoldenAngle = 137.507764f;
		private static bool _warnedMissingFoliageLayer;
		private static Material _debugMaterial;

		private readonly List<Occluder> _occluders = new List<Occluder>();
		private SmokeGrenade _grenade;
		private GameObject _root;
		private float _startTime;
		private float _lifeTime;
		private float _baseRadius;
		private float _radiusMultiplier;
		private float _heightMultiplier;

		public void Init(SmokeGrenade grenade)
		{
			if (_root != null) return;

			int foliageLayer = LayerMask.NameToLayer("Foliage");
			if (foliageLayer < 0)
			{
				if (!_warnedMissingFoliageLayer)
				{
					Debug.LogWarning("[BreakContact] Smoke occluders disabled: Foliage layer was not found.");
					_warnedMissingFoliageLayer = true;
				}

				Destroy(this);
				return;
			}

			_grenade = grenade;
			_startTime = Time.time;
			_lifeTime = Mathf.Max(1f, grenade.LifeTime);
			_baseRadius = SmokeVisualReplacement.GetApproxSmokeRadius(grenade);
			bool isM18 = SmokeVisualReplacement.IsM18SmokeGrenade(grenade);
			_radiusMultiplier = BreakContactConfig.SmokeOccluderRadiusMultiplier.Value
				* (isM18 ? BreakContactConfig.M18SmokeOccluderRadiusMultiplier.Value : 1f);
			_heightMultiplier = isM18 ? 1.35f : 1f;

			_root = new GameObject("BreakContact_SmokeOccluders");
			_root.layer = foliageLayer;
			_root.transform.SetPositionAndRotation(grenade.transform.position, Quaternion.identity);

			int baseCount = Mathf.Clamp(BreakContactConfig.SmokeOccluderDensity.Value, 4, 48);
			int count = Mathf.Clamp(Mathf.RoundToInt(baseCount * (isM18 ? 1.5f : 1f)), 4, 72);
			for (int i = 0; i < count; i++)
			{
				Occluder occluder = CreateOccluder(i, count, foliageLayer);
				_occluders.Add(occluder);
			}
		}

		private Occluder CreateOccluder(int index, int count, int foliageLayer)
		{
			GameObject obj = new GameObject("smoke_ai_foliage_occluder");
			obj.layer = foliageLayer;
			obj.transform.parent = _root.transform;
			obj.transform.localRotation = Quaternion.Euler(0f, index * GoldenAngle, 0f);

			CapsuleCollider collider = obj.AddComponent<CapsuleCollider>();
			collider.direction = 1;
			collider.isTrigger = true;
			collider.enabled = false;

			Vector2 unitOffset;
			if (index == 0)
			{
				unitOffset = Vector2.zero;
			}
			else
			{
				float angle = index * GoldenAngle * Mathf.Deg2Rad;
				float ring = Mathf.Sqrt((index - 0.25f) / Mathf.Max(1f, count - 1f));
				unitOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring;
			}

			return new Occluder
			{
				Transform = obj.transform,
				Collider = collider,
				UnitOffset = unitOffset,
				WidthScale = 0.85f + 0.3f * (((index * 37) % 100) / 100f)
			};
		}

		private void Update()
		{
			if (_root == null)
			{
				Cleanup();
				return;
			}

			float elapsed = Time.time - _startTime;
			if (elapsed >= _lifeTime)
			{
				Cleanup();
				return;
			}

			float size = SmokeVisualReplacement.EvaluateSmokeSize(_grenade, elapsed);
			bool enabledState = size > 0.05f;
			bool showDebugVisuals = BreakContactConfig.SmokeOccluderDebugVisuals.Value;
			float currentRadius = _baseRadius * _radiusMultiplier * size;
			float colliderRadius = Mathf.Clamp(currentRadius / Mathf.Sqrt(Mathf.Max(1, _occluders.Count)) * 0.85f, 0.2f, 2.2f);
			float height = Mathf.Lerp(1.4f, 3.6f, size) * _heightMultiplier;

			_root.transform.position = _grenade.transform.position;
			_root.transform.rotation = Quaternion.identity;

			for (int i = 0; i < _occluders.Count; i++)
			{
				Occluder occluder = _occluders[i];
				if (occluder.Collider == null) continue;

				Vector2 offset = occluder.UnitOffset * currentRadius;
				occluder.Transform.localPosition = new Vector3(offset.x, 0f, offset.y);
				occluder.Collider.enabled = enabledState;
				occluder.Collider.radius = colliderRadius * occluder.WidthScale;
				occluder.Collider.height = Mathf.Max(height, occluder.Collider.radius * 2f);
				occluder.Collider.center = new Vector3(0f, occluder.Collider.height * 0.5f, 0f);
				UpdateDebugVisual(occluder, enabledState && showDebugVisuals);
			}
		}

		private static void UpdateDebugVisual(Occluder occluder, bool visible)
		{
			if (!visible)
			{
				if (occluder.DebugVisual != null)
				{
					occluder.DebugVisual.SetActive(false);
				}

				return;
			}

			if (occluder.DebugVisual == null)
			{
				occluder.DebugVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				occluder.DebugVisual.name = "smoke_ai_foliage_occluder_debug";
				occluder.DebugVisual.transform.parent = occluder.Transform;
				Collider debugCollider = occluder.DebugVisual.GetComponent<Collider>();
				if (debugCollider != null)
				{
					Destroy(debugCollider);
				}

				MeshRenderer renderer = occluder.DebugVisual.GetComponent<MeshRenderer>();
				if (renderer != null)
				{
					renderer.sharedMaterial = DebugMaterial;
				}
			}

			occluder.DebugVisual.SetActive(true);
			occluder.DebugVisual.transform.localPosition = occluder.Collider.center;
			occluder.DebugVisual.transform.localRotation = Quaternion.identity;
			occluder.DebugVisual.transform.localScale = new Vector3(
				occluder.Collider.radius * 2f,
				occluder.Collider.height * 0.5f,
				occluder.Collider.radius * 2f);
		}

		private static Material DebugMaterial
		{
			get
			{
				if (_debugMaterial != null) return _debugMaterial;

				Shader shader = Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
				_debugMaterial = new Material(shader)
				{
					hideFlags = HideFlags.HideAndDontSave,
					color = new Color(0.1f, 1f, 0.2f, 0.22f)
				};
				_debugMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				_debugMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				_debugMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
				_debugMaterial.SetInt("_ZWrite", 0);
				_debugMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
				return _debugMaterial;
			}
		}

		private void Cleanup()
		{
			if (_root != null)
			{
				Destroy(_root);
				_root = null;
			}

			_occluders.Clear();
			Destroy(this);
		}

		private void OnDestroy()
		{
			if (_root != null)
			{
				Destroy(_root);
				_root = null;
			}

			_occluders.Clear();
		}
	}

	internal sealed class M18DelayedSmokeSwapController : MonoBehaviour
	{
		private const float SwapDelaySeconds = 0.5f;

		private EFT.Grenade _grenade;
		private SmokeGrenade _smokeGrenade;
		private float _triggerTime;
		private float _deadline;
		private float _nextAttemptTime;
		private bool _initialized;

		public void Init(EFT.Grenade grenade, SmokeGrenade smokeGrenade, float elapsedSinceThrow, string hook)
		{
			if (grenade == null)
			{
				Destroy(this);
				return;
			}

			_grenade = grenade;
			if (smokeGrenade != null)
			{
				_smokeGrenade = smokeGrenade;
			}

			float elapsed = Mathf.Max(0f, elapsedSinceThrow);
			float delay = Mathf.Max(0f, SwapDelaySeconds - elapsed);
			float requestedTrigger = Time.time + delay;
			if (!_initialized || requestedTrigger < _triggerTime)
			{
				_triggerTime = requestedTrigger;
			}

			float lifeTime = _smokeGrenade != null ? Mathf.Max(1f, _smokeGrenade.LifeTime) : 90f;
			_deadline = Mathf.Max(_deadline, Time.time + lifeTime + 2f);
			if (_initialized) return;

			_initialized = true;
			Debug.Log("[BreakContact] Scheduled M18 delayed Russian smoke swap from " + hook
				+ " in " + delay.ToString("0.00")
				+ "s. plumeScale=" + BreakContactConfig.M18RussianPlumeScale.Value.ToString("0.##")
				+ " buildSpeed=" + BreakContactConfig.M18RussianSmokeBuildSpeed.Value.ToString("0.##") + ".");
		}

		private void Update()
		{
			if (!BreakContactConfig.ReplaceM18SmokeVisual.Value)
			{
				Destroy(this);
				return;
			}

			if (_grenade == null)
			{
				Destroy(this);
				return;
			}

			if (Time.time < _triggerTime || Time.time < _nextAttemptTime) return;

			_nextAttemptTime = Time.time + 0.25f;
			if (TryApplySwap())
			{
				Debug.Log("[BreakContact] Triggered delayed M18 Russian smoke swap.");
				Destroy(this);
				return;
			}

			if (Time.time <= _deadline) return;

			Debug.LogWarning("[BreakContact] M18 delayed Russian smoke swap expired before the replacement could be spawned.");
			Destroy(this);
		}

		private bool TryApplySwap()
		{
			if (_smokeGrenade == null)
			{
				_smokeGrenade = _grenade as SmokeGrenade;
			}

			if (_smokeGrenade != null)
			{
				return SmokeVisualReplacement.TryAttachM18VisualReplacement(_smokeGrenade, SwapDelaySeconds);
			}

			if (!Singleton<Effects>.Instantiated || Singleton<Effects>.Instance == null) return false;

			return SmokeVisualReplacement.TrySpawnM18PointVisualReplacement(
				Singleton<Effects>.Instance,
				_grenade.transform.position,
				SwapDelaySeconds);
		}
	}

	internal sealed class M18VanillaSmokeSuppressor : MonoBehaviour
	{
		private SmokeGrenade _grenade;
		private GrenadeEmission _emission;
		private float _stopTime;

		public void Init(SmokeGrenade grenade)
		{
			_grenade = grenade;
			_stopTime = Time.time + Mathf.Max(1f, grenade.LifeTime);
			SuppressGrenadeVisuals(grenade);
		}

		public void Init(GrenadeEmission emission)
		{
			_emission = emission;
			_stopTime = Time.time + 3f;
			SuppressEmissionVisuals(emission);
		}

		private void LateUpdate()
		{
			if (_grenade != null)
			{
				if (Time.time >= _stopTime)
				{
					Destroy(this);
					return;
				}

				SuppressGrenadeVisuals(_grenade);
				return;
			}

			if (_emission != null)
			{
				SuppressEmissionVisuals(_emission);
			}
		}

		internal static void Suppress(GrenadeEmission emission)
		{
			if (emission == null) return;

			SuppressEmissionVisuals(emission);
			if (emission.GetComponent<M18VanillaSmokeSuppressor>() == null)
			{
				emission.gameObject.AddComponent<M18VanillaSmokeSuppressor>().Init(emission);
			}
		}

		private static void SuppressGrenadeVisuals(SmokeGrenade grenade)
		{
			if (grenade == null) return;

			SmokeGrenadeSettings settings = grenade.SmokeGrenadeSettings_0;
			if (settings?._emissionArea != null)
			{
				SuppressParticleVisuals(settings._emissionArea.gameObject);
			}

			SuppressChildGrenadeEmissions(grenade);
		}

		private static void SuppressChildGrenadeEmissions(SmokeGrenade grenade)
		{
			GrenadeEmission[] emissions = grenade.GetComponentsInChildren<GrenadeEmission>(true);
			for (int i = 0; i < emissions.Length; i++)
			{
				GrenadeEmission emission = emissions[i];
				if (SmokeVisualReplacement.IsRussianSmokeEmission(emission)) continue;

				SuppressEmissionVisuals(emission);
			}
		}

		private static void SuppressEmissionVisuals(GrenadeEmission emission)
		{
			if (emission == null) return;

			SuppressParticleVisuals(emission.gameObject);
		}

		private static void SuppressParticleVisuals(GameObject root)
		{
			if (root == null) return;

			ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
			for (int i = 0; i < systems.Length; i++)
			{
				ParticleSystem system = systems[i];
				ParticleSystem.EmissionModule emission = system.emission;
				emission.enabled = false;
				system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
				system.Clear(true);
			}

			ParticleSystemRenderer[] renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
			for (int i = 0; i < renderers.Length; i++)
			{
				renderers[i].enabled = false;
			}
		}
	}

	internal sealed class M18RussianSmokeController : MonoBehaviour
	{
		private sealed class TunedEmission
		{
			public GrenadeEmission Emission;
			public Vector3 OriginalScale;
			public List<ParticleSystemState> ParticleStates;
			public List<MaterialColorState> MaterialStates;

			public void Restore()
			{
				if (Emission != null)
				{
					Emission.transform.localScale = OriginalScale;
				}

				if (ParticleStates == null) return;

				for (int i = 0; i < ParticleStates.Count; i++)
				{
					ParticleStates[i].Restore();
				}

				if (MaterialStates == null) return;

				for (int i = 0; i < MaterialStates.Count; i++)
				{
					MaterialStates[i].Restore();
				}
			}
		}

		private sealed class ParticleSystemState
		{
			public ParticleSystem System;
			public bool Loop;
			public ParticleSystemScalingMode ScalingMode;
			public ParticleSystem.MinMaxGradient StartColor;
			public float StartSizeMultiplier;
			public float StartSpeedMultiplier;
			public float StartLifetimeMultiplier;
			public int MaxParticles;
			public float RateOverTimeMultiplier;
			public float RateOverDistanceMultiplier;
			public bool ShapeEnabled;
			public float ShapeRadius;

			public static ParticleSystemState Capture(ParticleSystem system)
			{
				ParticleSystem.MainModule main = system.main;
				ParticleSystem.EmissionModule emission = system.emission;
				ParticleSystem.ShapeModule shape = system.shape;
				return new ParticleSystemState
				{
					System = system,
					Loop = main.loop,
					ScalingMode = main.scalingMode,
					StartColor = main.startColor,
					StartSizeMultiplier = main.startSizeMultiplier,
					StartSpeedMultiplier = main.startSpeedMultiplier,
					StartLifetimeMultiplier = main.startLifetimeMultiplier,
					MaxParticles = main.maxParticles,
					RateOverTimeMultiplier = emission.rateOverTimeMultiplier,
					RateOverDistanceMultiplier = emission.rateOverDistanceMultiplier,
					ShapeEnabled = shape.enabled,
					ShapeRadius = shape.radius
				};
			}

			public void Restore()
			{
				if (System == null) return;

				ParticleSystem.MainModule main = System.main;
				main.loop = Loop;
				main.scalingMode = ScalingMode;
				main.startColor = StartColor;
				main.startSizeMultiplier = StartSizeMultiplier;
				main.startSpeedMultiplier = StartSpeedMultiplier;
				main.startLifetimeMultiplier = StartLifetimeMultiplier;
				main.maxParticles = MaxParticles;

				ParticleSystem.EmissionModule emission = System.emission;
				emission.rateOverTimeMultiplier = RateOverTimeMultiplier;
				emission.rateOverDistanceMultiplier = RateOverDistanceMultiplier;

				if (ShapeEnabled)
				{
					ParticleSystem.ShapeModule shape = System.shape;
					shape.radius = ShapeRadius;
				}
			}
		}

		private sealed class MaterialColorState
		{
			public Material Material;
			public string PropertyName;
			public Color Color;

			public void Restore()
			{
				if (Material == null || string.IsNullOrEmpty(PropertyName)) return;
				if (!Material.HasProperty(PropertyName)) return;

				Material.SetColor(PropertyName, Color);
			}
		}

		private const float GoldenAngle = 137.507764f;
		private static bool _warnedInvalidSmokeColor;

		private readonly List<TunedEmission> _emissions = new List<TunedEmission>();
		private SmokeGrenade _grenade;
		private Effects _effects;
		private GameObject _root;
		private float _startTime;
		private float _lifeTime;
		private float _initialPrewarm;
		private float _baseRadius;
		private bool _rootIsHost;
		private bool _cleaningUp;

		internal static bool TrySpawnAt(Effects effects, Vector3 position, float lifeTime, float baseRadius, float initialPrewarm)
		{
			GameObject host = new GameObject("BreakContact_M18RussianSmoke");
			M18RussianSmokeController controller = host.AddComponent<M18RussianSmokeController>();
			controller.Init(effects, position, lifeTime, baseRadius, initialPrewarm);
			return controller.HasSpawned;
		}

		private bool HasSpawned => _emissions.Count > 0;

		public void Init(SmokeGrenade grenade, float initialPrewarm = 0f)
		{
			if (_root != null) return;

			_grenade = grenade;
			InitInternal(
				Singleton<Effects>.Instance,
				grenade.transform.position,
				Mathf.Max(1f, grenade.EmitTime > 0f ? grenade.EmitTime : grenade.LifeTime),
				SmokeVisualReplacement.GetApproxSmokeRadius(grenade),
				initialPrewarm,
				false);
		}

		private void Init(Effects effects, Vector3 position, float lifeTime, float baseRadius, float initialPrewarm)
		{
			if (_root != null) return;

			InitInternal(effects, position, lifeTime, baseRadius, initialPrewarm, true);
		}

		private void InitInternal(
			Effects effects,
			Vector3 position,
			float lifeTime,
			float baseRadius,
			float initialPrewarm,
			bool rootIsHost)
		{
			_effects = effects ?? (Singleton<Effects>.Instantiated ? Singleton<Effects>.Instance : null);
			_startTime = Time.time;
			_lifeTime = Mathf.Max(1f, lifeTime);
			_baseRadius = Mathf.Clamp(baseRadius, 1.5f, 12f);
			_initialPrewarm = Mathf.Max(0f, initialPrewarm);
			_rootIsHost = rootIsHost;

			_root = rootIsHost ? gameObject : new GameObject("BreakContact_M18RussianSmoke");
			_root.transform.SetPositionAndRotation(position, Quaternion.identity);

			SpawnPlumes();
		}

		private void SpawnPlumes()
		{
			if (_effects == null)
			{
				Cleanup();
				return;
			}

			int plumeCount = Mathf.Clamp(BreakContactConfig.M18RussianPlumeCount.Value, 1, 8);
			float plumeScale = Mathf.Clamp(BreakContactConfig.M18RussianPlumeScale.Value, 0.5f, 4f);
			float buildSpeed = Mathf.Clamp(BreakContactConfig.M18RussianSmokeBuildSpeed.Value, 1f, 6f);
			float maxPrewarm = Mathf.Max(0.75f, _lifeTime - 0.25f);
			float prewarm = Mathf.Clamp(Mathf.Max(1.75f * buildSpeed, _initialPrewarm), 0.75f, Mathf.Min(14f, maxPrewarm));
			float offsetRadius = Mathf.Clamp(_baseRadius * 0.2f * plumeScale, 0.45f, 3.5f);
			int spawned = 0;

			for (int i = 0; i < plumeCount; i++)
			{
				Vector3 offset = GetPlumeOffset(i, plumeCount, offsetRadius);
				float variantScale = plumeScale * (0.92f + 0.07f * (i % 3));
				TunedEmission tuned = SpawnRussianEmission(offset, variantScale, prewarm);
				if (tuned == null) continue;

				_emissions.Add(tuned);
				spawned++;
			}

			if (spawned == 0)
			{
				Cleanup();
				return;
			}

			Debug.Log("[BreakContact] Spawned " + spawned + " Russian-style smoke plume(s) for M18 smoke.");
		}

		private TunedEmission SpawnRussianEmission(Vector3 offset, float scale, float prewarm)
		{
			GrenadeEmission emission = null;
			TunedEmission tuned = null;
			try
			{
				emission = _effects.GetEmissionEffect(SmokeVisualReplacement.RussianSmokeEmissionKey);
				if (emission == null) return null;

				List<MaterialColorState> materialStates;
				List<ParticleSystemState> particleStates = ConfigureRuntimeEmission(emission, scale, out materialStates);
				tuned = new TunedEmission
				{
					Emission = emission,
					OriginalScale = emission.transform.localScale,
					ParticleStates = particleStates,
					MaterialStates = materialStates
				};

				emission.transform.localScale = tuned.OriginalScale * scale;
				emission.AttachTo(_root.transform, offset);
				emission.SetFillParams(prewarm, Mathf.Max(1f, _lifeTime - prewarm));
				emission.StartEmission(prewarm);
				emission.Precaution();
				PrewarmAllSystems(emission, prewarm);
				return tuned;
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[BreakContact] Failed to spawn M18 Russian smoke replacement plume: " + ex.Message);
				if (tuned != null)
				{
					Release(tuned);
				}
				else if (emission != null)
				{
					try
					{
						_effects.DisposeEmissionEffect(emission);
					}
					catch
					{
						Destroy(emission.gameObject);
					}
				}

				return null;
			}
		}

		private static List<ParticleSystemState> ConfigureRuntimeEmission(
			GrenadeEmission emission,
			float scale,
			out List<MaterialColorState> materialStates)
		{
			float smokeAmount = Mathf.Clamp(BreakContactConfig.M18RussianSmokeAmount.Value, 0.5f, 8f);
			float buildSpeed = Mathf.Clamp(BreakContactConfig.M18RussianSmokeBuildSpeed.Value, 1f, 6f);
			Color smokeTint = GetConfiguredSmokeTint();
			float particleScale = Mathf.Sqrt(Mathf.Max(0.1f, scale)) * Mathf.Sqrt(smokeAmount);
			float rateScale = Mathf.Max(1f, smokeAmount * buildSpeed * Mathf.Lerp(0.85f, 1.15f, Mathf.Clamp01(scale / 4f)));
			List<ParticleSystemState> states = new List<ParticleSystemState>();
			materialStates = ConfigureMaterialColors(emission, smokeTint);

			ParticleSystem[] systems = emission.GetComponentsInChildren<ParticleSystem>(true);
			for (int i = 0; i < systems.Length; i++)
			{
				ParticleSystem system = systems[i];
				states.Add(ParticleSystemState.Capture(system));

				ParticleSystem.MainModule main = system.main;
				main.loop = true;
				main.scalingMode = ParticleSystemScalingMode.Hierarchy;
				main.startColor = TintMinMaxGradient(main.startColor, smokeTint);
				main.startSizeMultiplier *= particleScale * 1.2f;
				main.startSpeedMultiplier *= Mathf.Lerp(1.15f, particleScale * 1.25f, 0.6f);
				main.startLifetimeMultiplier *= Mathf.Lerp(1f, 1.35f, Mathf.Clamp01(smokeAmount / 4f));
				main.maxParticles = Mathf.CeilToInt(main.maxParticles * Mathf.Max(1f, smokeAmount * scale * 1.25f));

				ParticleSystem.EmissionModule emissionModule = system.emission;
				emissionModule.rateOverTimeMultiplier *= rateScale;
				emissionModule.rateOverDistanceMultiplier *= Mathf.Max(1f, smokeAmount * Mathf.Sqrt(Mathf.Max(0.1f, scale)));

				ParticleSystem.ShapeModule shape = system.shape;
				if (shape.enabled)
				{
					shape.radius *= Mathf.Sqrt(smokeAmount) * Mathf.Sqrt(Mathf.Max(0.1f, scale)) * 1.1f;
				}

				system.Clear(true);
			}

			return states;
		}

		private static Color GetConfiguredSmokeTint()
		{
			string colorHex = BreakContactConfig.M18RussianSmokeColorHex?.Value;
			if (string.IsNullOrEmpty(colorHex)) return Color.white;

			if (!colorHex.StartsWith("#", StringComparison.Ordinal))
			{
				colorHex = "#" + colorHex;
			}

			Color color;
			if (ColorUtility.TryParseHtmlString(colorHex, out color))
			{
				return color;
			}

			if (!_warnedInvalidSmokeColor)
			{
				Debug.LogWarning("[BreakContact] Invalid M18 Russian smoke color hex '" + colorHex + "'; using #FFFFFF.");
				_warnedInvalidSmokeColor = true;
			}

			return Color.white;
		}

		private static List<MaterialColorState> ConfigureMaterialColors(GrenadeEmission emission, Color tint)
		{
			List<MaterialColorState> states = new List<MaterialColorState>();
			HashSet<string> seen = new HashSet<string>();
			Renderer[] renderers = emission.GetComponentsInChildren<Renderer>(true);
			for (int i = 0; i < renderers.Length; i++)
			{
				Material[] materials = renderers[i].materials;
				for (int j = 0; j < materials.Length; j++)
				{
					Material material = materials[j];
					if (material == null) continue;

					TintMaterialProperty(material, "_TintColor", tint, states, seen);
					TintMaterialProperty(material, "_Color", tint, states, seen);
					TintMaterialProperty(material, "_BaseColor", tint, states, seen);
				}
			}

			return states;
		}

		private static void TintMaterialProperty(
			Material material,
			string propertyName,
			Color tint,
			List<MaterialColorState> states,
			HashSet<string> seen)
		{
			if (!material.HasProperty(propertyName)) return;

			string key = material.GetInstanceID() + ":" + propertyName;
			if (!seen.Add(key)) return;

			Color original = material.GetColor(propertyName);
			states.Add(new MaterialColorState
			{
				Material = material,
				PropertyName = propertyName,
				Color = original
			});
			material.SetColor(propertyName, TintColor(original, tint));
		}

		private static ParticleSystem.MinMaxGradient TintMinMaxGradient(ParticleSystem.MinMaxGradient gradient, Color tint)
		{
			switch (gradient.mode)
			{
				case ParticleSystemGradientMode.Color:
					return new ParticleSystem.MinMaxGradient(TintColor(gradient.color, tint));
				case ParticleSystemGradientMode.TwoColors:
					return new ParticleSystem.MinMaxGradient(
						TintColor(gradient.colorMin, tint),
						TintColor(gradient.colorMax, tint));
				case ParticleSystemGradientMode.Gradient:
					return new ParticleSystem.MinMaxGradient(TintGradient(gradient.gradient, tint));
				case ParticleSystemGradientMode.TwoGradients:
					return new ParticleSystem.MinMaxGradient(
						TintGradient(gradient.gradientMin, tint),
						TintGradient(gradient.gradientMax, tint));
				case ParticleSystemGradientMode.RandomColor:
					ParticleSystem.MinMaxGradient randomColor = new ParticleSystem.MinMaxGradient(TintGradient(gradient.gradient, tint));
					randomColor.mode = ParticleSystemGradientMode.RandomColor;
					return randomColor;
				default:
					return new ParticleSystem.MinMaxGradient(TintColor(gradient.color, tint));
			}
		}

		private static Gradient TintGradient(Gradient gradient, Color tint)
		{
			if (gradient == null) return null;

			Gradient copy = new Gradient();
			GradientColorKey[] colorKeys = gradient.colorKeys;
			for (int i = 0; i < colorKeys.Length; i++)
			{
				colorKeys[i].color = TintColor(colorKeys[i].color, tint);
			}

			GradientAlphaKey[] alphaKeys = gradient.alphaKeys;
			for (int i = 0; i < alphaKeys.Length; i++)
			{
				alphaKeys[i].alpha *= tint.a;
			}

			copy.SetKeys(colorKeys, alphaKeys);
			copy.mode = gradient.mode;
			return copy;
		}

		private static Color TintColor(Color color, Color tint)
		{
			return new Color(
				color.r * tint.r,
				color.g * tint.g,
				color.b * tint.b,
				color.a * tint.a);
		}

		private static void PrewarmAllSystems(GrenadeEmission emission, float prewarm)
		{
			if (emission == null || prewarm <= 0f) return;

			ParticleSystem[] systems = emission.GetComponentsInChildren<ParticleSystem>(true);
			for (int i = 0; i < systems.Length; i++)
			{
				ParticleSystem system = systems[i];
				system.Simulate(prewarm, false, true);
				system.Play(false);
			}
		}

		private static Vector3 GetPlumeOffset(int index, int count, float radius)
		{
			if (count <= 1 || index == 0) return Vector3.zero;

			float angle = index * GoldenAngle * Mathf.Deg2Rad;
			float ring = Mathf.Sqrt((index - 0.25f) / Mathf.Max(1f, count - 1f));
			return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius * ring;
		}

		private void Update()
		{
			if (_root == null)
			{
				Cleanup();
				return;
			}

			if (Time.time - _startTime >= _lifeTime)
			{
				Cleanup();
				return;
			}

			if (_grenade != null)
			{
				_root.transform.position = _grenade.transform.position;
				return;
			}

			if (!_rootIsHost)
			{
				Cleanup();
			}
		}

		private void Cleanup()
		{
			if (_cleaningUp) return;
			_cleaningUp = true;

			for (int i = 0; i < _emissions.Count; i++)
			{
				Release(_emissions[i]);
			}

			_emissions.Clear();

			if (_root != null)
			{
				if (_rootIsHost)
				{
					GameObject host = _root;
					_root = null;
					Destroy(host);
					return;
				}

				Destroy(_root);
				_root = null;
			}

			Destroy(this);
		}

		private void Release(TunedEmission tuned)
		{
			if (tuned?.Emission == null) return;

			try
			{
				if (_grenade != null)
				{
					tuned.Emission.StopEmission(_grenade);
				}
			}
			catch
			{
				// The emission is being returned immediately; failed audio fade-out is non-fatal.
			}

			try
			{
				tuned.Restore();
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[BreakContact] Failed to restore pooled Russian smoke particle settings: " + ex.Message);
			}

			try
			{
				if (_effects != null)
				{
					_effects.DisposeEmissionEffect(tuned.Emission);
				}
				else
				{
					Destroy(tuned.Emission.gameObject);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[BreakContact] Failed to return M18 Russian smoke emission to pool: " + ex.Message);
				Destroy(tuned.Emission.gameObject);
			}
		}

		private void OnDestroy()
		{
			if (!_cleaningUp)
			{
				Cleanup();
			}
		}
	}
}

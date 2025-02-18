using EFT;
using RealismMod;
using UnityEngine;

namespace TarkovIRL
{
    internal static class EfficiencyController
    {
        // readonlys
        static readonly float _LerpSpeed = 1f;

        // stacking condiitions
        static readonly int _HeavyBleedHash = 1022907245;
        static readonly int _LightBleedHash = 1022907248;
        static readonly int _FreshWoundHash = -2109260636;
        static readonly int _BoneBreakHash = 619622726;

        // binary conditions
        static readonly int _TremorsHash = 1782422140;
        static readonly int _PainHash = -2109260633;
        static readonly int _FatigueHash = 1782422139;
        static readonly int _ToxicationHash = -1302691615;
        static readonly int _OverdoseHash = 0;

        static readonly int[] _NominalEffectStates = new int[] { 619622721, 1782422141, 619622720 };
        static readonly int[] _KnownEffectStates = new int[] { 1022907245, 1022907248, 2109260636, 619622726, 1782422140, 2109260633, 1782422139, };

        // lerps
        static float _efficiencyLerpTarget = 0;
        static float _efficiencyLerp = 0;
        static float _efficiencyLastFrame = 0;

        static float debugTimer = 0;

        public static void UpdateEfficiencyLerp(float dt)
        {
            float efficiencyLerpMulti = _efficiencyLerpTarget < _efficiencyLastFrame ? 1f / _efficiencyLerpTarget : _efficiencyLerpTarget;
            _efficiencyLerp = Mathf.Lerp(_efficiencyLerp, _efficiencyLerpTarget, dt * _LerpSpeed * PrimeMover.EfficiencyLerpMulti.Value * efficiencyLerpMulti);
            _efficiencyLastFrame = _efficiencyLerpTarget;
            if (TIRLUtils.IsPriority(2)) TIRLUtils.LogError($"_efficiencyLerp {_efficiencyLerp}, _efficiencyLerpTarget {_efficiencyLerpTarget}, efficiencyLerpMulti {efficiencyLerpMulti}");

            debugTimer += dt;
            if (debugTimer > 0.2f)
            {
                if (PrimeMover.DebugEfficiency.Value)
                {
                    TIRLUtils.LogError($"Efficiency value normal (lower is better) {EfficiencyModifier} || Efficiency value inverse (higher is better) : {EfficiencyModifierInverse}");
                }
                debugTimer = 0;
            }
        }

        public static void UpdateEfficiency(Player player)
        {
            //
            // get raw values
            //
            float hydrationNorm = player.HealthController.Hydration.Normalized;
            float nutritionNorm = player.HealthController.Energy.Normalized;
            float overWeightVal = 1f - Mathf.Clamp01(player.Physical.Overweight);
            if (TIRLUtils.IsPriority(2)) TIRLUtils.LogError($"overWeightVal {overWeightVal}");
            float ergoPenalty = player.ErgonomicsPenalty;
            if (TIRLUtils.IsPriority(2)) TIRLUtils.LogError($"ergoPenalty {ergoPenalty}");
            //
            int heavyBleedCount = 0;
            int lightBleedCount = 0;
            int freshWoundCount = 0;
            int boneBreakCount = 0;
            //
            int tremorCount = 0;
            int painCount = 0;
            int fatigueCount = 0;
            int intoxCount = 0;
            int overdoseCount = 0;
            //
            foreach (var effect in player.HealthController.GetAllEffects())
            {
                if (!IsEffectKnown(effect, _NominalEffectStates) && !IsEffectKnown(effect, _KnownEffectStates))
                {
                    if (PrimeMover.DebugSpam.Value) TIRLUtils.LogError($"effect type {effect.Type.FullName}({effect.Type.FullName.GetHashCode()}) on bodypart {effect.BodyPart}");
                }
                if (effect.Type.FullName.GetHashCode() == _HeavyBleedHash) heavyBleedCount++;
                if (effect.Type.FullName.GetHashCode() == _LightBleedHash) lightBleedCount++;
                if (effect.Type.FullName.GetHashCode() == _FreshWoundHash) freshWoundCount++;
                if (effect.Type.FullName.GetHashCode() == _BoneBreakHash) boneBreakCount++;
                //
                if (effect.Type.FullName.GetHashCode() == _TremorsHash) tremorCount++;
                if (effect.Type.FullName.GetHashCode() == _PainHash) painCount++;
                if (effect.Type.FullName.GetHashCode() == _FatigueHash) fatigueCount++;
                //
                if (effect.Type.FullName.GetHashCode() == _ToxicationHash) intoxCount++;
                if (effect.Type.FullName.GetHashCode() == _OverdoseHash) overdoseCount++;
            }
            //
            if (PrimeMover.DebugSpam.Value) TIRLUtils.LogError($"numberOfHeavyBleeds {heavyBleedCount}, numberOfLightBleeds {lightBleedCount}" + 
                $" ");

            float healthCommon = player.HealthController.GetBodyPartHealth(EBodyPart.Common).Normalized;
            float stamNormalized = player.Physical.Stamina.NormalValue;
            float handStamNormalized = player.Physical.HandsStamina.NormalValue;

            //
            // compute outcomes
            //
            float hydroMulti = GetInjuryImpact(hydrationNorm, 5f);
            float nutritionMulti = GetInjuryImpact(nutritionNorm, 5f);
            float overWeightMulti = GetInjuryImpact(overWeightVal, 80f);
            //
            float healthMulti = GetInjuryImpact(healthCommon, 40f);
            float stamMulti = GetInjuryImpact(stamNormalized, 20f);
            float handStamMulti = GetInjuryImpact(handStamNormalized, 20f);
            //
            float heavyBleedsMulti = GetInjuryImpact(10f, heavyBleedCount);
            float boneBreakMulti = GetInjuryImpact(10f, boneBreakCount);
            float lightBleedsMulti = GetInjuryImpact(5f, lightBleedCount);
            float woundMulti = GetInjuryImpact(5f, freshWoundCount);
            //
            float tremorMulti = GetInjuryImpact(5f, tremorCount);
            float painMulti = GetInjuryImpact(5f, painCount);
            float fatigueMulti = GetInjuryImpact(5f, fatigueCount);
            //
            float intoxMulti = GetInjuryImpact(5f, intoxCount);
            //
            float injuryMulti = heavyBleedsMulti * lightBleedsMulti * woundMulti * boneBreakMulti * tremorMulti * painMulti;
            float overdoseMulti = RealismWrapper.IsOverdose ? 1.2f : 1f;

            //
            // negative effects
            //
            float negativeEffects = hydroMulti * nutritionMulti * overWeightMulti * healthMulti * stamMulti * fatigueMulti * handStamMulti * injuryMulti * intoxMulti * overdoseMulti;
            negativeEffects *= PrimeMover.ArtificalInjury.Value;

            // positive effects (incl from Realism)
            float adrenalineBuff = RealismWrapper.IsAdrenaline ? 0.5f : 1f;
            float sidestepDebuff = AnimStateController.IsSideStep ? 1.3f : 1f;
            float highReadyBuff = StanceController.CurrentStance == EStance.HighReady ? 0.85f : 1f;
            float lowReadyDebuff = StanceController.CurrentStance == EStance.LowReady ? 1.1f : 1f;
            float shortStockBuff = StanceController.CurrentStance == EStance.ShortStock ? 0.7f : 1f;
            float activeAimDebuff = StanceController.CurrentStance == EStance.ActiveAiming ? 1.05f : 1f;
            float mountedBuff = StanceController.IsMounting ? 0.5f : 1f;
            float leftShoulderDebuff = StanceController.IsLeftShoulder ? 1.1f : 1f;
            float sprintingmulti = player.IsSprintEnabled ? 1.5f : 1f;
            float speedMulti = 0.5f + (player.Speed * 0.5f);
            if (!PlayerMotionController.IsPlayerMovement)
            {
                speedMulti = 0.5f;
            }
            float poseMulti = 0.5f + (player.PoseLevel * 0.5f);
            float proneMulti = player.IsInPronePose ? 0.5f : 1f;

            // final mobility
            float positiveEffects = speedMulti * poseMulti * proneMulti * sprintingmulti * highReadyBuff * lowReadyDebuff * mountedBuff * shortStockBuff * leftShoulderDebuff * activeAimDebuff * sidestepDebuff * adrenalineBuff;

            // final output
            _efficiencyLerpTarget = negativeEffects * positiveEffects;

            // debug
            if (TIRLUtils.IsPriority(2))
            {
                TIRLUtils.LogError($"hydroMulti {hydroMulti}, nutritionMulti {nutritionMulti}, overWeightMulti {overWeightMulti}, healthMulti {healthMulti}, stamMulti {stamMulti}, handStamMulti {handStamMulti}, injuryMulti {injuryMulti} || _efficiencyLerpTarget {_efficiencyLerpTarget}");
            }
        }

        public static float EfficiencyModifier
        {
            get { return _efficiencyLerp; }
        }

        public static float EfficiencyModifierInverse
        {
            get { return 1f / _efficiencyLerp; }
        }

        static float GetInjuryImpact(float value, float impactWeightPercent)
        {
            float impactWeightAdjusted = impactWeightPercent * 0.01f * PrimeMover.EfficiencyInjuryDebuffMulti.Value;
            return 1f + ((1f - value) * impactWeightAdjusted);
        }

        static float GetInjuryImpact(float impactWeightPercent, int multipleInstances)
        {
            if (multipleInstances == 0)
            {
                return 1f;
            }

            float impactWeightAdjusted = impactWeightPercent * 0.01f * PrimeMover.EfficiencyInjuryDebuffMulti.Value;
            float finalEffectImpact = (1f + impactWeightAdjusted) * multipleInstances;
            return finalEffectImpact;
        }

        static bool IsEffectKnown(IEffect effect, int[] effectsArray)
        {
            foreach (var stateHash in effectsArray)
            {
                if (effect.Type.FullName.GetHashCode() == stateHash)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

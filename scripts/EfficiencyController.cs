using EFT;
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
        static readonly int _ToxicationHash = 0;

        static readonly int[] _NominalEffectStates = new int[] { 619622721, 1782422141, 619622720 };
        static readonly int[] _KnownEffectStates = new int[] { 1022907245, 1022907248, 2109260636, 619622726, 1782422140, 2109260633, 1782422139, };

        // lerps
        static float _efficiencyLerpTarget = 0;
        static float _efficiencyLerp = 0;
        static float _efficiencyLastFrame = 0;

        public static void UpdateEfficiencyLerp(float dt)
        {
            float efficiencyLerpMulti = _efficiencyLerpTarget < _efficiencyLastFrame ? 1f / _efficiencyLerpTarget : _efficiencyLerpTarget;
            _efficiencyLerp = Mathf.Lerp(_efficiencyLerp, _efficiencyLerpTarget, dt * _LerpSpeed * PrimeMover.EfficiencyLerpMulti.Value * efficiencyLerpMulti);
            _efficiencyLastFrame = _efficiencyLerpTarget;
            if (UtilsTIRL.IsPriority(2)) UtilsTIRL.Log($"_efficiencyLerp {_efficiencyLerp}, _efficiencyLerpTarget {_efficiencyLerpTarget}, efficiencyLerpMulti {efficiencyLerpMulti}");
        }

        public static void UpdateEfficiency(Player player)
        {
            //
            // get raw values
            //
            float hydrationNorm = player.HealthController.Hydration.Normalized;
            float nutritionNorm = player.HealthController.Energy.Normalized;
            float overWeightVal = 1f - Mathf.Clamp01(player.Physical.Overweight);
            if (UtilsTIRL.IsPriority(2)) UtilsTIRL.Log($"overWeightVal {overWeightVal}");
            float ergoPenalty = player.ErgonomicsPenalty;
            if (UtilsTIRL.IsPriority(2)) UtilsTIRL.Log($"ergoPenalty {ergoPenalty}");
            //
            float brokenArmR = player.HealthController.IsBodyPartBroken(EBodyPart.RightArm) ? 0 : 1f;
            float brokenArmL = player.HealthController.IsBodyPartBroken(EBodyPart.LeftArm) ? 0 : 1f;
            float brokenLegR = player.HealthController.IsBodyPartBroken(EBodyPart.RightLeg) ? 0 : 1f;
            float brokenLegL = player.HealthController.IsBodyPartBroken(EBodyPart.LeftLeg) ? 0 : 1f;
            //
            int heavyBleedCount = 0;
            int lightBleedCount = 0;
            int freshWoundCount = 0;
            int boneBreakCount = 0;
            //
            int tremorCount = 0;
            int painCount = 0;
            int fatigueCount = 0;
            //
            foreach (var effect in player.HealthController.GetAllEffects())
            {
                if (!IsEffectKnown(effect, _NominalEffectStates) && !IsEffectKnown(effect, _KnownEffectStates))
                {
                    if (UtilsTIRL.IsPriority(2)) UtilsTIRL.Log($"effect type {effect.Type.FullName.GetHashCode()} on bodypart {effect.BodyPart}");
                }
                if (effect.Type.FullName.GetHashCode() == _HeavyBleedHash) heavyBleedCount++;
                if (effect.Type.FullName.GetHashCode() == _LightBleedHash) lightBleedCount++;
                if (effect.Type.FullName.GetHashCode() == _FreshWoundHash) freshWoundCount++;
                if (effect.Type.FullName.GetHashCode() == _BoneBreakHash) boneBreakCount++;
                //
                if (effect.Type.FullName.GetHashCode() == _TremorsHash) tremorCount++;
                if (effect.Type.FullName.GetHashCode() == _PainHash) painCount++;
                if (effect.Type.FullName.GetHashCode() == _FatigueHash) fatigueCount++;
            }
            //
            if (UtilsTIRL.IsPriority(2)) UtilsTIRL.Log($"numberOfHeavyBleeds {heavyBleedCount}, numberOfLightBleeds {lightBleedCount}" + 
                $" ");

            float healthCommon = player.HealthController.GetBodyPartHealth(EBodyPart.Common).Normalized;
            float stamNormalized = player.Physical.Stamina.NormalValue;
            float handStamNormalized = player.Physical.HandsStamina.NormalValue;

            //
            // compute outcomes
            //
            float hydroMulti = GetNormalizedEffectImpact(hydrationNorm, 5f);
            float nutritionMulti = GetNormalizedEffectImpact(nutritionNorm, 5f);
            float overWeightMulti = GetNormalizedEffectImpact(overWeightVal, 50f);
            //
            float healthMulti = GetNormalizedEffectImpact(healthCommon, 50f);
            float stamMulti = GetNormalizedEffectImpact(stamNormalized, 20f);
            float handStamMulti = GetNormalizedEffectImpact(handStamNormalized, 20f);
            //
            float heavyBleedsMulti = GetNormalizedEffectImpact(15f, heavyBleedCount);
            float boneBreakMulti = GetNormalizedEffectImpact(15f, boneBreakCount);
            float lightBleedsMulti = GetNormalizedEffectImpact(5f, lightBleedCount);
            float woundMulti = GetNormalizedEffectImpact(5f, freshWoundCount);
            //
            float tremorMulti = GetNormalizedEffectImpact(5f, tremorCount);
            float painMulti = GetNormalizedEffectImpact(5f, painCount);
            float fatigueMulti = GetNormalizedEffectImpact(5f, fatigueCount);
            //
            float injuryMulti = heavyBleedsMulti * lightBleedsMulti * woundMulti * boneBreakMulti * tremorMulti * painMulti * fatigueMulti;

            //
            // negative effects
            //
            float negativeEffects = hydroMulti * nutritionMulti * overWeightMulti * healthMulti * stamMulti * handStamMulti * injuryMulti;
            negativeEffects *= PrimeMover.EfficiencyNegativeEffectsMulti.Value;

            // reduce negetive effects per speed and pose
            float sprintingmulti = player.IsSprintEnabled ? 1.5f : 1f;
            float speedMulti = 0.5f + (player.Speed * 0.5f);
            if (!PlayerMotionController.IsPlayerMovement)
            {
                speedMulti = 0.5f;
            }
            float poseMulti = 0.5f + (player.PoseLevel * 0.5f);
            float proneMulti = player.IsInPronePose ? 0.5f : 1f;

            // final mobility
            float mobilityMulti = speedMulti * poseMulti * proneMulti * sprintingmulti;

            // final output
            _efficiencyLerpTarget = negativeEffects * mobilityMulti;

            // debug
            if (UtilsTIRL.IsPriority(2))
            {
                UtilsTIRL.Log($"hydroMulti {hydroMulti}, nutritionMulti {nutritionMulti}, overWeightMulti {overWeightMulti}, healthMulti {healthMulti}, stamMulti {stamMulti}, handStamMulti {handStamMulti}, injuryMulti {injuryMulti} || _efficiencyLerpTarget {_efficiencyLerpTarget}");
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

        static float GetNormalizedEffectImpact(float value, float impactWeightPercent)
        {
            float impactWeightAdjusted = impactWeightPercent * 0.01f;
            return 1f + ((1f - value) * impactWeightAdjusted);
        }

        static float GetNormalizedEffectImpact(float impactWeightPercent, int multipleInstances)
        {
            if (multipleInstances == 0)
            {
                return 1f;
            }

            float impactWeightAdjusted = impactWeightPercent * 0.01f;
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

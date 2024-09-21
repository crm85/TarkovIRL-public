using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    internal static class EfficiencyController
    {
        // readonlys
        static readonly float _LerpSpeed = 1f;

        static float _efficiencyLerpTarget = 0;
        static float _efficiencyLerp = 0;

        public static void UpdateEfficiencyLerp(float dt)
        {
            float inverseEfficiency = 1f / _efficiencyLerpTarget;
            _efficiencyLerp = Mathf.Lerp(_efficiencyLerp, _efficiencyLerpTarget, dt * _LerpSpeed * PrimeMover.EfficiencyLerpMulti.Value * inverseEfficiency);
        }

        public static void UpdateEfficiency(Player player)
        {
            //
            // get raw values
            //
            float hydrationNorm = player.HealthController.Hydration.Normalized;
            float nutritionNorm = player.HealthController.Energy.Normalized;
            float overWeightVal = 1f - Mathf.Clamp01(player.Physical.Overweight);
            UtilsTIRL.Log(true, $"overWeightVal {overWeightVal}");
            float ergoPenalty = player.ErgonomicsPenalty;
            UtilsTIRL.Log(true, $"ergoPenalty {ergoPenalty}");
            //
            float brokenArmR = player.HealthController.IsBodyPartBroken(EBodyPart.RightArm) ? 0 : 1f;
            float brokenArmL = player.HealthController.IsBodyPartBroken(EBodyPart.LeftArm) ? 0 : 1f;
            float brokenLegR = player.HealthController.IsBodyPartBroken(EBodyPart.RightLeg) ? 0 : 1f;
            float brokenLegL = player.HealthController.IsBodyPartBroken(EBodyPart.LeftLeg) ? 0 : 1f;
            //
            float healthCommon = player.HealthController.GetBodyPartHealth(EBodyPart.Common).Normalized;
            float stamNormalized = player.Physical.Stamina.NormalValue;
            float handStamNormalized = player.Physical.HandsStamina.NormalValue;

            //
            // compute outcomes
            //
            float hydroMulti = GetNormalizedEffectImpact(hydrationNorm, 5f);
            float nutritionMulti = GetNormalizedEffectImpact(nutritionNorm, 5f);
            float overWeightMulti = GetNormalizedEffectImpact(overWeightVal, 20f);
            float ergoMulti = GetNormalizedEffectImpact(ergoPenalty, 5f);
            //
            float armMultiL = GetNormalizedEffectImpact(brokenArmL, 15f);
            float armMultiR = GetNormalizedEffectImpact(brokenArmR, 15f);
            float legMultiL = GetNormalizedEffectImpact(brokenLegL, 15f);
            float legMultiR = GetNormalizedEffectImpact(brokenLegR, 15f);
            //
            float healthMulti = GetNormalizedEffectImpact(healthCommon, 50f);
            float stamMulti = GetNormalizedEffectImpact(stamNormalized, 10f);
            float handStamMulti = GetNormalizedEffectImpact(handStamNormalized, 10f);

            //
            // negative effects
            //
            float negativeEffects = hydroMulti * nutritionMulti * overWeightMulti * ergoMulti * armMultiL * armMultiR * legMultiL * legMultiR * healthMulti * stamMulti * handStamMulti;

            // reduce negetive effects per speed and pose
            float sprintingmulti = player.IsSprintEnabled ? 1.5f : 1f;
            float speedMulti = 0.5f + (player.Speed * 0.5f);
            float poseMulti = 0.5f + (player.PoseLevel * 0.5f);
            float proneMulti = player.IsInPronePose ? 0.5f : 1f;

            // final mobility
            float mobilityMulti = speedMulti * poseMulti * proneMulti * sprintingmulti;

            // final output
            _efficiencyLerpTarget = negativeEffects * mobilityMulti;

            // debug
            UtilsTIRL.Log(true, $"hydroMulti {hydroMulti}, nutritionMulti {nutritionMulti}, overWeightMulti {overWeightMulti}, ergoMulti {ergoMulti}" +
                $", armMultiL {armMultiL}, armMultiR {armMultiR}, legMultiL {legMultiL}, legMultiR {legMultiR}, healthMulti {healthMulti}, stamMulti {stamMulti}, handStamMulti {handStamMulti}");
            UtilsTIRL.Log(true, $"final efficiency before mobility correction: {negativeEffects}; mobility corrections: {mobilityMulti}; after mobility correction: {_efficiencyLerpTarget}; actual lerp value {_efficiencyLerp}");
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
    }
}

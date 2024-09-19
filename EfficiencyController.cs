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
            // TODO : incorporate player.ErgonomicsPenalty into efficiency
            //

            //
            // TODO : incorporate strength correctly
            //

            //
            // Higher result = LESS efficient
            // divide by 1 to reverse result
            //
            float playerSpeed = 1f + player.Speed;
            float poseLevel = Mathf.Clamp(player.PoseLevel, 0.5f, 1f);

            float healthCommon = player.HealthController.GetBodyPartHealth(EBodyPart.Common).Normalized;
            float armHealthR = player.HealthController.GetBodyPartHealth(EBodyPart.RightArm).Normalized;
            float armHealthL = player.HealthController.GetBodyPartHealth(EBodyPart.LeftArm).Normalized;
            float stamNormalized = player.Physical.Stamina.Current / 104f;
            float handStamNormalized = player.Physical.HandsStamina.Current / 80f;
            //float strength = player.Skills.Strength.Current;
            float strength = PrimeMover.FootstepLerpMulti.Value;
            // i want this to cap out at 350, that removes 25% effect

            float currentWeight = player.Physical.PreviousWeight;

            float healthMulti = 1f + ((1f - healthCommon) * .2f);
            float armHealthRMulti = 1f + ((1f - armHealthR) * .2f);
            float armHealthLMulti = 1f + ((1f - armHealthL) * .2f);
            float stamMulti = 1f + ((1f - stamNormalized) * .1f);
            float handStamMulti = 1f + ((1f - handStamNormalized) * .1f);
            //float underweightReduction = Mathf.Clamp01(currentWeight / (strength * .034f));
            float underweightReduction = 1f;
            //float strengthMulti = 1f - (strength / 15000);
            float strengthMulti = 1f;

            _efficiencyLerpTarget = strengthMulti * underweightReduction * healthMulti * armHealthRMulti * armHealthLMulti * stamMulti * handStamMulti * playerSpeed * poseLevel;
        }

        public static float EfficiencyModifier
        {
            get { return _efficiencyLerp; }
        }

        public static float EfficiencyModifierInverse
        {
            get { return 1f / _efficiencyLerp; }
        }
    }
}

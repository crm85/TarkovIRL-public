using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    internal class HandShakeController
    {
        // readonlys
        static readonly float _HandShakeCurveSpeedMulti = 0.25f;
        static readonly float _HandShakeMultiGeneral = 0.005f;

        // vars
        static float _handShakeLoopTimeX = 0;
        static float _handShakeLoopTimeY = 0;

        public static Vector3 GetHandsShakePosition(Player player)
        {
            if (!player.ProceduralWeaponAnimation.IsAiming)
            {
                return Vector3.zero;
            }

            AnimationCurve shakeCurve = PrimeMover.Instance.HandsShakeCurve;
            float armStamNorm = player.Physical.HandsStamina.Current / 80f;
            float healthCommon = player.HealthController.GetBodyPartHealth(EBodyPart.Common).Normalized;
            float armHealthR = player.HealthController.GetBodyPartHealth(EBodyPart.RightArm).Normalized;
            float armHealthL = player.HealthController.GetBodyPartHealth(EBodyPart.LeftArm).Normalized;
            float strength = player.Skills.Strength.Current;
            float poseLevel = player.PoseLevel;

            float armStamMulti = 1f - armStamNorm;
            float healthMulti = 1f + ((1f - healthCommon) * .2f);
            float armHealthRMulti = 1f + ((1f - armHealthR) * .2f);
            float armHealthLMulti = 1f + ((1f - armHealthL) * .2f);
            //float strengthMulti = 1f - (strength / 15000);
            float strengthMulti = 1f;
            float poseLevelMulti = 1f + poseLevel;

            _handShakeLoopTimeX += player.DeltaTime * _HandShakeCurveSpeedMulti * 0.37f;
            if (_handShakeLoopTimeX >= 1f)
            {
                _handShakeLoopTimeX -= 1f;
            }

            _handShakeLoopTimeY -= player.DeltaTime * _HandShakeCurveSpeedMulti;
            if (_handShakeLoopTimeY <= 0)
            {
                _handShakeLoopTimeY += 1f;
            }

            float finalMulti = armStamMulti * healthMulti * armHealthLMulti * armHealthRMulti * strengthMulti * poseLevelMulti * PrimeMover.ArmShakeMulti.Value * _HandShakeMultiGeneral;

            float handsShakeX = shakeCurve.Evaluate(_handShakeLoopTimeX) * finalMulti;
            float handsShakeY = shakeCurve.Evaluate(_handShakeLoopTimeY) * finalMulti;

            return new Vector3(handsShakeX, handsShakeY, 0);
        }
    }
}

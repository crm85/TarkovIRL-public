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

            float effectSpeedMulti = _HandShakeCurveSpeedMulti * PrimeMover.ArmShakeRateMulti.Value;
            _handShakeLoopTimeX += player.DeltaTime * effectSpeedMulti * 0.37f;
            if (_handShakeLoopTimeX >= 1f)
            {
                _handShakeLoopTimeX -= 1f;
            }

            _handShakeLoopTimeY -= player.DeltaTime * effectSpeedMulti;
            if (_handShakeLoopTimeY <= 0)
            {
                _handShakeLoopTimeY += 1f;
            }

            float pistolFactor = WeaponController.IsPistol ? 2f : 1f;
            float unstockedFactor = !WeaponController.IsPistol && !WeaponController.IsStocked ? 1.8f : 1f;
            float finalMulti = EfficiencyController.EfficiencyModifier * PrimeMover.ArmShakeMulti.Value * _HandShakeMultiGeneral * pistolFactor * unstockedFactor * WeaponController.CurrentWeaponWeight;

            AnimationCurve shakeCurve = PrimeMover.Instance.HandsShakeCurve;
            float handsShakeX = shakeCurve.Evaluate(_handShakeLoopTimeX) * finalMulti;
            float handsShakeY = shakeCurve.Evaluate(_handShakeLoopTimeY) * finalMulti;

            return new Vector3(handsShakeX, handsShakeY, 0);
        }
    }
}

﻿using RealismMod;
using UnityEngine;

namespace TarkovIRL
{
    internal class NewSwayController
    {
        static float _lerpPosHorizontal = 0;
        static float _lerpPosVertical = 0;
        static float _lerpRot = 0;
        static float _weaponTiltLerp = 0;
        static float _leanVerticalLerp = 0;
        static float _vertDropFromRotLerp = 0;
        static float _hyperVerticalLerp = 0;

        static Vector3 _posSmoothed = Vector3.zero;
        static Vector3 _rotSmoothed = Vector3.zero;

        public static void UpdateLerp(float deltaTime)
        {
            bool shoulderContact = WeaponController.HasShoulderContact();
            float value = PlayerMotionController.HorizontalRotationDelta * PrimeMover.WeaponSwayMulti.Value;
            float value2 = PrimeMover.NewSwayRotDeltaClamp.Value;
            value = Mathf.Clamp(value, 0f - value2, value2);
            float num = (shoulderContact ? (-1f) : 0.5f);
            float num2 = ((shoulderContact && PlayerMotionController.IsAiming) ? 0f : 1f);
            float num3 = ((!shoulderContact && !WeaponController.IsPistol && PlayerMotionController.IsAiming) ? 0.7f : 1f);
            float num4 = ((shoulderContact && !PlayerMotionController.IsAiming) ? 0.7f : 1f);
            float num5 = ((!shoulderContact && PlayerMotionController.IsAiming) ? 1.25f : 1f);
            float num6 = ((shoulderContact && PlayerMotionController.IsAiming) ? (-0.25f) : 1f);
            float num7 = ((shoulderContact && PlayerMotionController.IsAiming) ? 2f : 1f);
            float num8 = ((!shoulderContact) ? 0.5f : 1f);
            float weaponMulti = WeaponController.GetWeaponMulti(getInverse: true);
            float efficiencyModifierInverse = EfficiencyController.EfficiencyModifierInverse;
            float num9 = weaponMulti * efficiencyModifierInverse;
            float num10 = ((!shoulderContact) ? (-1f) : 0f);
            float num11 = (PlayerMotionController.IsAiming ? 0.5f : 1f);
            float num12 = (WeaponController.IsPistol ? 2f : 1f);
            float num999 = StanceController.CurrentStance == EStance.LowReady ? 0.25f : 1f;
            float b = value * num * num2 * num3 * WeaponController.GetWeaponMulti(getInverse: false) * EfficiencyController.EfficiencyModifier * num999;
            float b2 = Mathf.Abs(value) * num10 * num11 * num12;

            float swayPosChangeSpeed = PrimeMover.NewSwayPositionDTMulti.Value;
            float swayRotChangeSpeed = PrimeMover.NewSwayRotationDTMulti.Value;

            _lerpPosHorizontal = Mathf.Lerp(_lerpPosHorizontal, b, deltaTime * num9 * num4 * num5 * swayPosChangeSpeed);
            _lerpPosVertical = Mathf.Lerp(_lerpPosVertical, b2, deltaTime * num9 * PrimeMover.NewSwayWpnUnstockedDropSpeed.Value);
            float value3 = value * num6 * WeaponController.GetWeaponMulti(getInverse: false) * EfficiencyController.EfficiencyModifier * num999;
            float num13 = (PlayerMotionController.IsAiming ? (PrimeMover.NewSwayADSRotClamp.Value * value2) : 1f);
            value3 = Mathf.Clamp(value3, 0f - num13, num13);
            _lerpRot = Mathf.Lerp(_lerpRot, value3, deltaTime * num9 * num7 * num8 * swayRotChangeSpeed);
            float value4 = PrimeMover.NewSwayRotFinalClamp.Value;
            _lerpRot = Mathf.Clamp(_lerpRot, 0f - value4, value4);
            float b3 = (PlayerMotionController.IsAiming ? 0f : (PrimeMover.WeaponCantValue.Value * 0.1f));
            _weaponTiltLerp = Mathf.Lerp(_weaponTiltLerp, b3, deltaTime * 20f);
            float num14 = (PlayerMotionController.IsAiming ? 0f : (PlayerMotionController.LeanNormal * PrimeMover.LeanExtraVerticalMulti.Value * WeaponController.GetWeaponMulti(getInverse: false)));
            num14 *= -1f;
            if (AnimStateController.IsLeftShoulder)
            {
                num14 *= -1f;
            }
            _leanVerticalLerp = Mathf.Lerp(_leanVerticalLerp, num14, deltaTime * 10f * EfficiencyController.EfficiencyModifierInverse);
            float num15 = ((WeaponController.IsStocked && PlayerMotionController.IsAiming) ? 0.2f : 1f);
            float b4 = PlayerMotionController.RotationDelta * PrimeMover.NewSwayWpnDropFromRotMulti.Value * WeaponController.GetWeaponMulti(getInverse: false) * EfficiencyController.EfficiencyModifier * num15;
            _vertDropFromRotLerp = Mathf.Lerp(_vertDropFromRotLerp, b4, deltaTime * PrimeMover.NewSwayWpnUnstockedDropSpeed.Value);
            float verticalRotationDelta = PlayerMotionController.VerticalRotationDelta;
            float num16 = ((verticalRotationDelta < 0f) ? (-1f) : 1f);
            float value5 = (PlayerMotionController.IsAiming ? 0f : (verticalRotationDelta * num16 * PrimeMover.HyperVerticalMulti.Value * WeaponController.GetWeaponMulti(getInverse: false) * EfficiencyController.EfficiencyModifier));
            float value6 = PrimeMover.HyperVerticalClamp.Value;
            value5 = Mathf.Clamp(value5, 0f - value6, value6);
            float hyperVertDt = PrimeMover.HyperVerticalDT.Value * RealismWrapper.WeaponBalanceMulti;
            _hyperVerticalLerp = Mathf.Lerp(_hyperVerticalLerp, value5, deltaTime * hyperVertDt);
        }

        public static Vector3 GetNewSwayPosition()
        {
            Vector3 posTarget = new Vector3(_lerpPosHorizontal, _lerpPosVertical, 0);
            _posSmoothed = Vector3.Lerp(_posSmoothed, posTarget, PrimeMover.Instance.DeltaTime * 15f);

            if (!PrimeMover.IsWeaponSway.Value)
            {
                return Vector3.zero;
            }
            if (AnimStateController.IsBlindfire)
            {
                return Vector3.zero;
            }
            Vector3 zero = Vector3.zero;
            zero.x = _posSmoothed.x * PrimeMover.NewSwayPositionMulti.Value * WeaponController.GetWeaponMulti(getInverse: false);
            zero.y = _posSmoothed.y * PrimeMover.NewSwayWpnUnstockedDropValue.Value * WeaponController.GetWeaponMulti(getInverse: false);
            return zero;
        }

        public static Quaternion GetNewSwayRotation()
        {
            Vector3 rotTarget = new Vector3(_lerpPosHorizontal, _lerpPosVertical, 0);
            rotTarget.x = _leanVerticalLerp + _vertDropFromRotLerp + _hyperVerticalLerp;
            rotTarget.y = _weaponTiltLerp;
            rotTarget.z = _lerpRot;
            _rotSmoothed = Vector3.Lerp(_rotSmoothed, rotTarget, PrimeMover.Instance.DeltaTime * 7f);

            if (!PrimeMover.IsWeaponSway.Value)
            {
                return Quaternion.identity;
            }
            if (AnimStateController.IsBlindfire)
            {
                return Quaternion.identity;
            }
            Quaternion identity = Quaternion.identity;
            identity.x = _rotSmoothed.x;
            identity.y = _rotSmoothed.y;
            identity.z = _rotSmoothed.z * PrimeMover.NewSwayRotationMulti.Value * WeaponController.GetWeaponMulti(getInverse: false);
            return identity;
        }
    }
}

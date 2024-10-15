using EFT;
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

        public static void UpdateLerp (float deltaTime)
        {
            if (!PrimeMover.IsWeaponSway.Value)
            {
                return;
            }

            float horizontalDelta = PlayerMotionController.HorizontalRotationDelta * PrimeMover.WeaponSwayMulti.Value;
            float isStockedMulti = WeaponController.IsStocked ? -1 : 0.5f;
            float isAdsPos = WeaponController.IsStocked && PlayerMotionController.IsAiming ? 0 : 1f;
            float isAdsPos2 = !WeaponController.IsStocked && !WeaponController.IsPistol && PlayerMotionController.IsAiming ? 0.7f : 1f;
            float isAdsPosDT = WeaponController.IsStocked && !PlayerMotionController.IsAiming ? 0.7f : 1f;
            float isAdsPosDT2 = !WeaponController.IsStocked && PlayerMotionController.IsAiming ? 1.25f : 1f;
            float isAdsRot = WeaponController.IsStocked && PlayerMotionController.IsAiming ? -0.25f : 1f;
            float isdAdsRotDT = WeaponController.IsStocked && PlayerMotionController.IsAiming ? 2f : 1f;
            float isStockedRotDT = !WeaponController.IsStocked ? 0.5f : 1f;
            float wpnMulti = WeaponController.GetWeaponMulti(true);
            float efficiencyMulti = EfficiencyController.EfficiencyModifierInverse;
            float lerpDTMulti = wpnMulti * efficiencyMulti;
            float posVerticalStockedMulti = !WeaponController.IsStocked ? -1f : 0;
            float posVerticalAimingMulti = PlayerMotionController.IsAiming ? 0.5f : 1f;
            float posVerticalPistolMulti = WeaponController.IsPistol ? 2f : 1f;


            // pos
            float lerpPosHorizontalTarget = horizontalDelta * isStockedMulti * isAdsPos * isAdsPos2 * WeaponController.GetWeaponMulti(false) * EfficiencyController.EfficiencyModifier;
            float lerpPosVerticalTarget = Mathf.Abs(horizontalDelta) * posVerticalStockedMulti * posVerticalAimingMulti * posVerticalPistolMulti;
            
            _lerpPosHorizontal = Mathf.Lerp(_lerpPosHorizontal, lerpPosHorizontalTarget, deltaTime * lerpDTMulti * isAdsPosDT * isAdsPosDT2 * PrimeMover.NewSwayPositionDTMulti.Value);
            _lerpPosVertical = Mathf.Lerp(_lerpPosVertical, lerpPosVerticalTarget, deltaTime * lerpDTMulti * PrimeMover.NewSwayVerticalPosDTMulti.Value);

            // rot
            float lerpRotTarget = horizontalDelta * isAdsRot * WeaponController.GetWeaponMulti(false) * EfficiencyController.EfficiencyModifier;
            _lerpRot = Mathf.Lerp(_lerpRot, lerpRotTarget, deltaTime * lerpDTMulti * isdAdsRotDT * isStockedRotDT * PrimeMover.NewSwayRotationDTMulti.Value);

            // tilt
            float tilteValue = PlayerMotionController.IsAiming ? 0 : PrimeMover.WeaponTiltValue.Value * 0.1f;
            _weaponTiltLerp = Mathf.Lerp(_weaponTiltLerp, tilteValue, deltaTime * 20f);

            // lean vertical
            float leanVerticalTarget = PlayerMotionController.IsAiming ? 0 : PlayerMotionController.LeanNormal * PrimeMover.LeanVerticalMulti.Value * WeaponController.GetWeaponMulti(false);
            leanVerticalTarget *= -1f;
            if (AnimStateController.IsLeftShoulder) leanVerticalTarget *= -1f;
            //if (AnimStateController.IsSideStep) leanVerticalTarget = 1f;
            _leanVerticalLerp = Mathf.Lerp(_leanVerticalLerp, leanVerticalTarget, deltaTime * 10f);

            // vertical drop from rotation
            float verticalDropTarget = PlayerMotionController.RotationDelta * PrimeMover.VerticalDropMulti.Value * WeaponController.GetWeaponMulti(false) * EfficiencyController.EfficiencyModifier;
            _vertDropFromRotLerp = Mathf.Lerp(_vertDropFromRotLerp, verticalDropTarget, deltaTime * PrimeMover.NewSwayVerticalPosDTMulti.Value);

            // hyper-vertical
            float hyperVertTarget = PlayerMotionController.IsAiming ? 0 : PlayerMotionController.VerticalRotationDelta * PrimeMover.HyperVerticalMulti.Value;
            float clamp = PrimeMover.HyperVerticalClamp.Value;
            hyperVertTarget = Mathf.Clamp(hyperVertTarget, -clamp, clamp);
            _hyperVerticalLerp = Mathf.Lerp(_hyperVerticalLerp, hyperVertTarget, deltaTime * PrimeMover.HyperVerticalDT.Value);

            // debug
            //UtilsTIRL.Log($" lerpDTMulti {lerpDTMulti}, efficiencyMulti {efficiencyMulti}, wpnMulti {wpnMulti}");
        }

        public static Vector3 GetNewSwayPosition()
        {
            if (!PrimeMover.IsWeaponSway.Value)
            {
                return Vector3.zero;
            }
            if (AnimStateController.IsBlindfire)
            {
                return Vector3.zero;
            }

            Vector3 addedSway = Vector3.zero;
            addedSway.x = _lerpPosHorizontal * PrimeMover.NewSwayPositionMulti.Value * WeaponController.GetWeaponMulti(false);
            addedSway.y = _lerpPosVertical * PrimeMover.NewSwayVerticalPosMulti.Value * WeaponController.GetWeaponMulti(false);
            return addedSway;
        }

        public static Quaternion GetNewSwayRotation()
        {
            if (!PrimeMover.IsWeaponSway.Value)
            {
                return Quaternion.identity;
            }
            if (AnimStateController.IsBlindfire)
            {
                return Quaternion.identity;
            }

            Quaternion addedRotation = Quaternion.identity;
            addedRotation.x = _leanVerticalLerp + _vertDropFromRotLerp + _hyperVerticalLerp;
            addedRotation.z = _lerpRot * PrimeMover.NewSwayRotationMulti.Value * WeaponController.GetWeaponMulti(false);
            addedRotation.y = _weaponTiltLerp;
            return addedRotation;
        }
    }
}

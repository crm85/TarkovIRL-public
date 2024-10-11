using EFT;
using UnityEngine;

namespace TarkovIRL
{
    internal class NewSwayController
    {
        static readonly float _RotationDTMagicNumber = 5.7f;
        static readonly float _MuteThresh = 0.0132f;

        static float _lerpPos = 0;
        static float _lerpRot = 0;

        public static void UpdateLerp (float deltaTime)
        {
            float delta = PlayerMotionController.HorizontalRotationDelta;
            float isStockedMulti = WeaponController.IsStocked ? -1 : 1f;
            float isAdsPos = WeaponController.IsStocked && PlayerMotionController.IsAiming ? 0 : 1f;
            float isAdsRot = WeaponController.IsStocked && PlayerMotionController.IsAiming ? -0.5f : 1f;
            float wpnMulti = WeaponController.GetWeaponMulti(true);
            float efficiencyMulti = EfficiencyController.EfficiencyModifierInverse;
            float lerpDTMulti = wpnMulti * efficiencyMulti;

            // pos
            float lerpPosTarget = delta * isStockedMulti * isAdsPos;
            _lerpPos = Mathf.Lerp(_lerpPos, lerpPosTarget, deltaTime * lerpDTMulti * PrimeMover.NewSwayPositionDTMulti.Value);

            // rot
            float lerpRotTarget = delta * isAdsRot;
            _lerpRot = Mathf.Lerp(_lerpRot, lerpRotTarget, deltaTime * lerpDTMulti * PrimeMover.NewSwayRotationDTMulti.Value);

            // debug
            //UtilsTIRL.Log($" lerpDTMulti {lerpDTMulti}, efficiencyMulti {efficiencyMulti}, wpnMulti {wpnMulti}");
        }
        public static Vector3 GetNewSwayPosition(Player player)
        {
            Vector3 addedSway = Vector3.zero;
            addedSway.x = _lerpPos * PrimeMover.NewSwayPositionMulti.Value * WeaponController.GetWeaponMulti(false);
            return addedSway;
        }

        public static Quaternion GetNewSwayRotation(Player player)
        {
            Quaternion addedRotation = Quaternion.identity;
            addedRotation.z = _lerpRot * PrimeMover.NewSwayRotationMulti.Value * WeaponController.GetWeaponMulti(false);
            return addedRotation;
        }
    }
}

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
            float isStockedAds = WeaponController.IsStocked && PlayerMotionController.IsAiming ? -1 : 1f;
            float isAds = PlayerMotionController.IsAiming && WeaponController.IsStocked ? 0 : 1f;

            // pos
            float lerpPosDTMulti = WeaponController.GetWeaponMulti(true);
            float lerpPosTarget = delta * isStockedMulti * isAds;
            _lerpPos = Mathf.Lerp(_lerpPos, lerpPosTarget, deltaTime * lerpPosDTMulti * PrimeMover.NewSwayPositionDTMulti.Value);

            // rot
            float lerpRotDTMulti = WeaponController.GetWeaponMulti(true);
            float lerpRotTarget = delta * isStockedAds;
            _lerpRot = Mathf.Lerp(_lerpRot, lerpRotTarget, deltaTime * lerpRotDTMulti * PrimeMover.NewSwayRotationDTMulti.Value);

            // debug
            UtilsTIRL.Log($" deltarot {delta}");
        }
        public static Vector3 GetNewSwayPosition(Player player)
        {
            Vector3 addedSway = Vector3.zero;
            addedSway.x = _lerpPos * PrimeMover.NewSwayPositionMulti.Value * EfficiencyController.EfficiencyModifier;
            return addedSway;
        }

        public static Quaternion GetNewSwayRotation(Player player)
        {
            Quaternion addedRotation = Quaternion.identity;
            addedRotation.z = _lerpRot * PrimeMover.NewSwayRotationMulti.Value * EfficiencyController.EfficiencyModifier;
            return addedRotation;
        }
    }
}

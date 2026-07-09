using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    internal static class DirectionalSwayController
    {
        const float MaxPositionOffset = 0.15f;
        const float MaxRotationComponent = 0.35f;
        const float SnapToZeroEpsilon = 0.000001f;

        static float _lateralPosLerp = 0;
        static float _projectedPosLerp = 0;

        static float _lateralRotLerp = 0;
        static float _verticalRotLerp = 0;

        public static void Reset()
        {
            _lateralPosLerp = 0f;
            _projectedPosLerp = 0f;
            _lateralRotLerp = 0f;
            _verticalRotLerp = 0f;
        }

        public static void UpdateDirectionalSwayLerp(float dt)
        {
            dt = Mathf.Clamp(TIRLUtils.FiniteOr(dt, 0f), 0f, 1f / 15f);

            if (!PrimeMover.IsDirectionalSway.Value)
            {
                Reset();
                return;
            }

            float lateralPosOffset = TIRLUtils.FiniteOr(PrimeMover.DirectionalSwayLateralPosValue.Value, 0f);
            float projectedPosOffset = TIRLUtils.FiniteOr(PrimeMover.DirectionalSwayProjectedPosValue.Value, 0f);
            float lateralRotOffset = TIRLUtils.FiniteOr(PrimeMover.DirectionalSwayLateralRotValue.Value, 0f);
            float verticalRotOffset = TIRLUtils.FiniteOr(PrimeMover.DirectionalSwayVerticalRotValue.Value, 0f);

            PlayerMotionController.EPlayerDir dir = PlayerMotionController.Direction;
            float dirSwayLateralValue = 0;
            float dirSwayVerticalValue = 0;

            if (dir == PlayerMotionController.EPlayerDir.FWD || dir == PlayerMotionController.EPlayerDir.BWD) dirSwayLateralValue = 0;
            else if (dir == PlayerMotionController.EPlayerDir.LEFT) dirSwayLateralValue = -1f;
            else if (dir == PlayerMotionController.EPlayerDir.FWDLEFT) dirSwayLateralValue = -0.5f;
            else if (dir == PlayerMotionController.EPlayerDir.BWDLEFT) dirSwayLateralValue = 0.5f;
            else if (dir == PlayerMotionController.EPlayerDir.RIGHT) dirSwayLateralValue = 1f;
            else if (dir == PlayerMotionController.EPlayerDir.FWDRIGHT) dirSwayLateralValue = 0.5f;
            else if (dir == PlayerMotionController.EPlayerDir.BWDRIGHT) dirSwayLateralValue = -0.5f;
            else if (dir == PlayerMotionController.EPlayerDir.NONE) dirSwayLateralValue = 0f;

            if (dir == PlayerMotionController.EPlayerDir.FWD) dirSwayVerticalValue = 1f;
            else if (dir == PlayerMotionController.EPlayerDir.FWDLEFT) dirSwayVerticalValue = 0.5f;
            else if (dir == PlayerMotionController.EPlayerDir.FWDRIGHT) dirSwayVerticalValue = 0.5f;

            else if (dir == PlayerMotionController.EPlayerDir.BWD) dirSwayVerticalValue = -1f;
            else if (dir == PlayerMotionController.EPlayerDir.BWDLEFT) dirSwayVerticalValue = -0.5f;
            else if (dir == PlayerMotionController.EPlayerDir.BWDRIGHT) dirSwayVerticalValue = -0.5f;

            float normalSpeed = Mathf.Clamp(TIRLUtils.FiniteOr(PlayerMotionController.GetNormalSpeed(), 0f), 0f, 1f);
            float speedAlpha = Mathf.Clamp(normalSpeed, 0.1f, 1f);
            float adsAlpha = PlayerMotionController.IsAiming ? TIRLUtils.FiniteOr(PrimeMover.DirectionalSwayLerpOnAds.Value, 1f) : 1f;
            float weaponMulti = Mathf.Clamp(TIRLUtils.FiniteOr(WeaponController.GetWeaponMulti(false), 1f), 0f, 10f);
            float efficiencyMulti = Mathf.Clamp(TIRLUtils.FiniteOr(EfficiencyController.EfficiencyModifier, 1f), 0f, 5f);
            float directionMulti = speedAlpha * weaponMulti * efficiencyMulti * adsAlpha;

            dirSwayLateralValue *= directionMulti;
            dirSwayVerticalValue *= directionMulti;

            float swayMulti = TIRLUtils.FiniteOr(PrimeMover.DirectionalSwayMulti.Value, 0f);
            float lateralPosTarget = ClampFinite(lateralPosOffset * dirSwayLateralValue * swayMulti, -MaxPositionOffset, MaxPositionOffset);
            float projectedPosTarget = ClampFinite(projectedPosOffset * dirSwayLateralValue * swayMulti, -MaxPositionOffset, MaxPositionOffset);

            float lateralRotTarget = ClampFinite(lateralRotOffset * dirSwayLateralValue * swayMulti, -MaxRotationComponent, MaxRotationComponent);
            float verticalRotTarget = ClampFinite(verticalRotOffset * dirSwayVerticalValue * swayMulti, -MaxRotationComponent, MaxRotationComponent);

            if (!WeaponController.HasCheekWeld())
            {
                lateralPosTarget = 0;
                lateralRotTarget = 0;
                projectedPosTarget = 0;
                verticalRotTarget = 0;
            }

            float speedDTmulti = Mathf.Clamp(normalSpeed, 0.5f, 1f);
            float finalDT = Mathf.Clamp01(dt * TIRLUtils.FiniteOr(PrimeMover.DirectionalSwayLerpSpeed.Value, 1f) * speedDTmulti);

            _lateralPosLerp = Mathf.Lerp(_lateralPosLerp, lateralPosTarget, finalDT);
            _projectedPosLerp = Mathf.Lerp(_projectedPosLerp, projectedPosTarget, finalDT);

            _lateralRotLerp = Mathf.Lerp(_lateralRotLerp, lateralRotTarget, finalDT);
            _verticalRotLerp = Mathf.Lerp(_verticalRotLerp, verticalRotTarget, finalDT);

            SanitizeState();
        }

        public static void GetDirectionalSway(out Vector3 position, out Quaternion rotation)
        {
            if (!PrimeMover.IsDirectionalSway.Value)
            {
                Reset();
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return;
            }

            position = Vector3.zero;
            position.x = _lateralPosLerp;
            position.z = _projectedPosLerp;

            rotation = TIRLUtils.GetQuatFromV3(new Vector3(_verticalRotLerp, 0f, _lateralRotLerp));
        }

        static float ClampFinite(float value, float min, float max)
        {
            if (!TIRLUtils.IsFinite(value))
            {
                return 0f;
            }

            return Mathf.Clamp(value, min, max);
        }

        static void SanitizeState()
        {
            _lateralPosLerp = ClampFinite(_lateralPosLerp, -MaxPositionOffset, MaxPositionOffset);
            _projectedPosLerp = ClampFinite(_projectedPosLerp, -MaxPositionOffset, MaxPositionOffset);
            _lateralRotLerp = ClampFinite(_lateralRotLerp, -MaxRotationComponent, MaxRotationComponent);
            _verticalRotLerp = ClampFinite(_verticalRotLerp, -MaxRotationComponent, MaxRotationComponent);

            if (Mathf.Abs(_lateralPosLerp) < SnapToZeroEpsilon) _lateralPosLerp = 0f;
            if (Mathf.Abs(_projectedPosLerp) < SnapToZeroEpsilon) _projectedPosLerp = 0f;
            if (Mathf.Abs(_lateralRotLerp) < SnapToZeroEpsilon) _lateralRotLerp = 0f;
            if (Mathf.Abs(_verticalRotLerp) < SnapToZeroEpsilon) _verticalRotLerp = 0f;
        }
    }
}

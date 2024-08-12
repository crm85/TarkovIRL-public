using UnityEngine;
using EFT;

namespace TarkovIRL
{
    public static class DeadzoneController
    {
        static readonly float _LerpRate = 10f;
        static readonly int _TurnState1 = -31136456;
        static readonly int _TurnState2 = 287005718;
        static readonly float _RotDeltaThresh = 0.0002f;

        static float _deadZoneLerpTarget = 0;
        static bool _updateDZ = true;

        static float ProcessHeadDelta(float rawHeadDelta)
        {
            float adjustedHeadDelta = rawHeadDelta / WeaponsHandlingController.CurrentWeaponErgo / 10f;
            return adjustedHeadDelta * WeaponsHandlingController.CurrentWeaponWeight * 0.1f;
        }

        public static Vector3 GetHeadRotationWithDeadzone(Player player)
        {
            if (!_updateDZ)
            {
                _updateDZ = PlayerMotionController.IsPlayerMovement || PlayerMotionController.RotationDelta > _RotDeltaThresh;
            }

            Vector3 headRotThisFrame = player.HeadRotation;
            headRotThisFrame.y *= 1.5f;

            bool flag1 = player.MovementContext.CurrentState.AnimatorStateHash == _TurnState1;
            bool flag2 = player.MovementContext.CurrentState.AnimatorStateHash == _TurnState2;
            bool isChangeingStance = flag1 || flag2;

            float headDeltaRaw = player.MovementContext.DeltaRotation;
            float headDeltaTaperMulti = Mathf.Abs(headDeltaRaw / 45f);
            float headDeltaAdjusted = ProcessHeadDelta(headDeltaRaw);

            float finalHeadRotation = headDeltaAdjusted * headDeltaTaperMulti;
            float lerpRate = _LerpRate;

            if (isChangeingStance)
            {
                finalHeadRotation = 0;
                lerpRate *= 0.35f;
            }

            if (player.ProceduralWeaponAnimation.IsAiming)
            {
                _updateDZ = false;
                finalHeadRotation = 0;
                lerpRate = _LerpRate * (1f / (WeaponsHandlingController.CurrentWeaponErgo * WeaponsHandlingController.CurrentWeaponWeight * 2f));
            }

            if (!_updateDZ)
            {
                finalHeadRotation = 0;
            }

            _deadZoneLerpTarget = Mathf.Lerp(_deadZoneLerpTarget, finalHeadRotation, Time.deltaTime * lerpRate);
            headRotThisFrame.y += _deadZoneLerpTarget * PrimeMover.DeadzoneGlobalMultiplier.Value;

            return headRotThisFrame;
        }
    }
}

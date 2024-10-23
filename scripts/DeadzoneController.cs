using UnityEngine;
using EFT;
using EFT.Animations;

namespace TarkovIRL
{
    internal static class DeadzoneController
    {
        static readonly float _LerpRate = 10f;
        static readonly float _RotDeltaThresh = 0.0002f;

        static float _deadZoneLerp = 0;
        static bool _updateDZ = true;

        public static bool DeadzoneUpdatedThisFrame = false;

        static float ProcessHeadDelta(float rawHeadDelta)
        {
            float adjustedHeadDelta = rawHeadDelta / WeaponController.CurrentWeaponErgoNorm / 10f;
            return adjustedHeadDelta * WeaponController.CurrentWeaponWeight * 0.1f;
        }

        public static Vector3 GetHeadRotationWithDeadzone(Player player, float deadzoneSetting, Vector3 headRotInitial)
        {
            if (!_updateDZ)
            {
                _updateDZ = PlayerMotionController.IsPlayerMovement || PlayerMotionController.RotationDelta > _RotDeltaThresh;
            }

            Vector3 headRotThisFrame = headRotInitial;
            headRotThisFrame.y *= 1.5f;
            bool isChangeingStance = AnimStateController.IsTurning;

            float headDeltaRaw = player.MovementContext.DeltaRotation;
            float headDeltaTaperMulti = Mathf.Abs(headDeltaRaw / 45f);
            float headDeltaAdjusted = ProcessHeadDelta(headDeltaRaw);

            float finalHeadRotation = headDeltaAdjusted * headDeltaTaperMulti * deadzoneSetting;
            float lerpRate = _LerpRate;


            if (player.ProceduralWeaponAnimation.IsAiming)
            {
                _updateDZ = false;
                finalHeadRotation = 0;
            }

            if (!_updateDZ)
            {
                finalHeadRotation = 0;
            }

            _deadZoneLerp = Mathf.Lerp(_deadZoneLerp, finalHeadRotation, PrimeMover.Instance.FixedDeltaTime * lerpRate);
            headRotThisFrame.y += _deadZoneLerp;

            if (UtilsTIRL.IsPriority(2)) UtilsTIRL.Log($"initial head rot {headRotInitial}, final output {headRotThisFrame}");

            return headRotThisFrame;
        }
        
    }
}

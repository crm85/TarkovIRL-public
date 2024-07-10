using UnityEngine;
using EFT.InventoryLogic;
using EFT.UI;
using Aki.Reflection.Patching;
using Comfort.Common;

namespace TarkovIRL
{
    public static class WeaponHandlingController
    {
        public static float DeltaTime = 0;
        public static float Time = 0;
        public static float TargetErgo = 0;
        public static float TotalWeaponWeight = 0;
        public static bool SwayUpdatedThisFrame = false;

        static Vector2 _playerRotLastFrame = Vector2.zero;
        static Vector3 _playerPosLastFrame = Vector3.zero;

        static float _playerRotationHistory = 0;
        static float _playerRotationAvg = 0;

        static bool _playerMoving = false;
        static float _verticalAvg = 0;

        static public bool IsReposStance = false;

        static public float ProcessHeadDelta(float rawHeadDelta)
        {
            float adjustedHeadDelta = rawHeadDelta / TargetErgo / 10f;
            return adjustedHeadDelta * WeaponHandlingController.TotalWeaponWeight * 0.1f;
        }

        public static void UpdateRotationHistory(Vector2 newRot)
        {
            // vertical 
            _verticalAvg = newRot.y > _playerRotLastFrame.y ? 1f : -1f;

            // horizontal
            float distance = Vector2.Distance(newRot.normalized, _playerRotLastFrame.normalized);

            _playerRotationHistory += distance;
            _playerRotationHistory -= _playerRotationAvg;
            _playerRotationHistory = Mathf.Clamp(_playerRotationHistory, 0, PrimeMover.RotHistoryPoolClamp.Value);
            _playerRotationAvg = _playerRotationHistory * DeltaTime;

            // set for next frame
            _playerRotLastFrame = newRot;
        }

        public static void UpdateTransformHistory(Vector3 position)
        {
            float dist = Vector3.Distance(position.normalized, _playerPosLastFrame.normalized);
            if (dist > 0)
            {
                _playerMoving = true;
            }
            else
            {
                _playerMoving = false;
            }
            _playerPosLastFrame = position;
        }

        public static float RotationDelta
        {
            get { return _playerRotationAvg; }
        }

        public static bool IsPlayerMovement
        {
            get { return _playerMoving; }
        }

        public static float VerticalTrend
        {
            get { return _verticalAvg; }
        }
    }
}

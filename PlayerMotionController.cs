using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using EFT;
using System.Security.Policy;

namespace TarkovIRL
{
    internal class PlayerMotionController
    {
        // readonlys
        static readonly float _RotationHistoryClamp = 0.015f;

        //vars
        static Vector2 _playerRotLastFrame = Vector2.zero;
        static Vector3 _playerPosLastFrame = Vector3.zero;

        static float _playerRotationHistory = 0;
        static float _playerRotationAvg = 0;

        static float _horizontalRotationHistory = 0;
        static float _horizontalRotationValue = 0;

        static bool _playerMoving = false;
        static float _verticalAvg = 0;
        static float _playerSpeed = 0;

        static public bool IsAiming = false;
        
        public static void UpdateMovement(Player player)
        {
            UpdateMoving(player.Position);
            UpdateRotation(player.Rotation);
            IsAiming = player.ProceduralWeaponAnimation.IsAiming;
            _playerSpeed = player.Speed;
        }
        static void UpdateMoving(Vector3 position)
        {
            float dist = Vector3.Distance(position, _playerPosLastFrame);
            UtilsTIRL.Log($"dist tracked {dist}");
            if (dist > PrimeMover.MotionTrackingThreshold.Value)
            {
                _playerMoving = true;
            }
            else
            {
                _playerMoving = false;
            }
            _playerPosLastFrame = position;
        }

        static void UpdateRotation(Vector2 newRot)
        {
            // vertical 
            _verticalAvg = newRot.y > _playerRotLastFrame.y ? 1f : -1f;

            float distance = Vector2.Distance(newRot, _playerRotLastFrame) * PrimeMover.Instance.FixedDeltaTime;
            float horizontalMovement = newRot.x - _playerRotLastFrame.x;
            horizontalMovement *= 0.01f;
            if (horizontalMovement < -1f)
            {
                horizontalMovement = 0;
            }
            if (horizontalMovement > 1f) 
            {
                horizontalMovement = 0;
            }

            // general rot detection
            _playerRotationHistory += distance;
            _playerRotationHistory -= _playerRotationAvg;
            _playerRotationHistory = Mathf.Clamp(_playerRotationHistory, 0, PrimeMover.RotationHistoryClamp.Value);
            _playerRotationAvg = _playerRotationHistory * PrimeMover.Instance.FixedDeltaTime * PrimeMover.RotationAverageDTMulti.Value;

            // horizontal tracking
            _horizontalRotationHistory += horizontalMovement;
            _horizontalRotationHistory -= _horizontalRotationValue;
            _horizontalRotationValue = _horizontalRotationHistory * PrimeMover.Instance.FixedDeltaTime * PrimeMover.RotationAverageDTMulti.Value;

            // set for next frame
            _playerRotLastFrame = newRot;
        }

        public static float GetNormalSpeed()
        {
            return _playerSpeed / .6f; ;
        }

        public static float VerticalTrend
        {
            get { return _verticalAvg; }
        }

        public static bool IsPlayerMovement
        {
            get { return _playerMoving; }
        }

        public static float RotationDelta
        {
            get { return _playerRotationAvg; }
        }

        public static float HorizontalRotationDelta
        {
            get { return _horizontalRotationValue; }
        }
    }
}

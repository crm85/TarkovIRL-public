using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using EFT;

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

        static public bool IsAiming = false;
        
        public static void UpdateMovement(Player player)
        {
            UpdateMoving(player.Position);
            UpdateRotation(player.Rotation);
            IsAiming = player.ProceduralWeaponAnimation.IsAiming;
        }
        static void UpdateMoving(Vector3 position)
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
        static void UpdateRotation(Vector2 newRot)
        {
            // vertical 
            _verticalAvg = newRot.y > _playerRotLastFrame.y ? 1f : -1f;

            float distance = Vector2.Distance(newRot, _playerRotLastFrame) * PrimeMover.Instance.FixedDeltaTime;
            float horizontalMovement = newRot.x - _playerRotLastFrame.x;
            horizontalMovement *= 0.01f;

            // general rot detection
            _playerRotationHistory += distance;
            _playerRotationHistory -= _playerRotationAvg;
            _playerRotationHistory = Mathf.Clamp(_playerRotationHistory, 0, PrimeMover.DevTestFloat6.Value);
            _playerRotationAvg = _playerRotationHistory * PrimeMover.Instance.FixedDeltaTime * PrimeMover.DevTestFloat3.Value;

            // horizontal tracking
            _horizontalRotationHistory += horizontalMovement;
            _horizontalRotationHistory -= _horizontalRotationValue;
            //_horizontalRotationHistory = Mathf.Clamp(_horizontalRotationHistory, -PrimeMover.DevTestFloat6.Value, PrimeMover.DevTestFloat6.Value);
            _horizontalRotationValue = _horizontalRotationHistory * PrimeMover.Instance.FixedDeltaTime * PrimeMover.DevTestFloat3.Value;

            //UtilsTIRL.Log($"_playerRotationAvg : {_playerRotationAvg} , _horizontalRotationValue : {_horizontalRotationValue}");

            // set for next frame
            _playerRotLastFrame = newRot;
        }

        public static float GetNormalSpeed(Player player)
        {
            return player.Speed / .6f; ;
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

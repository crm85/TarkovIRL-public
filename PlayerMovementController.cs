using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using EFT;

namespace TarkovIRL
{
    internal class PlayerMovementController
    {
        // readonlys
        static readonly float _RotationHistoryClamp = 0.015f;

        //vars
        static Vector2 _playerRotLastFrame = Vector2.zero;
        static Vector3 _playerPosLastFrame = Vector3.zero;

        static float _playerRotationHistory = 0;
        static float _playerRotationAvg = 0;

        static bool _playerMoving = false;
        static float _verticalAvg = 0;
        
        public static void UpdateMovement(Player player)
        {
            UpdateMoving(player.Position);
            UpdateRotation(player.Rotation);
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

            // horizontal
            float distance = Vector2.Distance(newRot.normalized, _playerRotLastFrame.normalized);

            _playerRotationHistory += distance;
            _playerRotationHistory -= _playerRotationAvg;
            _playerRotationHistory = Mathf.Clamp(_playerRotationHistory, 0, _RotationHistoryClamp);
            _playerRotationAvg = _playerRotationHistory * PrimeMover.Instance.DeltaTime;

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
    }
}

using EFT;
using EFT.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    public static class HandMovController
    {
        // readonlys
        static readonly float _RotPullInDeltaThresh = 0.0002f;
        static readonly float _RotPullInValue = 0.05f;
        static readonly float _LerpRate = 4f;
        static readonly float _PullInGateTime = 0.1f;
        static readonly float _StockMovementAddedPosValue = 0.01f;

        // vars
        static float _rotPullInTarget = 0;
        static float _rotPullInLerp = 0;
        static float _pullInGateTimer = 0;
        static bool _pullInGateOpen = false;

        static float _stockedMovementAddedPosTarget = 0;
        static float _stockedMovementAddedPosLerp = 0;

        public static Vector3 GetModifiedHandPosZMovement(Player player)
        {
            float dt = player.DeltaTime;
            _pullInGateTimer += dt;
            if(!player.ProceduralWeaponAnimation.IsAiming)
            {
                if (_pullInGateTimer > _PullInGateTime)
                {
                    _pullInGateOpen = true;
                }
                float rotDelta = WeaponsHandlingController.RotationDelta;
                if (rotDelta > _RotPullInDeltaThresh && _pullInGateOpen)
                {
                    _pullInGateOpen = false;
                    _pullInGateTimer = 0;
                    _rotPullInTarget = _RotPullInValue;
                }
                else if (rotDelta <= _RotPullInDeltaThresh && _pullInGateOpen)
                {
                    _pullInGateOpen = false;
                    _pullInGateTimer = 0;
                    _rotPullInTarget = 0;
                }
            }
            else
            {
                if (WeaponsHandlingController.IsPlayerMovement)
                {
                    _rotPullInTarget = _RotPullInValue;
                }
                else
                {
                    _rotPullInTarget = 0;
                }
            }
            _rotPullInLerp = Mathf.Lerp(_rotPullInLerp, _rotPullInTarget, dt * _LerpRate * 0.6f);
            return new Vector3(0, 0, -_rotPullInLerp);
        }

        public static Vector3 GetModifiedHandPosForUnstockedMovement(Player player)
        {
            float dt = player.DeltaTime;
            if (WeaponsHandlingController.IsStocked && WeaponsHandlingController.IsPlayerMovement)
            {
                _stockedMovementAddedPosTarget = _StockMovementAddedPosValue;
            }
            else
            {
                _stockedMovementAddedPosTarget = 0;
            }
            _stockedMovementAddedPosLerp = Mathf.Lerp(_stockedMovementAddedPosLerp, _stockedMovementAddedPosTarget, dt * _LerpRate * 0.4f);
            return new Vector3(0, -_stockedMovementAddedPosLerp, 0);
        }
    }
}

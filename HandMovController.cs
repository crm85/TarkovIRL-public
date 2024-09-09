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
        static readonly float _LerpRate = 2.2f;
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
                float rotDelta = PlayerMotionController.RotationDelta;
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
                _rotPullInTarget = 0;
            }
            _rotPullInLerp = Mathf.Lerp(_rotPullInLerp, _rotPullInTarget, dt * _LerpRate * 0.6f);
            float finalValue = PrimeMover.Instance.PullInCurve.Evaluate(_rotPullInLerp / _RotPullInValue) * _RotPullInValue;
            return new Vector3(0, 0, -finalValue);
        }

        public static Vector3 GetModifiedHandPosForUnstockedMovement(Player player)
        {
            float dt = player.DeltaTime;
            if (!WeaponController.IsStocked && !WeaponController.IsPistol && PlayerMotionController.IsPlayerMovement)
            {
                _stockedMovementAddedPosTarget = _StockMovementAddedPosValue;
            }
            else
            {
                _stockedMovementAddedPosTarget = 0;
            }
            _stockedMovementAddedPosLerp = Mathf.Lerp(_stockedMovementAddedPosLerp, _stockedMovementAddedPosTarget, dt * _LerpRate);
            return new Vector3(0, -_stockedMovementAddedPosLerp, 0);
        }
    }
}

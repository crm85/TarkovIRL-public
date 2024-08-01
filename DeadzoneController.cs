using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using EFT;

namespace TarkovIRL
{
    internal class DeadzoneController
    {
        //readonlys
        static readonly float _LerpRate = 10f;
        static readonly int _TurnState1 = -31136456;
        static readonly int _TurnState2 = 287005718;
        static readonly float _RotDeltaThresh = 0.0002f;

        //vars
        static float _deadZoneLerpTarget = 0;
        static bool _updateDZ = true;

        public static float ProcessHeadDelta(float rawHeadDelta)
        {
            float adjustedHeadDelta = rawHeadDelta / WeaponsHandlingController.CurrentWeaponErgo / 10f * WeaponsHandlingController.CurrentWeaponWeight * 0.1f;
            return adjustedHeadDelta;
        }

        public static Vector3 ProcessHeadDelta(Player player)
        {
            if (!PrimeMover.IsWeaponDeadzone.Value)
            {
                return Vector3.zero;
            }

            if (!_updateDZ)
            {
                _updateDZ = PlayerMovementController.IsPlayerMovement || PlayerMovementController.RotationDelta > _RotDeltaThresh;
            }

            Vector3 headRotThisFrame = player.HeadRotation;
            headRotThisFrame.y *= 1.5f;

            bool flag1 = player.MovementContext.CurrentState.AnimatorStateHash == _TurnState1;
            bool flag2 = player.MovementContext.CurrentState.AnimatorStateHash == _TurnState2;
            bool isChangeingStance = flag1 || flag2;

            float headDeltaRaw = player.MovementContext.DeltaRotation;
            float headDeltaTaperMulti = Mathf.Abs(headDeltaRaw / 45f);
            float headDeltaAdjusted = headDeltaRaw / WeaponsHandlingController.CurrentWeaponErgo / 10f * WeaponsHandlingController.CurrentWeaponWeight * 0.1f;

            float finalValue = headDeltaAdjusted * headDeltaTaperMulti;
            float lerpRate = _LerpRate;

            if (isChangeingStance)
            {
                finalValue = 0;
                lerpRate *= 0.35f;
            }

            if (player.ProceduralWeaponAnimation.IsAiming)
            {
                _updateDZ = false;
                finalValue = 0;
                lerpRate = _LerpRate * (1f / (WeaponsHandlingController.CurrentWeaponErgo * WeaponsHandlingController.CurrentWeaponWeight * 2f));
            }

            if (!_updateDZ)
            {
                finalValue = 0;
            }

            _deadZoneLerpTarget = Mathf.Lerp(_deadZoneLerpTarget, finalValue, Time.deltaTime * lerpRate);
            headRotThisFrame.y += _deadZoneLerpTarget * PrimeMover.DeadzoneGlobalMultiplier.Value;
            return headRotThisFrame;
        }
    }
}
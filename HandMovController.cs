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
        static float _lerpRate = 4f;
        static float _currentLerpRate = 0;

        static float poseVerticalPoseOffsetModifier = .03f;
        static float poseProjectedPoseOffsetModifier = -0.05f;

        static float _poseVerticalPoseOffsetTarget = 0;
        static float _poseProjectedPoseOffsetTarget = 0;

        static float _poseVerticalPoseOffsetLerp = 0;
        static float _poseProjectedPoseOffsetLerp = 0;

        static float _poseLevelLastFrame = 0;

        static Vector3 poseShiftVector = Vector3.zero;
        static bool _isChangingPose = false;
        static float _changingPoseCycle = 0;
        static float _vertDiff = 0;
        static float _changePoseModifier = 0.01f;

        static float _rotPullInTarget = 0;
        static float _rotPullInLerp = 0;
        static readonly float RotPullInDeltaThresh = 0.0002f;
        static readonly float RotPullInValue = 0.05f;
        static bool _pullInGateOpen = false;
        static float _pullInGateTime = 0.1f;
        static float _pullInGateTimer = 0;

        static float _stockedMovementAddedPosTarget = 0;
        static float _stockedMovementAddedPosLerp = 0;
        static readonly float StockMovementAddedPosValue = 0.01f;

        public static void UpdateLerp(float dt)
        {
            VerticalPoseUpdate(dt);
            ChangePoseUpdate(dt);
            StockedMovUpdate(dt);
        }

        public static Vector3 GetModifiedHandPosWithPose(Player player)
        {
            _currentLerpRate = _lerpRate;

            float poseLevel = player.PoseLevel;
            _poseVerticalPoseOffsetTarget = (1f - poseLevel) * poseVerticalPoseOffsetModifier;
            _poseProjectedPoseOffsetTarget = (1f - poseLevel) * poseProjectedPoseOffsetModifier;

            if (player.ProceduralWeaponAnimation.IsAiming)
            {
                _poseVerticalPoseOffsetTarget = 0;
                _poseProjectedPoseOffsetTarget = 0;

                _currentLerpRate = _lerpRate * 2f;
            }

            return new Vector3(0, _poseVerticalPoseOffsetLerp, _poseProjectedPoseOffsetLerp);
        }

        public static Vector3 GetModifiedHandPosWithPoseChange(Player player)
        {
            Vector3 addedVert = poseShiftVector;
            float poseLevelThisFrame = player.PoseLevel;
            if (poseLevelThisFrame != _poseLevelLastFrame)
            {
                _vertDiff = poseLevelThisFrame - _poseLevelLastFrame;
                if (!_isChangingPose)
                {
                    _isChangingPose = true;
                }
            }

            if (!player.ProceduralWeaponAnimation.IsAiming)
            {
                addedVert.y *= 3f;
            }
            
            _poseLevelLastFrame = poseLevelThisFrame;
            return addedVert;
        }

        static void ChangePoseUpdate(float dt)
        {
            if (_isChangingPose)
            {
                float vertDiffAbs = Mathf.Abs(_vertDiff);
                float speedMod = 1f + (1f - vertDiffAbs);
                float directionSpeedMod = _vertDiff < 0 ? 1.1f : 0.75f;
                _changingPoseCycle += dt * speedMod * directionSpeedMod;
                if (_changingPoseCycle >= 1f)
                {
                    _isChangingPose = false;
                    _changingPoseCycle = 0;
                    return;
                }
                float directionAttenuationMod = _vertDiff < 0 ? 1f : 0.5f;
                float addedVert = PrimeMover.Instance.PoseChangeCurve.Evaluate(_changingPoseCycle) * vertDiffAbs * _changePoseModifier * directionAttenuationMod;
                poseShiftVector = new Vector3(0, addedVert, 0);
            }
            else
            {
                poseShiftVector = Vector3.zero;
            }
        }

        public static Vector3 GetModifiedHandPosForRotSpeed(Player player)
        {
            float dt = player.DeltaTime;
            _pullInGateTimer += dt;
            if(!player.ProceduralWeaponAnimation.IsAiming)
            {
                if (_pullInGateTimer > _pullInGateTime)
                {
                    _pullInGateOpen = true;
                }
                float rotDelta = WeaponsHandlingController.RotationDelta;
                if (rotDelta > RotPullInDeltaThresh && _pullInGateOpen)
                {
                    _pullInGateOpen = false;
                    _pullInGateTimer = 0;
                    _rotPullInTarget = RotPullInValue;
                }
                else if (rotDelta <= RotPullInDeltaThresh && _pullInGateOpen)
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
                    _rotPullInTarget = RotPullInValue;
                }
                else
                {
                    _rotPullInTarget = 0;
                }
            }
            _rotPullInLerp = Mathf.Lerp(_rotPullInLerp, _rotPullInTarget, dt * _lerpRate * 0.6f);
            return new Vector3(0, 0, -_rotPullInLerp);
        }

        static void VerticalPoseUpdate(float dt)
        {
            _poseVerticalPoseOffsetLerp = Mathf.Lerp(_poseVerticalPoseOffsetLerp, _poseVerticalPoseOffsetTarget, dt * _currentLerpRate);
            _poseProjectedPoseOffsetLerp = Mathf.Lerp(_poseProjectedPoseOffsetLerp, _poseProjectedPoseOffsetTarget, dt * _currentLerpRate);
        }

        static void StockedMovUpdate(float dt)
        {
            if (WeaponsHandlingController.IsStocked && WeaponsHandlingController.IsPlayerMovement)
            {
                _stockedMovementAddedPosTarget = StockMovementAddedPosValue;
            }
            else
            {
                _stockedMovementAddedPosTarget = 0;
            }

            _stockedMovementAddedPosLerp = Mathf.Lerp(_stockedMovementAddedPosLerp, _stockedMovementAddedPosTarget, dt * _lerpRate * .4f);
        }

        public static Vector3 GetModifiedHandPosForUnstockedMovement()
        {
            return new Vector3(0, -_stockedMovementAddedPosLerp, 0);
        }
    }
}

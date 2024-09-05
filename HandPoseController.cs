using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    internal class HandPoseController
    {
        // readonlys
        static readonly float _LerpRate = 4f;
        static readonly float _OffsetTargetMultiY = .03f;
        static readonly float _OffsetTargetMultiZ = -0.05f;
        static readonly float _ChangePoseModifier = 0.01f;

        // vars
        static float _currentLerpRate = 0;
        static float _offsetTargetY = 0;
        static float _offsetTargetZ = 0;
        static float _offsetTargetYLerp = 0;
        static float _offsetTargetZLerp = 0;

        static Vector3 _poseShiftVector = Vector3.zero;
        static float _poseLevelLastFrame = 0;
        static float _poseDifference = 0;
        static float _changingPoseCycle = 0;
        static bool _isChangingPose = false;

        static void VerticalPoseUpdate(float dt)
        {
            _offsetTargetYLerp = Mathf.Lerp(_offsetTargetYLerp, _offsetTargetY, dt * _currentLerpRate);
            _offsetTargetZLerp = Mathf.Lerp(_offsetTargetZLerp, _offsetTargetZ, dt * _currentLerpRate);
        }
        static void ChangePoseUpdate(float dt)
        {
            if (_isChangingPose)
            {
                float vertDiffAbs = Mathf.Abs(_poseDifference);
                float speedMod = 1f + (1f - vertDiffAbs);
                float directionSpeedMod = _poseDifference < 0 ? 1.1f : 0.75f;
                _changingPoseCycle += dt * speedMod * directionSpeedMod;
                if (_changingPoseCycle >= 1f)
                {
                    _isChangingPose = false;
                    _changingPoseCycle = 0;
                    return;
                }
                float directionAttenuationMod = _poseDifference < 0 ? 1f : 0.5f;
                float addedVert = PrimeMover.Instance.PoseChangeCurve.Evaluate(_changingPoseCycle) * vertDiffAbs * _ChangePoseModifier * directionAttenuationMod;
                _poseShiftVector = new Vector3(0, addedVert, 0);
            }
            else
            {
                _poseShiftVector = Vector3.zero;
            }
        }

        public static Vector3 GetModifiedHandPosWithPose(Player player)
        {
            VerticalPoseUpdate(player.DeltaTime);

            _currentLerpRate = _LerpRate;
            float poseLevel = player.PoseLevel;

            _offsetTargetY = (1f - poseLevel) * _OffsetTargetMultiY;
            _offsetTargetZ = (1f - poseLevel) * _OffsetTargetMultiZ;

            if (player.ProceduralWeaponAnimation.IsAiming)
            {
                _offsetTargetY = 0;
                _offsetTargetZ = 0;

                _currentLerpRate = _LerpRate * 2f;
            }

            return new Vector3(0, _offsetTargetYLerp, _offsetTargetZLerp);
        }

        public static Vector3 GetModifiedHandPosWithPoseChange(Player player)
        {
            ChangePoseUpdate(player.DeltaTime);

            Vector3 addedVert = _poseShiftVector;
            float poseLevelThisFrame = player.PoseLevel;
            if (poseLevelThisFrame != _poseLevelLastFrame)
            {
                _poseDifference = poseLevelThisFrame - _poseLevelLastFrame;
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
    }
}

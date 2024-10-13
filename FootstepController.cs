using EFT;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    static internal class FootstepController
    {
        // readonlys
        static readonly float _UpdateMulti = 12f;
        static readonly float _StepIntensityMulti = 0.0025f;

        // vars
        static float _stepLerp = 0;
        static float _currentSpeed = 0;

        static float _sideToSideRotationLerp = 0;
        static float _sideToSidePositionLerp = 0;
        static float _sideToSideRotationSmoothingLerp = 0;
        static float _sideToSidePositionSmoothingLerp = 0;

        static bool _currentStepLeft = false;
        static bool _movementLastFrame = false;

        public static void UpdateStep(float dt)
        {
            float speedAdjusted = 1f + _currentSpeed;
            _stepLerp = Mathf.Lerp(_stepLerp, 1f, dt * _UpdateMulti * speedAdjusted * PrimeMover.FootstepLerpMulti.Value);
            _sideToSideRotationLerp += dt * PrimeMover.SideToSideRotationDTMulti.Value * speedAdjusted;
            _sideToSidePositionLerp += dt * PrimeMover.SideToSidePositionDTMulti.Value * speedAdjusted;

            float sideToSideAddedRotationValue = PrimeMover.Instance.SideToSideCurve.Evaluate(_sideToSideRotationLerp) * PrimeMover.SideToSideSwayMulti.Value;
            sideToSideAddedRotationValue *= _currentStepLeft ? 1f : -1f;
            float sideToSideAddedPositionValue = PrimeMover.Instance.FootStepCurve.Evaluate(_sideToSidePositionLerp) * PrimeMover.SideToSideSwayMulti.Value;
            sideToSideAddedPositionValue *= _currentStepLeft ? -1f : 1f;

            _sideToSideRotationSmoothingLerp = Mathf.Lerp(_sideToSideRotationSmoothingLerp, sideToSideAddedRotationValue, dt * 7f);
            _sideToSidePositionSmoothingLerp = Mathf.Lerp(_sideToSidePositionSmoothingLerp, sideToSideAddedPositionValue, dt * 7f);
        }

        public static void NewStep(Player player)
        {
            _stepLerp = 0;
            _currentSpeed = player.Speed;
            _sideToSideRotationLerp = 0;
            _sideToSidePositionLerp = 0;

            if (!PlayerMotionController.IsPlayerMovement && !_movementLastFrame)
            {
                _currentStepLeft = true;
            }
            else
            {
                _currentStepLeft = !_currentStepLeft;
            }
            _movementLastFrame = PlayerMotionController.IsPlayerMovement;
        }

        public static Vector3 GetModifiedHandPosFootstep
        {
            get 
            {
                float speedAdjusted = 0.2f + _currentSpeed;
                float stepValue = PrimeMover.Instance.FootStepCurve.Evaluate(_stepLerp) * _StepIntensityMulti * PrimeMover.FootstepIntesnityMulti.Value * (EfficiencyController.EfficiencyModifier * 0.5f) * speedAdjusted;
                return new Vector3(0, stepValue, 0); 
            }
        }

        public static Quaternion GetSideToSideRotation()
        {
            Quaternion addedRotation = Quaternion.identity;
            addedRotation.z = _sideToSideRotationSmoothingLerp * PlayerMotionController.GetNormalSpeed();
            addedRotation.z *= PlayerMotionController.IsAiming ? 0.4f : 1f;
            //addedRotation.y = PrimeMover.WeaponTiltValue.Value * _sideToSideRotationSmoothingLerp;
            // ^^ needs some elaboration

            return addedRotation;
        }

        public static Vector3 GetSideToSidePosition()
        {
            Vector3 addedPosition = Vector3.zero;
            addedPosition.x = _sideToSidePositionSmoothingLerp * PlayerMotionController.GetNormalSpeed();
            addedPosition.x *= PlayerMotionController.IsAiming ? 0.4f : 1f;
            return addedPosition;
        }
    }
}

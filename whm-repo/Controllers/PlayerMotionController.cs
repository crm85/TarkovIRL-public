using System;
using UnityEngine;
using EFT;

namespace TarkovIRL
{
    internal static class PlayerMotionController
    {
        public enum EPlayerDir { FWD, BWD, LEFT, RIGHT, FWDLEFT, FWDRIGHT, BWDLEFT, BWDRIGHT, NONE };
        static EPlayerDir _dir;

        //vars
        static Vector2 _playerRotLastFrame = Vector2.zero;
        static Vector3 _playerPosLastFrame = Vector3.zero;

        static float _playerRotationHistory = 0;
        static float _playerRotationAvg = 0;

        static float _horizontalRotationHistory = 0;
        static float _horizontalRotationValue = 0;

        static float _verticalRotationHistory = 0;
        static float _verticalRotationValue = 0;

        static bool _playerMoving = false;
        static bool _inputMoving = false;
        static bool _positionMoving = false;
        static bool _hasPositionSample = false;
        static float _verticalAvg = 0;
        static float _playerSpeed = 0;
        static float _leanNormalized = 0;
        static float _armStam = 0;

        public static bool IsAiming = false;
        public static bool IsSprinting = false;
        public static bool IsProne = false;
        public static bool IsHoldingBreath = false;
        public static bool IsAugmentedBreath = false;

        static Vector3 _lastPositionRecorded = Vector3.zero;
        static Vector2 _lastRotationRecorded = Vector2.zero;
        static Vector3 _lastInputDirection = Vector3.zero;

        public static void UpdateMovementMeasurementsInFDT(float fdt)
        {
            UpdateIsMovingBool(_lastPositionRecorded);
            UpdateRotationEngine(_lastRotationRecorded, fdt);
        }

        public static void UpdateMovementInformation(Player player)
        {
            _lastPositionRecorded = TIRLUtils.FiniteOr(player.Position, _lastPositionRecorded);
            _lastRotationRecorded = TIRLUtils.FiniteOr(player.Rotation, _lastRotationRecorded);
            IsAiming = player.ProceduralWeaponAnimation.IsAiming;
            IsProne = player.IsInPronePose;
            IsSprinting = player.IsSprintEnabled;
            _playerSpeed = Mathf.Max(0f, TIRLUtils.FiniteOr(player.Speed, 0f));
            _leanNormalized = TIRLUtils.FiniteOr(player.MovementContext.Tilt / 5f, 0f);
            _armStam = Mathf.Clamp01(TIRLUtils.FiniteOr(player.Physical.HandsStamina.NormalValue, 0f));
            Vector3 inputDirection = player.InputDirection;
            _lastInputDirection = TIRLUtils.FiniteOr(inputDirection, Vector3.zero);
            _inputMoving = _lastInputDirection.sqrMagnitude > 0.0001f || _playerSpeed > PrimeMover.MotionTrackingThreshold.Value;
            _playerMoving = _inputMoving || _positionMoving;
            UpdateMovementDirection(_lastInputDirection);
            //TIRLUtils.Log($"player input: {player.InputDirection}", true);
        }

        public static void Reset(Player player = null)
        {
            _playerRotLastFrame = player != null ? TIRLUtils.FiniteOr(player.Rotation, Vector2.zero) : Vector2.zero;
            _playerPosLastFrame = player != null ? TIRLUtils.FiniteOr(player.Position, Vector3.zero) : Vector3.zero;
            _lastRotationRecorded = _playerRotLastFrame;
            _lastPositionRecorded = _playerPosLastFrame;

            _playerRotationHistory = 0f;
            _playerRotationAvg = 0f;
            _horizontalRotationHistory = 0f;
            _horizontalRotationValue = 0f;
            _verticalRotationHistory = 0f;
            _verticalRotationValue = 0f;
            _verticalAvg = 0f;
            _playerSpeed = 0f;
            _leanNormalized = 0f;
            _armStam = 0f;
            _playerMoving = false;
            _inputMoving = false;
            _positionMoving = false;
            _hasPositionSample = player != null;
            _lastInputDirection = Vector3.zero;
            _dir = EPlayerDir.NONE;
            IsAiming = false;
            IsSprinting = false;
            IsProne = false;
            IsHoldingBreath = false;
            IsAugmentedBreath = false;
        }
        static void UpdateIsMovingBool(Vector3 position)
        {
            position = TIRLUtils.FiniteOr(position, _playerPosLastFrame);

            if (!_hasPositionSample)
            {
                _playerPosLastFrame = position;
                _positionMoving = false;
                _hasPositionSample = true;
                _playerMoving = _inputMoving;
                return;
            }

            float threshold = Mathf.Max(0f, TIRLUtils.FiniteOr(PrimeMover.MotionTrackingThreshold.Value, 0f));
            float distSqr = (position - _playerPosLastFrame).sqrMagnitude;
            _positionMoving = TIRLUtils.IsFinite(distSqr) && distSqr > threshold * threshold;
            _playerMoving = _inputMoving || _positionMoving;
            _playerPosLastFrame = position;
        }

        static void UpdateRotationEngine(Vector2 newRot, float dt)
        {
            //
            // One could say this function is the heart of the mod, if there is one
            //

            dt = Mathf.Clamp(TIRLUtils.FiniteOr(dt, 0f), 0f, 1f / 15f);
            newRot = TIRLUtils.FiniteOr(newRot, _playerRotLastFrame);

            // vertical
            float horizontalMovementRaw = Mathf.DeltaAngle(_playerRotLastFrame.x, newRot.x);
            float verticalMovementRaw = newRot.y - _playerRotLastFrame.y;

            if (!TIRLUtils.IsFinite(horizontalMovementRaw) || Mathf.Abs(horizontalMovementRaw) > 90f)
            {
                horizontalMovementRaw = 0f;
            }
            if (!TIRLUtils.IsFinite(verticalMovementRaw) || Mathf.Abs(verticalMovementRaw) > 90f)
            {
                verticalMovementRaw = 0f;
            }

            _verticalAvg = verticalMovementRaw >= 0f ? 1f : -1f;

            float distance = new Vector2(horizontalMovementRaw, verticalMovementRaw).magnitude * dt;
            float horizontalMovement = horizontalMovementRaw;
            float verticalMovement = verticalMovementRaw;

            horizontalMovement *= dt;
            verticalMovement *= dt;

            if (Mathf.Abs(horizontalMovement) > 1f)
            {
                horizontalMovement = 0f;
            }
            if (Mathf.Abs(verticalMovement) > 1f)
            {
                verticalMovement = 0f;
            }

            float historyClamp = Mathf.Max(0.001f, TIRLUtils.FiniteOr(PrimeMover.RotationHistoryClamp.Value, 0.1f));
            float rotationAverageMulti = TIRLUtils.FiniteOr(PrimeMover.RotationAverageDTMulti.Value, 80f);

            // general rot detection
            _playerRotationHistory += distance;
            _playerRotationHistory -= _playerRotationAvg;
            _playerRotationHistory = Mathf.Clamp(_playerRotationHistory, 0, historyClamp);
            _playerRotationAvg = _playerRotationHistory * dt * rotationAverageMulti;

            // horizontal tracking
            _horizontalRotationHistory += horizontalMovement;
            _horizontalRotationHistory -= _horizontalRotationValue;
            _horizontalRotationHistory = Mathf.Clamp(_horizontalRotationHistory, -historyClamp, historyClamp);
            _horizontalRotationValue = _horizontalRotationHistory * dt * rotationAverageMulti;

            // vertical tracking
            _verticalRotationHistory += verticalMovement;
            _verticalRotationHistory -= _verticalRotationValue;
            _verticalRotationHistory = Mathf.Clamp(_verticalRotationHistory, -historyClamp, historyClamp);
            _verticalRotationValue = _verticalRotationHistory * dt * rotationAverageMulti;

            SanitizeRotationState();

            // set for next frame
            _playerRotLastFrame = newRot;
        }

        public static float GetNormalSpeed()
        {
            return TIRLUtils.FiniteOr(_playerSpeed / 0.6f, 0f);
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

        public static float VerticalRotationDelta
        {
            get { return _verticalRotationValue; }
        }

        public static float LeanNormal
        {
            get
            {
                return _leanNormalized;
            }
        }

        static void UpdateMovementDirection(Vector3 playerInput)
        {
            playerInput = TIRLUtils.FiniteOr(playerInput, Vector3.zero);
            bool forward = playerInput.y > 0;
            bool backward = playerInput.y < 0;
            bool leftward = playerInput.x < 0;
            bool rightward = playerInput.x > 0;

            if (!_playerMoving)
            {
                _dir = EPlayerDir.NONE;
                //TIRLUtils.Log($"player not moving", false);
                return;
            }

            if (forward && !leftward && !rightward) _dir = EPlayerDir.FWD;
            else if (forward && leftward) _dir = EPlayerDir.FWDLEFT;
            else if (forward && rightward) _dir = EPlayerDir.FWDRIGHT;
            else if (backward && !leftward && !rightward) _dir = EPlayerDir.BWD;
            else if (backward && leftward) _dir = EPlayerDir.BWDLEFT;
            else if (backward && rightward) _dir = EPlayerDir.BWDRIGHT;
            else if (leftward) _dir = EPlayerDir.LEFT;
            else if (rightward) _dir = EPlayerDir.RIGHT;
            else _dir = EPlayerDir.NONE;
        }

        public static EPlayerDir Direction
        {
            get { return _dir; }
        }

        public static float ArmStamNorm
        {
            get { return _armStam; }
        }

        static void SanitizeRotationState()
        {
            if (!TIRLUtils.IsFinite(_playerRotationHistory)) _playerRotationHistory = 0f;
            if (!TIRLUtils.IsFinite(_playerRotationAvg)) _playerRotationAvg = 0f;
            if (!TIRLUtils.IsFinite(_horizontalRotationHistory)) _horizontalRotationHistory = 0f;
            if (!TIRLUtils.IsFinite(_horizontalRotationValue)) _horizontalRotationValue = 0f;
            if (!TIRLUtils.IsFinite(_verticalRotationHistory)) _verticalRotationHistory = 0f;
            if (!TIRLUtils.IsFinite(_verticalRotationValue)) _verticalRotationValue = 0f;
        }
    }
}

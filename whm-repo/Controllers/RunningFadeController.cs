using EFT;
using RealismMod;
using UnityEngine;

namespace TarkovIRL
{
    internal static class RunningFadeController
    {
        enum ERunPoseMode { None, NormalSprint, HighReadySprint }

        static ERunPoseMode _lastMode = ERunPoseMode.None;
        static ERunPoseMode _lastLoggedMode = ERunPoseMode.None;
        static ERunPoseMode _transitionMode = ERunPoseMode.None;
        static Vector3 _positionOffset = Vector3.zero;
        static Vector3 _rotationOffset = Vector3.zero;
        static float _transitionAlpha = 0f;

        public static void Reset()
        {
            _lastMode = ERunPoseMode.None;
            _lastLoggedMode = ERunPoseMode.None;
            _transitionMode = ERunPoseMode.None;
            _positionOffset = Vector3.zero;
            _rotationOffset = Vector3.zero;
            _transitionAlpha = 0f;
        }

        public static void GetRunPoseOffsets(Player player, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (!CanEvaluate(player))
            {
                return;
            }

            if (TryGetDebugOverride(out Vector3 debugPosition, out Vector3 debugRotation))
            {
                _positionOffset = debugPosition;
                _rotationOffset = debugRotation;
                _lastMode = ERunPoseMode.None;
                _transitionMode = ERunPoseMode.None;
                _transitionAlpha = 0f;
                position = _positionOffset;
                rotation = TIRLUtils.GetQuatFromV3(_rotationOffset);
                return;
            }

            ERunPoseMode mode = GetMode(player);
            float dt = GetDeltaTime(player);
            float smoothing = mode == ERunPoseMode.None ? PrimeMover.RunPoseExitSpeed.Value : PrimeMover.RunPoseEnterSpeed.Value;

            GetTargetOffsets(mode, dt, out Vector3 targetPosition, out Vector3 targetRotation);

            float lerp = Mathf.Clamp01(dt * smoothing);
            _positionOffset = Vector3.Lerp(_positionOffset, targetPosition, lerp);
            _rotationOffset = Vector3.Lerp(_rotationOffset, targetRotation, lerp);

            if (_positionOffset.sqrMagnitude < 0.000001f)
            {
                _positionOffset = Vector3.zero;
            }
            if (_rotationOffset.sqrMagnitude < 0.000001f)
            {
                _rotationOffset = Vector3.zero;
            }

            LogModeChange(player, mode);
            _lastMode = mode;

            position = _positionOffset;
            rotation = TIRLUtils.GetQuatFromV3(_rotationOffset);
        }

        static bool CanEvaluate(Player player)
        {
            if (!PrimeMover.IsRunPoseCorrection.Value || player == null || !player.HasFirearmInHands())
            {
                Reset();
                return false;
            }

            return true;
        }

        static ERunPoseMode GetMode(Player player)
        {
            if (player.MovementContext == null || player.ProceduralWeaponAnimation == null)
            {
                return ERunPoseMode.None;
            }

            if (player.IsInventoryOpened || player.IsInPronePose || PlayerMotionController.IsProne)
            {
                return ERunPoseMode.None;
            }

            if (player.ProceduralWeaponAnimation.IsAiming || PlayerMotionController.IsAiming)
            {
                return ERunPoseMode.None;
            }

            if (AnimStateController.IsBlindfire || StanceController.IsLeftShoulder || IsMountedOrStationary(player))
            {
                return ERunPoseMode.None;
            }

            bool isBodySprint = AnimStateController.IsRunning || PlayerMotionController.IsSprinting;
            if (player.MovementContext.CurrentState != null)
            {
                isBodySprint |= player.MovementContext.CurrentState.Name == EPlayerState.Sprint;
            }

            if (!isBodySprint)
            {
                return ERunPoseMode.None;
            }

            if (RealismWrapper.IsTacSprint || StanceController.CurrentStance == EStance.HighReady)
            {
                return ERunPoseMode.HighReadySprint;
            }

            return ERunPoseMode.NormalSprint;
        }

        static bool IsMountedOrStationary(Player player)
        {
            if (WeaponController.IsUsingMounted)
            {
                return true;
            }

            if (player.MovementContext.CurrentState != null && player.MovementContext.CurrentState.Name == EPlayerState.Stationary)
            {
                return true;
            }

            return player.MovementContext.IsInMountedState || player.MovementContext.StationaryWeapon != null;
        }

        static bool TryGetDebugOverride(out Vector3 position, out Vector3 rotation)
        {
            position = Vector3.zero;
            rotation = Vector3.zero;

            if (PrimeMover.DebugRunPoseOverride == null)
            {
                return false;
            }

            switch (PrimeMover.DebugRunPoseOverride.Value)
            {
                case PrimeMover.ERunPoseDebugOverride.NormalSprintTransition:
                    position = PrimeMover.RunPoseSprintTransitionPosition.Value;
                    rotation = PrimeMover.RunPoseSprintTransitionRotation.Value;
                    return true;
                case PrimeMover.ERunPoseDebugOverride.HighReadyTransition:
                    position = PrimeMover.RunPoseHighReadyTransitionPosition.Value;
                    rotation = PrimeMover.RunPoseHighReadyTransitionRotation.Value;
                    return true;
                default:
                    return false;
            }
        }

        static void GetTargetOffsets(ERunPoseMode mode, float dt, out Vector3 position, out Vector3 rotation)
        {
            if (mode == ERunPoseMode.None)
            {
                GetExitOffsets(dt, out position, out rotation);
                return;
            }

            if (_lastMode == ERunPoseMode.None || _lastMode != mode)
            {
                _transitionAlpha = 1f;
                _transitionMode = mode;
            }

            _transitionAlpha = Mathf.MoveTowards(_transitionAlpha, 0f, dt * Mathf.Max(0.1f, PrimeMover.RunPoseEnterSpeed.Value));

            GetFinalOffsets(mode, out Vector3 finalPosition, out Vector3 finalRotation);
            GetTransitionOffsets(mode, out Vector3 transitionPosition, out Vector3 transitionRotation);

            position = Vector3.Lerp(finalPosition, transitionPosition, _transitionAlpha);
            rotation = Vector3.Lerp(finalRotation, transitionRotation, _transitionAlpha);
        }

        static void GetExitOffsets(float dt, out Vector3 position, out Vector3 rotation)
        {
            if (_lastMode != ERunPoseMode.None)
            {
                _transitionAlpha = 1f;
                _transitionMode = _lastMode;
            }

            if (_transitionMode == ERunPoseMode.None || _transitionAlpha <= 0f)
            {
                _transitionMode = ERunPoseMode.None;
                position = Vector3.zero;
                rotation = Vector3.zero;
                return;
            }

            _transitionAlpha = Mathf.MoveTowards(_transitionAlpha, 0f, dt * Mathf.Max(0.1f, PrimeMover.RunPoseExitSpeed.Value));
            GetTransitionOffsets(_transitionMode, out Vector3 transitionPosition, out Vector3 transitionRotation);
            position = transitionPosition * _transitionAlpha;
            rotation = transitionRotation * _transitionAlpha;
        }

        static void GetFinalOffsets(ERunPoseMode mode, out Vector3 position, out Vector3 rotation)
        {
            switch (mode)
            {
                case ERunPoseMode.HighReadySprint:
                    position = PrimeMover.RunPoseHighReadyPosition.Value;
                    rotation = PrimeMover.RunPoseHighReadyRotation.Value;
                    break;
                case ERunPoseMode.NormalSprint:
                    position = PrimeMover.RunPoseSprintPosition.Value;
                    rotation = PrimeMover.RunPoseSprintRotation.Value;
                    break;
                default:
                    position = Vector3.zero;
                    rotation = Vector3.zero;
                    break;
            }
        }

        static void GetTransitionOffsets(ERunPoseMode mode, out Vector3 position, out Vector3 rotation)
        {
            switch (mode)
            {
                case ERunPoseMode.HighReadySprint:
                    position = PrimeMover.RunPoseHighReadyTransitionPosition.Value;
                    rotation = PrimeMover.RunPoseHighReadyTransitionRotation.Value;
                    break;
                case ERunPoseMode.NormalSprint:
                    position = PrimeMover.RunPoseSprintTransitionPosition.Value;
                    rotation = PrimeMover.RunPoseSprintTransitionRotation.Value;
                    break;
                default:
                    position = Vector3.zero;
                    rotation = Vector3.zero;
                    break;
            }
        }

        static float GetDeltaTime(Player player)
        {
            float dt = PrimeMover.Instance != null ? PrimeMover.Instance.DeltaTime : player.DeltaTime;
            if (dt <= 0f)
            {
                dt = Time.deltaTime;
            }

            return Mathf.Clamp(dt, 0f, 1f / 15f);
        }

        static void LogModeChange(Player player, ERunPoseMode mode)
        {
            if (!PrimeMover.IsLogging.Value || mode == _lastLoggedMode)
            {
                return;
            }

            string bodyStateName = "null";
            int bodyStateHash = 0;
            if (player.MovementContext != null && player.MovementContext.CurrentState != null)
            {
                bodyStateName = player.MovementContext.CurrentState.AnimatorStateName;
                bodyStateHash = player.MovementContext.CurrentState.AnimatorStateHash;
            }

            TIRLUtils.Logger.LogInfo((object)$"Run pose mode {mode}; transition {_transitionMode}/{_transitionAlpha}; body {bodyStateName}/{bodyStateHash}; hands {AnimStateController.WeaponState}/{AnimStateController.WeaponStateHash}; stance {StanceController.CurrentStance}; tacSprint {RealismWrapper.IsTacSprint}");
            _lastLoggedMode = mode;
        }
    }
}

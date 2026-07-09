using EFT;
using System;
using UnityEngine;
using static TarkovIRL.AnimStateController;

namespace TarkovIRL
{
    internal static class AugmentedReloadController
    {
        static readonly float _IntoReloadX = 17f;
        static readonly float _IntoReloadY = 4f;
        static readonly float _IntoReloadZ = 5f;

        static readonly float _OutReloadX = 7f;
        static readonly float _OutReloadY = 2f;
        static readonly float _OutReloadZ = -3f;

        public static bool _AugmentedModeOn = false;
        static EWeaponState _state;
        static EWeaponState _stateLastFrame;
        static Player _player = null;
        static ObjectInHandsAnimator _animator = null;
        static ObjectInHandsAnimator _animatorWithSpeedOverride = null;
        static bool _speedOverrideActive = false;

        static readonly int _animatorLayer = 1;

        public static Vector3 GetAugmentedReloadHeadOffset()
        {
            if (!PrimeMover.IsAugmentedReload.Value)
            {
                return Vector3.zero;
            }

            _state = AnimStateController.WeaponState;

            if (!_AugmentedModeOn || !IsPlayerHeldFirearm())
            {
                return Vector3.zero;
            }

            if (_state == EWeaponState.INTO_RELOAD)
            {
                return new Vector3(_OutReloadX, _OutReloadY, _OutReloadZ);
            }

            else if (_state == EWeaponState.MID_RELOAD)
            {
                return new Vector3(_IntoReloadX, _IntoReloadY, _IntoReloadZ);
            }

            else if (_state == EWeaponState.MID_RELOAD_2)
            {
                return new Vector3(_OutReloadX, _OutReloadY, _OutReloadZ);
            }

            else if (_state == EWeaponState.CHECK_MAG)
            {
                return new Vector3(_OutReloadX, _OutReloadY, _OutReloadZ);
            }

            return Vector3.zero;
        }

        public static void ToggleAugmentedMode()
        {
            _state = AnimStateController.WeaponState;
            if (IsAugmentedReloadActionState())
            {
                _AugmentedModeOn = !_AugmentedModeOn;
            }
            else
            {
                _AugmentedModeOn = false;
            }
        }

        public static void RefreshAnimator(Player player)
        {
            if (_animator != null && player != null && _animator != player.HandsAnimator)
            {
                ResetAnimSpeed();
            }

            _player = player;
            _animator = player?.HandsAnimator;
        }

        public static void RefreshAnimator(ObjectInHandsAnimator animator)
        {
            if (_animator != null && _animator != animator)
            {
                ResetAnimSpeed();
            }

            _player = null;
            _animator = animator;
        }

        public static void Update()
        {
            _state = AnimStateController.WeaponState;

            if (!PrimeMover.IsAugmentedReload.Value)
            {
                _AugmentedModeOn = false;
                ResetAnimSpeed();
                return;
            }

            if (_animator == null)
            {
                _AugmentedModeOn = false;
                ResetAnimSpeed();
                return;
            }

            if (!IsPlayerHeldFirearm())
            {
                _AugmentedModeOn = false;
                ResetAnimSpeed();
                return;
            }

            if (_stateLastFrame != EWeaponState.INTO_RELOAD && _state == EWeaponState.INTO_RELOAD)
            {
                if (PrimeMover.IsAugmentedReloadDefault.Value)
                {
                    _AugmentedModeOn = true;
                }
            }

            UpdateSpeed();
            _stateLastFrame = _state;
        }

        static void UpdateSpeed()
        {
            if (!IsAugmentedReloadActionState())
            {
                _AugmentedModeOn = false;
                ResetAnimSpeed();
            }
            else
            {
                SetAnimSpeed(GetAnimationSpeedFromContext());
            }
        }

        static void SetAnimSpeed(float speed)
        {
            if (_animator == null)
            {
                return;
            }
            try
            {
                _animator.SetAnimationSpeed(speed);
                _animatorWithSpeedOverride = _animator;
                _speedOverrideActive = true;
            }
            catch (Exception)
            {
                //TIRLUtils.LogError("set anim speed failed");
                _animator = null;
            }
        }

        static void ResetAnimSpeed()
        {
            if (!_speedOverrideActive)
            {
                return;
            }

            ObjectInHandsAnimator animator = _animatorWithSpeedOverride ?? _animator;
            if (animator != null)
            {
                try
                {
                    animator.SetAnimationSpeed(1f);
                }
                catch (Exception)
                {
                    //TIRLUtils.LogError("reset anim speed failed");
                }
            }

            _animatorWithSpeedOverride = null;
            _speedOverrideActive = false;
        }

        static float GetAnimationSpeedFromContext()
        {
            float sprintingMulti = PlayerMotionController.IsSprinting ? PrimeMover.AugmentedReloadSprintingDebuff.Value : 1f;
            float augmentedMulti = _AugmentedModeOn ? PrimeMover.AugmentedReloadSpeed.Value : 1f;
            float slowerReloadMulti;
            if (_animator != null && _state == EWeaponState.MID_RELOAD)
            {
                try
                {
                    float animTime = _animator.Animator.GetCurrentAnimatorStateInfo(_animatorLayer).normalizedTime;
                    slowerReloadMulti = animTime < 1f ? PrimeMover.Instance.SlowReloadCurve.Evaluate(animTime) : 1f;
                }
                catch (NullReferenceException e)
                {
                    TIRLUtils.LogError($"anim layer {_animatorLayer} returned null -- {e}");
                    slowerReloadMulti = 1f;
                }
            }
            else
            {
                slowerReloadMulti = 1f;
            }
            float realismSpeed = _state == EWeaponState.CHECK_MAG ? RealismWrapper.GetRealismCheckMagSpeed() : RealismWrapper.GetRealismReloadSpeed();
            return sprintingMulti * augmentedMulti * realismSpeed * slowerReloadMulti;
        }

        static bool IsAugmentedReloadActionState()
        {
            return IsReloadAnimationState() || _state == EWeaponState.CHECK_MAG;
        }

        static bool IsReloadAnimationState()
        {
            return _state == EWeaponState.INTO_RELOAD || _state == EWeaponState.MID_RELOAD || _state == EWeaponState.MID_RELOAD_2 || _state == EWeaponState.OUT_OF_RELOAD || _state == EWeaponState.RELOAD_FAST;
        }

        static bool IsPlayerHeldFirearm()
        {
            if (_player == null)
            {
                return true;
            }

            if (_player.MovementContext != null && _player.MovementContext.StationaryWeapon != null)
            {
                return false;
            }

            return _player.HasFirearmInHands();
        }
    }
}

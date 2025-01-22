using EFT;
using System;
using UnityEngine;
using static TarkovIRL.AnimStateController;

namespace TarkovIRL
{
    internal class AugmentedReloadController
    {
        static readonly float _IntoReloadX = 17f;
        static readonly float _IntoReloadY = 4f;
        static readonly float _IntoReloadZ = 5f;

        static readonly float _OutReloadX = 7f;
        static readonly float _OutReloadY = 2f;
        static readonly float _OutReloadZ = -3f;

        static bool _AugmentedModeOn = false;
        static EWeaponState _state;
        static ObjectInHandsAnimator _animator = null;
        static float _refreshAnimatorTimer = 0;

        static int _animatorLayer = 1;

        public static Vector3 GetAugmentedReloadHeadOffset()
        {
            _state = AnimStateController.WeaponState;

            if (!_AugmentedModeOn)
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
            if (AugmentedSwitchOpen())
            {
                _AugmentedModeOn = !_AugmentedModeOn;
            }
            else
            {
                _AugmentedModeOn = false;
            }
        }

        public static void RefreshAnimator(ObjectInHandsAnimator animator)
        {
            _animator = animator;
        }

        public static void Update()
        {
            if (_animator == null)
            {
                return;
            }

            UpdateSpeed();
        }

        static void UpdateSpeed()
        {
            if (!AugmentedSwitchOpen())
            {
                _AugmentedModeOn = false;
            }
            else
            {
                SetAnimSpeed(GetReloadSpeedFromContext());
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
            }
            catch (Exception e)
            {
                UtilsTIRL.Log($"set anim speed failed, {e}");
            }
        }

        static float GetReloadSpeedFromContext()
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
                    UtilsTIRL.Log($"anim layer {_animatorLayer} returned null -- {e}");
                    slowerReloadMulti = 1f;
                }
            }
            else
            {
                slowerReloadMulti = 1f;
            }
            float realismSpeed = _state == EWeaponState.CHECK_MAG ? RealismWrapper.GetRealismCheckMagSpeed() : RealismWrapper.GetRealismReloadSpeed();

            //UtilsTIRL.Log($"reload state : {_state} at {_animator.Animator.GetCurrentAnimatorStateInfo(1).normalizedTime}");

            return sprintingMulti * augmentedMulti * realismSpeed * slowerReloadMulti;
        }

        static bool AugmentedSwitchOpen()
        {
            return _state == EWeaponState.INTO_RELOAD || _state == EWeaponState.MID_RELOAD || _state == EWeaponState.MID_RELOAD_2 || _state == EWeaponState.OUT_OF_RELOAD || _state == EWeaponState.RELOAD_FAST || _state == EWeaponState.CHECK_MAG;
        }
    }
}
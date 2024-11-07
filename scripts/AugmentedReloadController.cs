using UnityEngine;
using EFT;
using static TarkovIRL.AnimStateController;

namespace TarkovIRL
{
    internal class AugmentedReloadController
    {
        static readonly float _IntoReloadX = 17f;
        static readonly float _IntoReloadY = 4f;
        static readonly float _IntoReloadZ = 5f;

        static readonly float _OutReloadX = 7;
        static readonly float _OutReloadY = 2;
        static readonly float _OutReloadZ = -3f;

        static readonly float _AugmentedSpeed = 1.3f;

        static bool _AugmentedModeOn = false;
        static EWeaponState _state;
        static EWeaponState _stateLastFrame;
        static ObjectInHandsAnimator _animator = null;

        public static Vector3 GetAugmentedReloadHeadOffset()
        {
            _stateLastFrame = _state;
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

            else if (_state == EWeaponState.OUT_OF_RELOAD)
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
            if (_state == EWeaponState.INTO_RELOAD || _state == EWeaponState.MID_RELOAD || _state == EWeaponState.CHECK_MAG)
            {
                _AugmentedModeOn = !_AugmentedModeOn;
                float newAnimSpeed = _animator.Animator.GetCurrentAnimatorStateInfo(1).speed * _AugmentedSpeed;
                SetAnimSpeed(newAnimSpeed);
            }
            else
            {
                _AugmentedModeOn = false;
                float newAnimSpeed = _animator.Animator.GetCurrentAnimatorStateInfo(1).speed * (1f / _AugmentedSpeed);
                SetAnimSpeed(newAnimSpeed);
            }
        }

        public static void Update(ObjectInHandsAnimator animator)
        {
            if (_animator == null)
            {
                _animator = animator;
            }
            else
            {
                if (_animator.Animator != null)
                {
                    MaintainCorrectSpeedDuringReload();
                    float currentAnimSpeed = _animator.Animator.GetCurrentAnimatorStateInfo(1).speed;
                    //UtilsTIRL.Log($"anim speed {currentAnimSpeed}");
                }
            }
            if (_state == EWeaponState.IDLE)
            {
                _AugmentedModeOn = false;
            }
        }

        static void MaintainCorrectSpeedDuringReload()
        {
            if (_stateLastFrame != _state)
            {
                if (_stateLastFrame == EWeaponState.INTO_RELOAD && _state == EWeaponState.MID_RELOAD)
                {
                    if (_AugmentedModeOn)
                    {
                        float newAnimSpeed = _animator.Animator.GetCurrentAnimatorStateInfo(1).speed * _AugmentedSpeed;
                        SetAnimSpeed(newAnimSpeed);
                    }
                }
                if (_stateLastFrame == EWeaponState.MID_RELOAD && _state == EWeaponState.OUT_OF_RELOAD)
                {
                    if (_AugmentedModeOn)
                    {
                        float newAnimSpeed = _animator.Animator.GetCurrentAnimatorStateInfo(1).speed * _AugmentedSpeed;
                        SetAnimSpeed(newAnimSpeed);
                    }
                }
                else if (_stateLastFrame == EWeaponState.OUT_OF_RELOAD && _state == EWeaponState.IDLE)
                {
                    if (_AugmentedModeOn)
                    {
                        float newAnimSpeed = _animator.Animator.GetCurrentAnimatorStateInfo(1).speed * (1f / _AugmentedSpeed);
                        //SetAnimSpeed(newAnimSpeed);
                    }
                }
            }
        }

        static void SetAnimSpeed(float speed)
        {
            _animator.SetAnimationSpeed(speed);
        }
    }
}
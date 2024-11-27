using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT;
using UnityEngine;
using EFT.UI;
using UnityEngine.Assertions.Must;
using System;

namespace TarkovIRL
{
    internal class WeaponSelectionController
    {
        public enum EWeaponSelection { SLING, SHOULDER, PISTOL, OTHER }
        static EWeaponSelection _lastSelectedWeapon;

        enum ETransitionPhase { FIRST_FRAME, ORDER_ARM, PRESENT_ARM, DONE_IDLE }
        static ETransitionPhase _transitionPhase = ETransitionPhase.DONE_IDLE;

        enum ETransitionType { SLING_TO_SHOULDER, SHOULDER_TO_SLING, QUICK_PISTOL_TO_SLING, QUICK_SLING_TO_PISTOL, QUICK_SHOULDER_TO_PISTOL, PISTOL_TO_SHOULDER, SLING_TO_PISTOL, SHOULDER_TO_PISTOL, PISTOL_TO_SLING };
        static ETransitionType _transitionType;

        static Vector3 _posLerp = Vector3.zero;
        static Vector3 _posStart = Vector3.zero;
        static Vector3 _posEnd = Vector3.zero;

        static Vector3 _rotLerp = Vector3.zero;
        static Vector3 _rotStart = Vector3.zero;
        static Vector3 _rotEnd = Vector3.zero;

        static Vector3 _posLerpHistory = Vector3.zero;
        static Vector3 _rotLerpHistory = Vector3.zero;
        static Vector3 _posLerpSmoothed = Vector3.zero;
        static Vector3 _rotLerpSmoothed = Vector3.zero;

        static float _processLerp = 0f;
        static AnimationCurve _speedCurve;
        static Player _player = null;

        static AnimationCurve _animSpeedCurvePhase1 = new AnimationCurve();
        static AnimationCurve _animSpeedCurvePhase2 = new AnimationCurve();

        static float _processSpeedPhase1 = 0;
        static float _processSpeedPhase2 = 0;

        static float _weaponHashLastFrame = 0;
        static bool _changedStateWindowOpen = false;

        public static void UpdateAnimationPump(float dt)
        {

            _posLerpHistory.x += _posLerp.x;
            _posLerpHistory.y += _posLerp.y;
            _posLerpHistory.z += _posLerp.z;

            _posLerpHistory.x -= _posLerpSmoothed.x;
            _posLerpHistory.y -= _posLerpSmoothed.y;
            _posLerpHistory.z -= _posLerpSmoothed.z;

            _rotLerpHistory.x += _rotLerp.x;
            _rotLerpHistory.y += _rotLerp.y;
            _rotLerpHistory.z += _rotLerp.z;

            _rotLerpHistory.x -= _rotLerpSmoothed.x;
            _rotLerpHistory.y -= _rotLerpSmoothed.y;
            _rotLerpHistory.z -= _rotLerpSmoothed.z;

            _posLerpSmoothed.x = _posLerpHistory.x * dt * PrimeMover.TransformSmoothingDTMulti.Value;
            _posLerpSmoothed.y = _posLerpHistory.y * dt * PrimeMover.TransformSmoothingDTMulti.Value;
            _posLerpSmoothed.z = _posLerpHistory.z * dt * PrimeMover.TransformSmoothingDTMulti.Value;

            _rotLerpSmoothed.x = _rotLerpHistory.x * dt * PrimeMover.TransformSmoothingDTMulti.Value;
            _rotLerpSmoothed.y = _rotLerpHistory.y * dt * PrimeMover.TransformSmoothingDTMulti.Value;
            _rotLerpSmoothed.z = _rotLerpHistory.z * dt * PrimeMover.TransformSmoothingDTMulti.Value;

            if (_transitionPhase == ETransitionPhase.DONE_IDLE)
            {
                _posLerp = Vector3.zero;
                _rotLerp = Vector3.zero;
                _posStart = Vector3.zero;
                _posEnd = Vector3.zero;
                _rotStart = Vector3.zero;
                _rotEnd = Vector3.zero;
                _changedStateWindowOpen = true;
                return;
            }

            if (_transitionPhase == ETransitionPhase.FIRST_FRAME)
            {
                _processLerp = 0;
                _posLerp = Vector3.zero;
                _rotLerp = Vector3.zero;
                _posStart = Vector3.zero;
                _posEnd = Vector3.zero;
                _rotStart = Vector3.zero;
                _rotEnd = Vector3.zero;

                _transitionPhase = ETransitionPhase.ORDER_ARM;
                return;
            }

            //UtilsTIRL.Log($" _transitionPhase - {_transitionPhase} ");

            if (_player != null)
            {
                if (_player.HandsIsEmpty)
                {
                    return;
                }
                if (_player.HandsAnimator == null)
                {
                    return;
                }
                if (_player.HandsAnimator.Animator == null)
                {
                    return;
                }

                if (_speedCurve == null)
                {
                    return;
                }

                if (_transitionPhase == ETransitionPhase.ORDER_ARM)
                {
                    if (WeaponController.WeaponHash != _weaponHashLastFrame)
                    {
                        _changedStateWindowOpen = true;
                    }
                }

                // you can get the speed straight out of the anim curve as long
                // as the curve never hits zero...and is going at a reasonable rate

                _speedCurve = _transitionPhase == ETransitionPhase.ORDER_ARM ? _animSpeedCurvePhase1 : _animSpeedCurvePhase2;
                float processSpeed = _speedCurve.Evaluate(_processLerp);
                processSpeed *= _transitionPhase == ETransitionPhase.ORDER_ARM ? _processSpeedPhase1 : _processSpeedPhase2;
                float efficiencyMod = Mathf.Clamp(EfficiencyController.EfficiencyModifierInverse, 0.5f, 1.5f);
                float proneMod = PlayerMotionController.IsProne ? 0.33f : 1f;
                float speed = processSpeed * WeaponController.GetWeaponMulti(true) * efficiencyMod * proneMod;

                _processLerp = UtilsTIRL.FulfilledLerp(_processLerp, 1f, speed * dt);
                SetAnimSpeed(_player, speed * 1.1f);

                _posLerp = Vector3.Lerp(_posStart, _posEnd, _processLerp);
                _rotLerp = Vector3.Lerp(_rotStart, _rotEnd, _processLerp);

                //UtilsTIRL.Log($"speed {speed}, processlerp {_processLerp}, _changedStateWindowOpen {_changedStateWindowOpen}, _transitionPhase {_transitionPhase}, stateChange {WeaponController.WeaponHash != _weaponHashLastFrame}");

                if (_processLerp >= 0.98f)
                {
                    if (_transitionPhase == ETransitionPhase.ORDER_ARM && _changedStateWindowOpen)
                    {
                        _transitionPhase = ETransitionPhase.PRESENT_ARM;
                        _processLerp = 0;

                        // trying this to close the gap between the phases
                        _posLerpSmoothed = _posLerp;
                        _rotLerpSmoothed = _rotLerp;
                    }
                    else if (_transitionPhase == ETransitionPhase.PRESENT_ARM)
                    {
                        // wrap up transition
                        _transitionPhase = ETransitionPhase.DONE_IDLE;
                        _processLerp = 0;
                        _posLerp = Vector3.zero;
                        _rotLerp = Vector3.zero;
                        _posStart = Vector3.zero;
                        _posEnd = Vector3.zero;
                        _rotStart = Vector3.zero;
                        _rotEnd = Vector3.zero;
                    }
                }

                // resetting the targets
                SetParamsThisFrame();

                _weaponHashLastFrame = WeaponController.WeaponHash;
            }
        }

        static void SetParamsThisFrame()
        {
            ETransitionPhase phase = _transitionPhase;
            ETransitionType type = _transitionType;

            if (type == ETransitionType.SLING_TO_SHOULDER)
            {
                if (phase == ETransitionPhase.ORDER_ARM)
                {
                    _posStart = Vector3.zero;
                    _posEnd = new Vector3(PrimeMover.SlingPositionEndX.Value, PrimeMover.SlingPositionEndY.Value, PrimeMover.SlingPositionEndZ.Value);

                    _rotStart = Vector3.zero;
                    _rotEnd = new Vector3(PrimeMover.SlingRotationEndX.Value, PrimeMover.SlingRotationEndY.Value, PrimeMover.SlingRotationEndZ.Value);
                }
                else
                {
                    _posStart = new Vector3(PrimeMover.ShoulderPositionStartX.Value, PrimeMover.ShoulderPositionStartY.Value, PrimeMover.ShoulderPositionStartZ.Value);
                    _posEnd = Vector3.zero;

                    _rotStart = new Vector3(PrimeMover.ShoulderRotationStartX.Value, PrimeMover.ShoulderRotationStartY.Value, PrimeMover.ShoulderRotationStartZ.Value);
                    _rotEnd = Vector3.zero;
                }
            }

            if (type == ETransitionType.SHOULDER_TO_SLING)
            {
                if (phase == ETransitionPhase.ORDER_ARM)
                {
                    _posStart = Vector3.zero;
                    _posEnd = new Vector3(PrimeMover.ShoulderPositionEndX.Value, PrimeMover.ShoulderPositionEndY.Value, PrimeMover.ShoulderPositionEndZ.Value);

                    _rotStart = Vector3.zero;
                    _rotEnd = new Vector3(PrimeMover.ShoulderRotationEndX.Value, PrimeMover.ShoulderRotationEndY.Value, PrimeMover.ShoulderRotationEndZ.Value);
                }
                else
                {
                    _posStart = new Vector3(PrimeMover.SlingPositionStartX.Value, PrimeMover.SlingPositionStartY.Value, PrimeMover.SlingPositionStartZ.Value);
                    _posEnd = Vector3.zero;

                    _rotStart = new Vector3(PrimeMover.SlingRotationStartX.Value, PrimeMover.SlingRotationStartY.Value, PrimeMover.SlingRotationStartZ.Value);
                    _rotEnd = Vector3.zero;
                }
            }

            if (type == ETransitionType.QUICK_SLING_TO_PISTOL)
            {
                if (phase == ETransitionPhase.ORDER_ARM)
                {
                    _posStart = Vector3.zero;
                    _posEnd = new Vector3(PrimeMover.SlingPositionEndX.Value, PrimeMover.SlingPositionEndY.Value, PrimeMover.SlingPositionEndZ.Value);

                    _rotStart = Vector3.zero;
                    _rotEnd = new Vector3(PrimeMover.SlingRotationEndX.Value, PrimeMover.SlingRotationEndY.Value, PrimeMover.SlingRotationEndZ.Value);
                }
                else
                {
                    _posStart = new Vector3(PrimeMover.HolsterPositionStartX.Value, PrimeMover.HolsterPositionStartY.Value, PrimeMover.HolsterPositionStartZ.Value);
                    _posEnd = Vector3.zero;

                    _rotStart = new Vector3(PrimeMover.HolsterRotationStartX.Value, PrimeMover.HolsterRotationStartY.Value, PrimeMover.HolsterRotationStartZ.Value);
                    _rotEnd = Vector3.zero;
                }
            }

            if (type == ETransitionType.PISTOL_TO_SHOULDER)
            {
                if (phase == ETransitionPhase.ORDER_ARM)
                {
                    _posStart = Vector3.zero;
                    _posEnd = Vector3.zero;

                    _rotStart = Vector3.zero;
                    _rotEnd = Vector3.zero;
                }
                else
                {
                    _posStart = new Vector3(PrimeMover.ShoulderPositionStartX.Value, PrimeMover.ShoulderPositionStartY.Value, PrimeMover.ShoulderPositionStartZ.Value);
                    _posEnd = Vector3.zero;

                    _rotStart = new Vector3(PrimeMover.ShoulderRotationStartX.Value, PrimeMover.ShoulderRotationStartY.Value, PrimeMover.ShoulderRotationStartZ.Value);
                    _rotEnd = Vector3.zero;
                }
            }
        }

        public static void GetWeaponSelectionTransforms(out Vector3 pos, out Quaternion rot)
        {
            pos = _posLerpSmoothed;
            rot = UtilsTIRL.GetQuatFromV3(_rotLerpSmoothed);
        }

        public static void Process(ECommand command, Player player)
        {
            _speedCurve = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1f, 1f));
            _player = player;

            if (_transitionPhase != ETransitionPhase.DONE_IDLE)
            {
                return;
            }
            if (AnimStateController.WeaponState == AnimStateController.EWeaponState.UNKNOWN_STATE)
            {
                return;
            }
            //UtilsTIRL.Log($"weapon state {AnimStateController.WeaponState}");

            Item newLastWeapon = player.LastEquippedWeaponOrKnifeItem;
            Item slingWeapon = player.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem;
            Item shoulderWeapon = player.Inventory.Equipment.GetSlot(EquipmentSlot.SecondPrimaryWeapon).ContainedItem;
            Item holsterWeapon = player.Inventory.Equipment.GetSlot(EquipmentSlot.Holster).ContainedItem;

            if (newLastWeapon.Equals(slingWeapon))
            {
                if (command == ECommand.SelectFirstPrimaryWeapon)
                {
                    return;
                }
                _lastSelectedWeapon = EWeaponSelection.SLING;
            }
            if (newLastWeapon.Equals(shoulderWeapon))
            {
                if (command == ECommand.SelectSecondPrimaryWeapon)
                {
                    return;
                }
                _lastSelectedWeapon = EWeaponSelection.SHOULDER;
            }
            if (newLastWeapon.Equals(holsterWeapon))
            {
                if (command == ECommand.SelectSecondaryWeapon)
                {
                    return;
                }
                _lastSelectedWeapon = EWeaponSelection.PISTOL;
            }
            if (newLastWeapon.Equals(holsterWeapon))
            {
                if (command == ECommand.QuickSelectSecondaryWeapon)
                {
                    if (slingWeapon == null)
                    {
                        return;
                    }
                }
                _lastSelectedWeapon = EWeaponSelection.PISTOL;
            }

            // TODO: find cases and information for selecting from vest
            if (command == ECommand.SelectFirstPrimaryWeapon && slingWeapon == null)
            {
                return;
            }
            if (command == ECommand.SelectSecondPrimaryWeapon && shoulderWeapon == null)
            {
                return;
            }
            if (command == ECommand.SelectSecondaryWeapon && holsterWeapon == null)
            {
                return;
            }

            // swap between sling and shoulder
            if (command == ECommand.SelectFirstPrimaryWeapon && _lastSelectedWeapon == EWeaponSelection.SHOULDER)
            {
                ProcessShoulderToSling();
            }
            if (command == ECommand.SelectSecondPrimaryWeapon && _lastSelectedWeapon == EWeaponSelection.SLING)
            {
                ProcessSlingToShoulder();
            }

            // quick swap bx sling and pistol
            if (command == ECommand.QuickSelectSecondaryWeapon && _lastSelectedWeapon == EWeaponSelection.PISTOL)
            {
                ProcessSlingToPistol();
            }
            if (command == ECommand.QuickSelectSecondaryWeapon && _lastSelectedWeapon == EWeaponSelection.SLING)
            {
                ProcessQuickSlingToPistol();
            }

            // quick swap between pistol and shoulder should be disabled
            if (command == ECommand.SelectSecondaryWeapon && _lastSelectedWeapon == EWeaponSelection.SHOULDER)
            {
                ProcessShoulderToPistol();
            }

            // transition from pistol to shoulder
            if (command == ECommand.SelectSecondPrimaryWeapon && _lastSelectedWeapon == EWeaponSelection.PISTOL)
            {
                ProcessPistolToShoulder();
                UtilsTIRL.Log($"pistol to shoulder");
            }
        }

        static void ProcessShoulderToSling()
        {
            _transitionType = ETransitionType.SHOULDER_TO_SLING;
            _transitionPhase = ETransitionPhase.FIRST_FRAME;
            _changedStateWindowOpen = false;

            _animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0, PrimeMover.Shoulder2SlingCurve1_1.Value), new Keyframe(0.2f, PrimeMover.Shoulder2SlingCurve2_1.Value), new Keyframe(0.4f, PrimeMover.Shoulder2SlingCurve3_1.Value), new Keyframe(0.6f, PrimeMover.Shoulder2SlingCurve4_1.Value), new Keyframe(0.8f, PrimeMover.Shoulder2SlingCurve5_1.Value), new Keyframe(1, PrimeMover.Shoulder2SlingCurve6_1.Value));
            _animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0, PrimeMover.Shoulder2SlingCurve1_2.Value), new Keyframe(0.2f, PrimeMover.Shoulder2SlingCurve2_1.Value), new Keyframe(0.4f, PrimeMover.Shoulder2SlingCurve3_2.Value), new Keyframe(0.6f, PrimeMover.Shoulder2SlingCurve4_2.Value), new Keyframe(0.8f, PrimeMover.Shoulder2SlingCurve5_2.Value), new Keyframe(1, PrimeMover.Shoulder2SlingCurve6_2.Value));

            _processSpeedPhase1 = PrimeMover.Shoulder2SlingSpeed1.Value;
            _processSpeedPhase2 = PrimeMover.Shoulder2SlingSpeed2.Value;
        }

        static void ProcessSlingToShoulder()
        {
            _transitionType = ETransitionType.SLING_TO_SHOULDER;
            _transitionPhase = ETransitionPhase.FIRST_FRAME;
            _changedStateWindowOpen = false;

            _animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0, PrimeMover.Sling2ShoulderCurve1_1.Value), new Keyframe(0.2f, PrimeMover.Sling2ShoulderCurve2_1.Value), new Keyframe(0.4f, PrimeMover.Sling2ShoulderCurve3_1.Value), new Keyframe(0.6f, PrimeMover.Sling2ShoulderCurve4_1.Value), new Keyframe(0.8f, PrimeMover.Sling2ShoulderCurve5_1.Value), new Keyframe(1, PrimeMover.Sling2ShoulderCurve6_1.Value));
            _animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0, PrimeMover.Sling2ShoulderCurve1_2.Value), new Keyframe(0.2f, PrimeMover.Sling2ShoulderCurve2_2.Value), new Keyframe(0.4f, PrimeMover.Sling2ShoulderCurve3_2.Value), new Keyframe(0.6f, PrimeMover.Sling2ShoulderCurve4_2.Value), new Keyframe(0.8f, PrimeMover.Sling2ShoulderCurve5_2.Value), new Keyframe(1, PrimeMover.Sling2ShoulderCurve6_2.Value));

            _processSpeedPhase1 = PrimeMover.Sling2ShoulderSpeed1.Value;
            _processSpeedPhase2 = PrimeMover.Sling2ShoulderSpeed2.Value;
        }

        static void ProcessSlingToPistol()
        {
            _transitionType = ETransitionType.SLING_TO_PISTOL;
            _transitionPhase = ETransitionPhase.FIRST_FRAME;
            _changedStateWindowOpen = false;

            _animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0, PrimeMover.Sling2ShoulderCurve1_1.Value), new Keyframe(0.2f, PrimeMover.Sling2ShoulderCurve2_1.Value), new Keyframe(0.4f, PrimeMover.Sling2ShoulderCurve3_1.Value), new Keyframe(0.6f, PrimeMover.Sling2ShoulderCurve4_1.Value), new Keyframe(0.8f, PrimeMover.Sling2ShoulderCurve5_1.Value), new Keyframe(1, PrimeMover.Sling2ShoulderCurve6_1.Value));
            _animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 1f));

            _processSpeedPhase1 = 1f;
            _processSpeedPhase2 = 1f;
        }

        static void ProcessQuickSlingToPistol()
        {
            _transitionType = ETransitionType.QUICK_SLING_TO_PISTOL;
            _transitionPhase = ETransitionPhase.FIRST_FRAME;
            _changedStateWindowOpen = false;

            _animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(0.2f, 3f), new Keyframe(0.4f, 3f), new Keyframe(0.6f, 3f), new Keyframe(0.8f, 3), new Keyframe(1, 3f));
            _animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0, 3f), new Keyframe(1, 3f));

            _processSpeedPhase1 = 1f;
            _processSpeedPhase2 = 1f;
        }

        static void ProcessShoulderToPistol()
        {
            // this should interrupt the normal quickdraw
            // since it shouldn't be a thing when using shoulder
            // slot
            _transitionType = ETransitionType.SHOULDER_TO_PISTOL;
            _transitionPhase = ETransitionPhase.FIRST_FRAME;
            _changedStateWindowOpen = false;

            _animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0, PrimeMover.Shoulder2SlingCurve1_1.Value), new Keyframe(0.2f, PrimeMover.Shoulder2SlingCurve2_1.Value), new Keyframe(0.4f, PrimeMover.Shoulder2SlingCurve3_1.Value), new Keyframe(0.6f, PrimeMover.Shoulder2SlingCurve4_1.Value), new Keyframe(0.8f, PrimeMover.Shoulder2SlingCurve5_1.Value), new Keyframe(1, PrimeMover.Shoulder2SlingCurve6_1.Value));
            _animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 1f));

            _processSpeedPhase1 = PrimeMover.Sling2ShoulderSpeed1.Value;
            _processSpeedPhase2 = PrimeMover.Sling2ShoulderSpeed2.Value;
        }
        static void ProcessPistolToShoulder()
        {
            _transitionType = ETransitionType.PISTOL_TO_SHOULDER;
            _transitionPhase = ETransitionPhase.FIRST_FRAME;
            _changedStateWindowOpen = false;

            _animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0.8f));
            _animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0, PrimeMover.Sling2ShoulderCurve1_2.Value), new Keyframe(0.2f, PrimeMover.Sling2ShoulderCurve2_2.Value), new Keyframe(0.4f, PrimeMover.Sling2ShoulderCurve3_2.Value), new Keyframe(0.6f, PrimeMover.Sling2ShoulderCurve4_2.Value), new Keyframe(0.8f, PrimeMover.Sling2ShoulderCurve5_2.Value), new Keyframe(1, 1f));

            _processSpeedPhase1 = 1f;
            _processSpeedPhase2 = 1f;
        }
        static void ProcessPistolToSling()
        {
            _transitionType = ETransitionType.PISTOL_TO_SLING;
            _transitionPhase = ETransitionPhase.FIRST_FRAME;
            _changedStateWindowOpen = false;

            _animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0.8f));
            _animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0, PrimeMover.Sling2ShoulderCurve1_2.Value), new Keyframe(0.2f, PrimeMover.Sling2ShoulderCurve2_2.Value), new Keyframe(0.4f, PrimeMover.Sling2ShoulderCurve3_2.Value), new Keyframe(0.6f, PrimeMover.Sling2ShoulderCurve4_2.Value), new Keyframe(0.8f, PrimeMover.Sling2ShoulderCurve5_2.Value), new Keyframe(1, 1f));

            _processSpeedPhase1 = 1f;
            _processSpeedPhase2 = 1f;
        }

        static void SetAnimSpeed(Player player, float speed)
        {
            if (player == null)
            {
                return;
            }

            player.HandsAnimator.SetAnimationSpeed(speed);
        }
    }
}

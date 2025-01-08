using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT;
using UnityEngine;
using EFT.UI;
using UnityEngine.Assertions.Must;
using System;
using static TarkovIRL.AnimStateController;
using System.Reflection.Emit;

namespace TarkovIRL
{
    internal class WeaponSelectionController
    {
        public enum EWeaponSelection { SLING, SHOULDER, PISTOL, OTHER }
        static EWeaponSelection _lastSelectedWeapon;

        static Vector3 _posLerp = Vector3.zero;
        static Vector3 _orderEndPos = Vector3.zero;
        static Vector3 _presentStartPos = Vector3.zero;

        static Vector3 _rotLerp = Vector3.zero;
        static Vector3 _orderEndRot = Vector3.zero;
        static Vector3 _presentStartRot = Vector3.zero;

        static Vector3 _posLerpHistory = Vector3.zero;
        static Vector3 _rotLerpHistory = Vector3.zero;
        static Vector3 _posLerpSmoothed = Vector3.zero;
        static Vector3 _rotLerpSmoothed = Vector3.zero;

        static Player _player = null;

        static AnimationCurve _animSpeedCurvePhase1 = new AnimationCurve();
        static AnimationCurve _animSpeedCurvePhase2 = new AnimationCurve();

        static AnimationCurve _Flatcurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe (1f, 1f));

        public static void UpdateAnimationPump(float dt)
        {
            if (_player == null) return;
            if (_player.HandsAnimator == null) return;

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

            EWeaponState state = AnimStateController.WeaponState;
            float animProgress = _player.HandsAnimator.Animator.GetCurrentAnimatorStateInfo(1).normalizedTime;
            float efficiencyMod = Mathf.Clamp(EfficiencyController.EfficiencyModifierInverse, 0.5f, 1.5f);
            float proneMod = PlayerMotionController.IsProne ? 0.33f : 1f;
            float finalSpeedMulti = efficiencyMod * proneMod * WeaponController.GetWeaponMulti(true);

            if (state == EWeaponState.ORDER_ARM)
            {
                float speed = _animSpeedCurvePhase1.Evaluate(animProgress);
                SetAnimSpeed(_player, speed);
                _posLerp = Vector3.Lerp(Vector3.zero, _orderEndPos, animProgress);
                _rotLerp = Vector3.Lerp(Vector3.zero, _orderEndRot, animProgress);
                //UtilsTIRL.Log($" animProgress of {state} - {animProgress} at speed {speed}");
            }
            else if (state == EWeaponState.PRESENT_ARM)
            {
                float speed = _animSpeedCurvePhase2.Evaluate(animProgress);
                SetAnimSpeed(_player, speed);
                _posLerp = Vector3.Lerp(_presentStartPos, Vector3.zero, animProgress);
                _rotLerp = Vector3.Lerp(_presentStartRot, Vector3.zero, animProgress);
                //UtilsTIRL.Log($" animProgress of {state} - {animProgress} at speed {speed}");
            }
        }

        public static void GetWeaponSelectionTransforms(out Vector3 pos, out Quaternion rot)
        {
            pos = _posLerpSmoothed;
            rot = UtilsTIRL.GetQuatFromV3(_rotLerpSmoothed);
        }

        public static void Process(ECommand command, Player player)
        {
            _player = player;
            if (AnimStateController.WeaponState == AnimStateController.EWeaponState.UNKNOWN_STATE)
            {
                return;
            }
            //UtilsTIRL.Log($"weapon state {AnimStateController.WeaponState}");

            Item newLastWeapon = player.LastEquippedWeaponOrKnifeItem;
            Item slingWeapon = player.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem;
            Item shoulderWeapon = player.Inventory.Equipment.GetSlot(EquipmentSlot.SecondPrimaryWeapon).ContainedItem;
            Item holsterWeapon = player.Inventory.Equipment.GetSlot(EquipmentSlot.Holster).ContainedItem;

            // TODO : no case for coming from nothing -- if you have no weapon at all and pick one up,
            // null ref and you can't proceed without doing something else with your hands

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
            if (command == ECommand.SelectSecondaryWeapon && _lastSelectedWeapon == EWeaponSelection.SLING)
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
            }

            // pistol to sling
            if (command == ECommand.SelectFirstPrimaryWeapon && _lastSelectedWeapon == EWeaponSelection.PISTOL)
            {
                ProcessPistolToSling();
            }

            // transition to slot
            bool selectionFromSlot = command == ECommand.PressSlot0;
            selectionFromSlot &= command == ECommand.PressSlot4;
            selectionFromSlot &= command == ECommand.PressSlot5;
            selectionFromSlot &= command == ECommand.PressSlot6;
            selectionFromSlot &= command == ECommand.PressSlot7;
            selectionFromSlot &= command == ECommand.PressSlot8;
            selectionFromSlot &= command == ECommand.PressSlot9;
            selectionFromSlot &= command == ECommand.SelectFastSlot0;
            selectionFromSlot &= command == ECommand.SelectFastSlot4;
            selectionFromSlot &= command == ECommand.SelectFastSlot5;
            selectionFromSlot &= command == ECommand.SelectFastSlot6;
            selectionFromSlot &= command == ECommand.SelectFastSlot7;
            selectionFromSlot &= command == ECommand.SelectFastSlot8;
            selectionFromSlot &= command == ECommand.SelectFastSlot9;
            if (selectionFromSlot && _lastSelectedWeapon == EWeaponSelection.SHOULDER)
            {
                ProcessShoulderToSlot();
            }

            //UtilsTIRL.Log($"command selected : {command}");
        }

        /*
        _animSpeedCurvePhase1 = 
        _animSpeedCurvePhase2 = 

        _orderEndPos = new Vector3();
        _orderEndRot = new Vector3();

        _presentStartPos = new Vector3();
        _presentStartRot = new Vector3();
        */

        static void ProcessShoulderToSling()
        {
            _animSpeedCurvePhase1 = PrimeMover.Instance.OrderShoulderCurve;
            _animSpeedCurvePhase2 = _Flatcurve;

            _orderEndPos = new Vector3(PrimeMover.ShoulderPositionEndX.Value, PrimeMover.ShoulderPositionEndY.Value, PrimeMover.ShoulderPositionEndZ.Value);
            _orderEndRot = new Vector3(PrimeMover.ShoulderRotationEndX.Value, PrimeMover.ShoulderRotationEndY.Value, PrimeMover.ShoulderRotationEndZ.Value);

            _presentStartPos = new Vector3(PrimeMover.SlingPositionStartX.Value, PrimeMover.SlingPositionStartY.Value, PrimeMover.SlingPositionStartZ.Value);
            _presentStartRot = new Vector3(PrimeMover.SlingRotationStartX.Value, PrimeMover.SlingRotationStartY.Value, PrimeMover.SlingRotationStartZ.Value);
        }

        static void ProcessSlingToShoulder()
        {
            _animSpeedCurvePhase1 = _Flatcurve;
            _animSpeedCurvePhase2 = PrimeMover.Instance.PresentShoulderCurve;

            _orderEndPos = new Vector3(PrimeMover.SlingPositionEndX.Value, PrimeMover.SlingPositionEndY.Value, PrimeMover.SlingPositionEndZ.Value);
            _orderEndRot = new Vector3(PrimeMover.SlingRotationEndX.Value, PrimeMover.SlingRotationEndY.Value, PrimeMover.SlingRotationEndZ.Value);

            _presentStartPos = new Vector3(PrimeMover.ShoulderPositionStartX.Value, PrimeMover.ShoulderPositionStartY.Value, PrimeMover.ShoulderPositionStartZ.Value);
            _presentStartRot = new Vector3(PrimeMover.ShoulderRotationStartX.Value, PrimeMover.ShoulderRotationStartY.Value, PrimeMover.ShoulderRotationStartZ.Value);
        }

        // pistol
        static void ProcessSlingToPistol()
        {
            _animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
            _animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));

            _orderEndPos = new Vector3(PrimeMover.SlingPositionEndX.Value, PrimeMover.SlingPositionEndY.Value, PrimeMover.SlingPositionEndZ.Value);
            _orderEndRot = new Vector3(PrimeMover.SlingRotationEndX.Value, PrimeMover.SlingRotationEndY.Value, PrimeMover.SlingRotationEndZ.Value);

            _presentStartPos = new Vector3(PrimeMover.HolsterPositionStartX.Value, PrimeMover.HolsterPositionStartY.Value, PrimeMover.HolsterPositionStartZ.Value);
            _presentStartRot = new Vector3(PrimeMover.HolsterRotationStartX.Value, PrimeMover.HolsterRotationStartY.Value, PrimeMover.HolsterRotationStartZ.Value);
        }

        static void ProcessQuickSlingToPistol()
        {
            _animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
            _animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0f, 3f), new Keyframe(1f, 3f));

            _orderEndPos = new Vector3(PrimeMover.SlingPositionEndX.Value, PrimeMover.SlingPositionEndY.Value, PrimeMover.SlingPositionEndZ.Value);
            _orderEndRot = new Vector3(PrimeMover.SlingRotationEndX.Value, PrimeMover.SlingRotationEndY.Value, PrimeMover.SlingRotationEndZ.Value);

            _presentStartPos = new Vector3(PrimeMover.HolsterPositionStartX.Value, PrimeMover.HolsterPositionStartY.Value, PrimeMover.HolsterPositionStartZ.Value);
            _presentStartRot = new Vector3(PrimeMover.HolsterRotationStartX.Value, PrimeMover.HolsterRotationStartY.Value, PrimeMover.HolsterRotationStartZ.Value);
        }

        static void ProcessShoulderToPistol()
        {
            _animSpeedCurvePhase1 = PrimeMover.Instance.OrderShoulderCurve;
            _animSpeedCurvePhase2 = _Flatcurve;

            _orderEndPos = new Vector3(PrimeMover.ShoulderPositionEndX.Value, PrimeMover.ShoulderPositionEndY.Value, PrimeMover.ShoulderPositionEndZ.Value);
            _orderEndRot = new Vector3(PrimeMover.ShoulderRotationEndX.Value, PrimeMover.ShoulderRotationEndY.Value, PrimeMover.ShoulderRotationEndZ.Value);

            _presentStartPos = new Vector3(PrimeMover.HolsterPositionStartX.Value, PrimeMover.HolsterPositionStartY.Value, PrimeMover.HolsterPositionStartZ.Value);
            _presentStartRot = new Vector3(PrimeMover.HolsterRotationStartX.Value, PrimeMover.HolsterRotationStartY.Value, PrimeMover.HolsterRotationStartZ.Value);
        }
        static void ProcessPistolToShoulder()
        {
            _animSpeedCurvePhase1 = _Flatcurve;
            _animSpeedCurvePhase2 = PrimeMover.Instance.PresentShoulderCurve;

            _orderEndPos = new Vector3(PrimeMover.HolsterPositionEndX.Value, PrimeMover.HolsterPositionEndY.Value, PrimeMover.HolsterPositionEndZ.Value);
            _orderEndRot = new Vector3(PrimeMover.HolsterRotationEndX.Value, PrimeMover.HolsterRotationEndY.Value, PrimeMover.HolsterRotationEndZ.Value);

            _presentStartPos = new Vector3(PrimeMover.ShoulderPositionStartX.Value, PrimeMover.ShoulderPositionStartY.Value, PrimeMover.ShoulderPositionStartZ.Value);
            _presentStartRot = new Vector3(PrimeMover.ShoulderRotationStartX.Value, PrimeMover.ShoulderRotationStartY.Value, PrimeMover.ShoulderRotationStartZ.Value);

        }
        static void ProcessPistolToSling()
        {
            _animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0f, 1.75f), new Keyframe(1f, 1.75f));
            _animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0f, 1.75f), new Keyframe(1f, 1.75f));

            _orderEndPos = new Vector3(PrimeMover.HolsterPositionEndX.Value, PrimeMover.HolsterPositionEndY.Value, PrimeMover.HolsterPositionEndZ.Value);
            _orderEndRot = new Vector3(PrimeMover.HolsterRotationEndX.Value, PrimeMover.HolsterRotationEndY.Value, PrimeMover.HolsterRotationEndZ.Value);

            _presentStartPos = new Vector3(PrimeMover.SlingPositionStartX.Value, PrimeMover.SlingPositionStartY.Value, PrimeMover.SlingPositionStartZ.Value);
            _presentStartRot = new Vector3(PrimeMover.SlingRotationStartX.Value, PrimeMover.SlingRotationStartY.Value, PrimeMover.SlingRotationStartZ.Value);
        }
        static void ProcessShoulderToSlot()
        {
            // not completely thought out
            _animSpeedCurvePhase1 = PrimeMover.Instance.OrderShoulderCurve;
            _animSpeedCurvePhase2 = _Flatcurve;

            _orderEndPos = new Vector3(PrimeMover.ShoulderPositionEndX.Value, PrimeMover.ShoulderPositionEndY.Value, PrimeMover.ShoulderPositionEndZ.Value);
            _orderEndRot = new Vector3(PrimeMover.ShoulderRotationEndX.Value, PrimeMover.ShoulderRotationEndY.Value, PrimeMover.ShoulderRotationEndZ.Value);

            _presentStartPos = Vector3.zero;
            _presentStartRot = Vector3.zero;

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

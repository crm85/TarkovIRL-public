using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT;
using UnityEngine;
using static TarkovIRL.AnimStateController;

namespace TarkovIRL
{
    internal static class WeaponSelectionController
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

        static readonly Vector3 Pistol_Start_Pos = new Vector3(0.05f, 0.1f, -0.2f);
        static readonly Vector3 Pistol_Start_Rot = new Vector3(0.1f, -0.2f, 0.05f);
        static readonly Vector3 Pistol_End_Pos = Vector3.zero;
        static readonly Vector3 Pistol_End_Rot = Vector3.zero;

        static readonly Vector3 Shoulder_Start_Pos = new Vector3(0.27f, -0.5f, -0.12f);
        static readonly Vector3 Shoulder_Start_Rot = new Vector3(-0.4f, -0.56f, 0f);
        static readonly Vector3 Shoulder_End_Pos = new Vector3(0.25f, -0.5f, -0.19f);
        static readonly Vector3 Shoulder_End_Rot = new Vector3(-0.4f, -0.56f, 0f);

        static readonly Vector3 Sling_Start_Pos = new Vector3(0.06f, -0.16f, -0.1f);
        static readonly Vector3 Sling_Start_Rot = new Vector3(0, -0.75f, -0.24f);
        static readonly Vector3 Sling_End_Pos = new Vector3(0.37f, -0.16f, -0.46f);
        static readonly Vector3 Sling_End_Rot = new Vector3(0, 1f, -0.24f);

        static readonly float TransformSmoothingDTMulti = 12f;

        static Player _player = null;

        static AnimationCurve _animSpeedCurvePhase1 = new AnimationCurve();
        static AnimationCurve _animSpeedCurvePhase2 = new AnimationCurve();

        static readonly AnimationCurve FlatCurve = ConstantCurve(1f);
        static bool _transitionFirstFrame = false;
        static bool _transitionWindowOpen = false;
        static bool _transitionHasStarted = false;
        static bool _transitionSpeedOverrideActive = false;
        static float _transitionStartWait = 0f;
        static float _transitionInactiveWait = 0f;
        const float TransitionStartTimeout = 1f;
        const float TransitionInactiveTimeout = 0.5f;

        public static void UpdateAnimationPump(float dt)
        {
            if (!PrimeMover.IsWeaponTrans.Value)
            {
                CloseTransition(resetSpeed: true);
                return;
            }

            UpdateTransformSmoothing(dt);

            if (_transitionFirstFrame)
            {
                _transitionFirstFrame = false;
                _transitionWindowOpen = true;
            }

            if (!_transitionWindowOpen)
            {
                return;
            }

            if (!CanRunWeaponTransition(_player))
            {
                CloseTransition(resetSpeed: true);
                return;
            }

            EWeaponState state = AnimStateController.WeaponState;
            bool isTransitionState = state == EWeaponState.ORDER_ARM || state == EWeaponState.PRESENT_ARM;
            if (!_transitionHasStarted && !isTransitionState)
            {
                _transitionStartWait += dt;
                if (_transitionStartWait > TransitionStartTimeout)
                {
                    CloseTransition(resetSpeed: true);
                    return;
                }
            }

            float animProgress = _player.HandsAnimator.Animator.GetCurrentAnimatorStateInfo(1).normalizedTime;
            float efficiencyMod = Mathf.Clamp(EfficiencyController.EfficiencyModifierInverse, 0.5f, 1.5f);
            float proneMod = PlayerMotionController.IsProne ? 0.5f : 1f;
            float foldedStockMulti = WeaponController.IsStockFolded ? 1.5f : 1f;
            float finalSpeedMulti = efficiencyMod * proneMod * WeaponController.GetWeaponMulti(true) * PrimeMover.TransitionSpeedMulti.Value * foldedStockMulti;

            if (state == EWeaponState.ORDER_ARM)
            {
                _transitionHasStarted = true;
                _transitionStartWait = 0f;
                _transitionInactiveWait = 0f;
                float speed = _animSpeedCurvePhase1.Evaluate(animProgress) * finalSpeedMulti;
                SetTransitionAnimSpeed(speed);
                _posLerp = Vector3.Lerp(Vector3.zero, _orderEndPos, animProgress);
                _rotLerp = Vector3.Lerp(Vector3.zero, _orderEndRot, animProgress);
                //UtilsTIRL.Log($" animProgress of {state} - {animProgress} at speed {speed}");
            }
            else if (state == EWeaponState.PRESENT_ARM)
            {
                _transitionHasStarted = true;
                _transitionStartWait = 0f;
                _transitionInactiveWait = 0f;
                float speed = _animSpeedCurvePhase2.Evaluate(animProgress) * finalSpeedMulti;
                SetTransitionAnimSpeed(speed);
                _posLerp = Vector3.Lerp(_presentStartPos, Vector3.zero, animProgress);
                _rotLerp = Vector3.Lerp(_presentStartRot, Vector3.zero, animProgress);
                //UtilsTIRL.Log($" animProgress of {state} - {animProgress} at speed {speed}");

            }
            // cleanup transition
            if (_transitionHasStarted && state == EWeaponState.IDLE)
            {
                CloseTransition(resetSpeed: true);
            }
            else if (_transitionHasStarted && !isTransitionState)
            {
                _transitionInactiveWait += dt;
                if (_transitionInactiveWait > TransitionInactiveTimeout)
                {
                    CloseTransition(resetSpeed: true);
                }
            }
            //UtilsTIRL.Log($"transition open at state {state}/{AnimStateController.WeaponStateHash} - {animProgress}");

        }

        static void UpdateTransformSmoothing(float dt)
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

            _posLerpSmoothed.x = _posLerpHistory.x * dt * TransformSmoothingDTMulti;
            _posLerpSmoothed.y = _posLerpHistory.y * dt * TransformSmoothingDTMulti;
            _posLerpSmoothed.z = _posLerpHistory.z * dt * TransformSmoothingDTMulti;

            _rotLerpSmoothed.x = _rotLerpHistory.x * dt * TransformSmoothingDTMulti;
            _rotLerpSmoothed.y = _rotLerpHistory.y * dt * TransformSmoothingDTMulti;
            _rotLerpSmoothed.z = _rotLerpHistory.z * dt * TransformSmoothingDTMulti;
        }

        public static void GetWeaponSelectionTransforms(out Vector3 pos, out Quaternion rot)
        {
            pos = _posLerpSmoothed;
            rot = TIRLUtils.GetQuatFromV3(_rotLerpSmoothed);
        }

        public static void Process(ECommand command, Player player)
        {
            if (player == null)
            {
                return;
            }

            _player = player;
            if (!CanRunWeaponTransition(player))
            {
                CloseTransition(resetSpeed: true);
                return;
            }

            if (AnimStateController.WeaponState == AnimStateController.EWeaponState.UNKNOWN_STATE)
            {
                return;
            }
            //UtilsTIRL.Log($"weapon state {AnimStateController.WeaponState}");
            //TIRLUtils.Log($"selection processing", true);

            if (!TryGetLastWeapon(player, out Item newLastWeapon))
            {
                return;
            }

            Item slingWeapon = player.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem;
            Item shoulderWeapon = player.Inventory.Equipment.GetSlot(EquipmentSlot.SecondPrimaryWeapon).ContainedItem;
            Item holsterWeapon = player.Inventory.Equipment.GetSlot(EquipmentSlot.Holster).ContainedItem;

            if (!TryRememberLastSelectedWeapon(command, newLastWeapon, slingWeapon, shoulderWeapon, holsterWeapon))
            {
                return;
            }

            if (!CommandHasTargetWeapon(command, slingWeapon, shoulderWeapon, holsterWeapon))
            {
                return;
            }

            if (TryConfigureTransition(command))
            {
                StartTransition();
            }
        }

        static bool TryGetLastWeapon(Player player, out Item lastWeapon)
        {
            lastWeapon = player.LastEquippedWeaponOrKnifeItem;
            if (lastWeapon != null)
            {
                return true;
            }

            player.TrySetLastEquippedWeapon(true);
            lastWeapon = player.LastEquippedWeaponOrKnifeItem;
            return lastWeapon != null;
        }

        static bool TryRememberLastSelectedWeapon(ECommand command, Item lastWeapon, Item slingWeapon, Item shoulderWeapon, Item holsterWeapon)
        {
            if (slingWeapon != null && lastWeapon.Equals(slingWeapon))
            {
                if (command == ECommand.SelectFirstPrimaryWeapon)
                {
                    return false;
                }

                _lastSelectedWeapon = EWeaponSelection.SLING;
                return true;
            }

            if (shoulderWeapon != null && lastWeapon.Equals(shoulderWeapon))
            {
                if (command == ECommand.SelectSecondPrimaryWeapon)
                {
                    return false;
                }

                _lastSelectedWeapon = EWeaponSelection.SHOULDER;
                return true;
            }

            if (holsterWeapon != null && lastWeapon.Equals(holsterWeapon))
            {
                if (command == ECommand.SelectSecondaryWeapon)
                {
                    return false;
                }

                if (command == ECommand.QuickSelectSecondaryWeapon && slingWeapon == null)
                {
                    return false;
                }

                _lastSelectedWeapon = EWeaponSelection.PISTOL;
            }

            return true;
        }

        static bool CommandHasTargetWeapon(ECommand command, Item slingWeapon, Item shoulderWeapon, Item holsterWeapon)
        {
            if (command == ECommand.SelectFirstPrimaryWeapon)
            {
                return slingWeapon != null;
            }

            if (command == ECommand.SelectSecondPrimaryWeapon)
            {
                return shoulderWeapon != null;
            }

            if (command == ECommand.SelectSecondaryWeapon)
            {
                return holsterWeapon != null;
            }

            return true;
        }

        static bool TryConfigureTransition(ECommand command)
        {
            if (command == ECommand.SelectFirstPrimaryWeapon && _lastSelectedWeapon == EWeaponSelection.SHOULDER)
            {
                ProcessShoulderToSling();
                return true;
            }

            if (command == ECommand.SelectSecondPrimaryWeapon && _lastSelectedWeapon == EWeaponSelection.SLING)
            {
                ProcessSlingToShoulder();
                return true;
            }

            if (command == ECommand.SelectSecondaryWeapon && _lastSelectedWeapon == EWeaponSelection.SLING)
            {
                ProcessSlingToPistol();
                return true;
            }

            if (command == ECommand.QuickSelectSecondaryWeapon && _lastSelectedWeapon == EWeaponSelection.SLING)
            {
                ProcessQuickSlingToPistol();
                return true;
            }

            if ((command == ECommand.SelectSecondaryWeapon || command == ECommand.QuickSelectSecondaryWeapon) && _lastSelectedWeapon == EWeaponSelection.SHOULDER)
            {
                ProcessShoulderToPistol();
                return true;
            }

            if (command == ECommand.SelectSecondPrimaryWeapon && _lastSelectedWeapon == EWeaponSelection.PISTOL)
            {
                ProcessPistolToShoulder();
                return true;
            }

            if (command == ECommand.SelectFirstPrimaryWeapon && _lastSelectedWeapon == EWeaponSelection.PISTOL)
            {
                ProcessPistolToSling();
                return true;
            }

            return false;
        }

        static void StartTransition()
        {
            _transitionFirstFrame = true;
            _transitionWindowOpen = false;
            _transitionHasStarted = false;
            _transitionStartWait = 0f;
            _transitionInactiveWait = 0f;
        }

        static void CloseTransition(bool resetSpeed)
        {
            if (resetSpeed)
            {
                ResetTransitionAnimSpeed();
            }

            _transitionFirstFrame = false;
            _transitionWindowOpen = false;
            _transitionHasStarted = false;
            _transitionStartWait = 0f;
            _transitionInactiveWait = 0f;
            _posLerp = Vector3.zero;
            _rotLerp = Vector3.zero;
        }

        static void ProcessShoulderToSling()
        {
            ConfigureTransition(PrimeMover.Instance.OrderShoulderCurve, FlatCurve, Shoulder_End_Pos, Shoulder_End_Rot, Sling_Start_Pos, Sling_Start_Rot);
        }

        static void ProcessSlingToShoulder()
        {
            ConfigureTransition(FlatCurve, PrimeMover.Instance.PresentShoulderCurve, Sling_End_Pos, Sling_End_Rot, Shoulder_Start_Pos, Shoulder_Start_Rot);
        }

        static void ProcessSlingToPistol()
        {
            ConfigureTransition(ConstantCurve(1f), ConstantCurve(0.3f), Sling_End_Pos, Sling_End_Rot, Pistol_Start_Pos, Pistol_Start_Rot);
        }

        static void ProcessQuickSlingToPistol()
        {
            ConfigureTransition(SpeedCurve(1.25f, 0.75f), SpeedCurve(1.2f, 2f), Sling_End_Pos, Sling_End_Rot, Pistol_Start_Pos, Pistol_Start_Rot);
        }

        static void ProcessShoulderToPistol()
        {
            ConfigureTransition(PrimeMover.Instance.OrderShoulderCurve, ConstantCurve(0.3f), Shoulder_End_Pos, Shoulder_End_Rot, Pistol_Start_Pos, Pistol_Start_Rot);
        }

        static void ProcessPistolToShoulder()
        {
            ConfigureTransition(ConstantCurve(0.25f), PrimeMover.Instance.PresentShoulderCurve, Pistol_End_Pos, Pistol_End_Rot, Shoulder_Start_Pos, Shoulder_Start_Rot);
        }

        static void ProcessPistolToSling()
        {
            ConfigureTransition(SpeedCurve(0.35f, 0.25f), ConstantCurve(1.75f), Pistol_End_Pos, Pistol_End_Rot, Sling_Start_Pos, Sling_Start_Rot);
        }

        static void ConfigureTransition(AnimationCurve phase1, AnimationCurve phase2, Vector3 orderEndPos, Vector3 orderEndRot, Vector3 presentStartPos, Vector3 presentStartRot)
        {
            _animSpeedCurvePhase1 = phase1;
            _animSpeedCurvePhase2 = phase2;

            _orderEndPos = orderEndPos;
            _orderEndRot = orderEndRot;

            _presentStartPos = presentStartPos;
            _presentStartRot = presentStartRot;
        }

        static AnimationCurve ConstantCurve(float value)
        {
            return SpeedCurve(value, value);
        }

        static AnimationCurve SpeedCurve(float start, float end)
        {
            return new AnimationCurve(new Keyframe(0f, start), new Keyframe(1f, end));
        }

        static bool CanRunWeaponTransition(Player player)
        {
            if (player == null)
            {
                return false;
            }

            if (player.MovementContext != null && player.MovementContext.StationaryWeapon != null)
            {
                return false;
            }

            return player.HasFirearmInHands();
        }

        static void SetTransitionAnimSpeed(float speed)
        {
            SetAnimSpeed(_player, speed);
            _transitionSpeedOverrideActive = true;
        }

        static void ResetTransitionAnimSpeed()
        {
            if (!_transitionSpeedOverrideActive)
            {
                return;
            }

            SetAnimSpeed(_player, 1f);
            _transitionSpeedOverrideActive = false;
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

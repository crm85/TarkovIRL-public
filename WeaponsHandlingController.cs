using UnityEngine;

namespace TarkovIRL
{
    public static class WeaponsHandlingController
    {
        // public vars

        public static float DeltaTime = 0;
        public static float Time = 0;
        public static float CurrentWeaponErgo = 0;
        public static float CurrentWeaponWeight = 0;
        public static float CurrentWeaponLength = 0;

        public static bool IsSwayUpdatedThisFrame = false;
        public static bool IsReposStance = false;
        public static bool IsStocked = false;
        

        // private vars

        static Vector2 _playerRotLastFrame = Vector2.zero;
        static Vector3 _playerPosLastFrame = Vector3.zero;

        static float _playerRotationHistory = 0;
        static float _playerRotationAvg = 0;

        static bool _playerMoving = false;
        static float _verticalAvg = 0;

        public static bool SwayThisFrame = false;

        static public float ProcessHeadDelta(float rawHeadDelta)
        {
            float adjustedHeadDelta = rawHeadDelta / CurrentWeaponErgo / 10f;
            return adjustedHeadDelta * WeaponsHandlingController.CurrentWeaponWeight * 0.1f;
        }

        public static void UpdateRotationHistory(Vector2 newRot)
        {
            // vertical 
            _verticalAvg = newRot.y > _playerRotLastFrame.y ? 1f : -1f;

            // horizontal
            float distance = Vector2.Distance(newRot.normalized, _playerRotLastFrame.normalized);

            _playerRotationHistory += distance;
            _playerRotationHistory -= _playerRotationAvg;
            _playerRotationHistory = Mathf.Clamp(_playerRotationHistory, 0, PrimeMover.RotHistoryPoolClamp.Value);
            _playerRotationAvg = _playerRotationHistory * DeltaTime;

            // set for next frame
            _playerRotLastFrame = newRot;
        }

        public static void UpdateTransformHistory(Vector3 position)
        {
            float dist = Vector3.Distance(position.normalized, _playerPosLastFrame.normalized);
            if (dist > 0)
            {
                _playerMoving = true;
            }
            else
            {
                _playerMoving = false;
            }
            _playerPosLastFrame = position;
        }

        public static float RotationDelta
        {
            get { return _playerRotationAvg; }
        }

        public static bool IsPlayerMovement
        {
            get { return _playerMoving; }
        }

        public static float VerticalTrend
        {
            get { return _verticalAvg; }
        }

        public static float GetGeneralEfficiencyModifier(EFT.Player player)
        {
            float healthCommon = player.HealthController.GetBodyPartHealth(EBodyPart.Common).Normalized;
            float armHealthR = player.HealthController.GetBodyPartHealth(EBodyPart.RightArm).Normalized;
            float armHealthL = player.HealthController.GetBodyPartHealth(EBodyPart.LeftArm).Normalized;
            float stamNormalized = player.Physical.Stamina.Current / 104f;
            float handStamNormalized = player.Physical.HandsStamina.Current / 80f;
            float strength = player.Skills.Strength.Current;
            float currentWeight = player.Physical.PreviousWeight;

            float healthMulti = 1f + ((1f - healthCommon) * .2f);
            float armHealthRMulti = 1f + ((1f - armHealthR) * .2f);
            float armHealthLMulti = 1f + ((1f - armHealthL) * .2f);
            float stamMulti = 1f + ((1f - stamNormalized) * .1f);
            float handStamMulti = 1f + ((1f - handStamNormalized) * .1f);
            float underweightReduction = Mathf.Clamp01(currentWeight / (strength * .034f));
            float strengthMulti = 1f - (strength / 15000);

            float generalEfficiency = strengthMulti * underweightReduction * healthMulti * armHealthRMulti * armHealthLMulti * stamMulti * handStamMulti;
            return generalEfficiency;
        }

        public static float GetSpeedModifier(EFT.Player player)
        {
            float speedMulti = player.Speed / .6f;
            if (!WeaponsHandlingController.IsPlayerMovement)
            {
                speedMulti = 0;
            }
            speedMulti = Mathf.Clamp(speedMulti, .25f, 1f);
            return speedMulti;
        }
    }
}

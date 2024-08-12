using UnityEngine;
using EFT;

namespace TarkovIRL
{
    public static class WeaponsHandlingController
    {
        // public vars

        public static float CurrentWeaponErgo = 0;
        public static float CurrentWeaponWeight = 0;

        public static bool IsSwayUpdatedThisFrame = false;
        public static bool IsStocked = false;
        public static bool SwayThisFrame = false;
        
        public static void UpdateWpnStats(Player.FirearmController fc)
        {
            if (fc != null)
            {
                CurrentWeaponWeight = fc.Weapon.GetSingleItemTotalWeight();
                CurrentWeaponErgo = fc.TotalErgonomics / 100f;
                bool isFolded = fc.Weapon.GetFoldable() != null && fc.Weapon.Folded;
                bool isPistol = fc.Weapon.WeapClass == "pistol";
                IsStocked = isFolded || isPistol;
            }
            else
            {
                CurrentWeaponWeight = 0;
                CurrentWeaponErgo = 1f;
            }
        }

        public static float GetSwayModifier(Player player)
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

        public static float GetEfficiencyNormalized(Player player, ref float healthCommonNorm, ref float armHealthNormR, ref float armHealthNormL, ref float stamNorm, ref float handStamNorm, ref float strengthNorm)
        {
            healthCommonNorm = player.HealthController.GetBodyPartHealth(EBodyPart.Common).Normalized;
            armHealthNormR = player.HealthController.GetBodyPartHealth(EBodyPart.RightArm).Normalized;
            armHealthNormL = player.HealthController.GetBodyPartHealth(EBodyPart.LeftArm).Normalized;
            stamNorm = player.Physical.Stamina.Current / 104f;
            handStamNorm = player.Physical.HandsStamina.Current / 80f;
            strengthNorm = player.Skills.Strength.Current / 15000f;
            return healthCommonNorm * armHealthNormR * armHealthNormL * stamNorm * handStamNorm * strengthNorm;
        }
    }
}

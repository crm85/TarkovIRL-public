using UnityEngine;
using EFT;

namespace TarkovIRL
{
    public static class WeaponController
    {
        public enum E_CALIBER { UNKNOWN, R_MINI, R_LOW, R_MID, R_HIGH, P_ } ;
        public static E_CALIBER Caliber;

        // public vars

        public static float CurrentWeaponErgoNorm = 0;
        public static float CurrentWeaponWeight = 0;

        public static bool IsSwayUpdatedThisFrame = false;
        public static bool IsStocked = false;
        public static bool IsPistol = false;
        public static bool SwayThisFrame = false;
        
        public static void UpdateWpnStats(Player.FirearmController fc)
        {
            if (fc != null)
            {
                CurrentWeaponWeight = fc.Weapon.GetSingleItemTotalWeight();
                CurrentWeaponErgoNorm = fc.TotalErgonomics / 100f;
                IsStocked = CheckForStock(fc.Weapon);
                ProcessCaliber(fc.Weapon.AmmoCaliber);
            }
            else
            {
                CurrentWeaponWeight = 0;
                CurrentWeaponErgoNorm = 1f;
            }
        }

        static bool CheckForStock(EFT.InventoryLogic.Weapon weapon)
        {
            bool isPistol = weapon.WeapClass == "pistol";
            IsPistol = isPistol;
            if (isPistol)
            {
                return false;
            }
            else
            {
                if (weapon.GetFoldable() != null)
                {
                    if (weapon.Folded)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static float GetEfficiencyModifier(Player player)
        {
            float healthCommon = player.HealthController.GetBodyPartHealth(EBodyPart.Common).Normalized;
            float armHealthR = player.HealthController.GetBodyPartHealth(EBodyPart.RightArm).Normalized;
            float armHealthL = player.HealthController.GetBodyPartHealth(EBodyPart.LeftArm).Normalized;
            float stamNormalized = player.Physical.Stamina.Current / 104f;
            float handStamNormalized = player.Physical.HandsStamina.Current / 80f;
            //float strength = player.Skills.Strength.Current;
            float strength = PrimeMover.DevTestFloat1.Value;
            // i want this to cap out at 350, that removes 25% effect

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

        static void ProcessCaliber(string cal)
        {
            if (cal == "556x45NATO")
            {
                //Caliber = E_CALIBER.R_556_545;
            }
            if (cal == "545x39")
            {
                //Caliber = E_CALIBER.R_556_545;
            }

            // unknown case
            else
            {
                Caliber = E_CALIBER.UNKNOWN;
            }
        }
    }
}

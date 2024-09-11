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

        public static float GetWeaponMulti()
        {
            float ergoAdjusted = PrimeMover.Instance.ErgoAttenuationCurve.Evaluate(CurrentWeaponErgoNorm);
            float weaponMulti = WeaponController.CurrentWeaponWeight * (1f - ergoAdjusted);
            return weaponMulti;
        }
    }
}

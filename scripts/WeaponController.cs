﻿using UnityEngine;
using EFT;

namespace TarkovIRL
{
    public static class WeaponController
    {
        // public vars

        public static float CurrentWeaponErgoNorm = 0;
        public static float CurrentWeaponWeight = 0;

        public static bool IsStocked = false;
        public static bool IsPistol = false;
        public static bool SwayThisFrame = false;
        
        public static void UpdateWpnStats(Player.FirearmController fc)
        {
            if (fc != null)
            {
                CurrentWeaponWeight = PrimeMover.Instance.WeightAttenuationCurve.Evaluate(fc.Weapon.GetSingleItemTotalWeight());
                CurrentWeaponErgoNorm = PrimeMover.Instance.ErgoAttenuationCurve.Evaluate(fc.TotalErgonomics / 100f);
                IsStocked = CheckForStock(fc.Weapon);
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

        public static float GetWeaponMulti(bool getInverse)
        {
            float weaponMulti = CurrentWeaponWeight * (1f - CurrentWeaponErgoNorm);
            if (getInverse) return 1f / weaponMulti;
            return weaponMulti;
        }
    }
}

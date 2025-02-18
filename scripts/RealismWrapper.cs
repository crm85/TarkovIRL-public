using EFT;
using RealismMod;
using UnityEngine;

namespace TarkovIRL
{
    internal class RealismWrapper
    {
        static RealismHealthController _realHealth = null;

        public static float GetRealismReloadSpeed()
        {
            float reloadSpeed = Mathf.Clamp(WeaponStats.CurrentMagReloadSpeed * PlayerValues.ReloadInjuryMulti * PlayerValues.ReloadSkillMulti * PlayerValues.GearReloadMulti * StanceController.HighReadyManipBuff * StanceController.ActiveAimManipBuff * Plugin.RealHealthController.AdrenalineReloadBonus * Mathf.Max(PlayerValues.RemainingArmStamFactor, 0.8f), 0.65f, 1.35f);
            return reloadSpeed;
        }

        public static float GetRealismCheckMagSpeed()
        {
            float value = PluginConfig.GlobalCheckAmmoMulti.Value;
            if (WeaponStats._WeapClass == "pistol")
            {
                value = PluginConfig.GlobalCheckAmmoPistolSpeedMulti.Value;
            }
            float animationSpeed = Mathf.Clamp(WeaponStats.CurrentMagReloadSpeed * PlayerValues.ReloadSkillMulti * PlayerValues.ReloadInjuryMulti * StanceController.HighReadyManipBuff * PlayerValues.RemainingArmStamReloadFactor * Plugin.RealHealthController.AdrenalineReloadBonus * value, 0.7f, 1.35f);
            return animationSpeed;
        }

        public static bool IsShoulderContact()
        {
            return WeaponStats.HasShoulderContact;
        }

        public static bool IsAdrenaline
        { 
            get
            {
                if (_realHealth == null)
                {
                    _realHealth = Plugin.RealHealthController;
                }
                if (_realHealth != null)
                {
                    return _realHealth.HasPositiveAdrenalineEffect || _realHealth.HasNegativeAdrenalineEffect;
                }
                else
                {
                    // do some error here
                    return false;
                }
            } 
        }

        public static float WeaponBalanceMulti
        {
            get
            {
                float weaponBalanceMulti = 1f + (Mathf.Pow(WeaponStats.Balance, 2f) * 0.001f);
                return weaponBalanceMulti;
            }
        }

        public static bool IsTacSprint 
        {
            get { return StanceController.IsDoingTacSprint; }
        }

        public static bool IsOverdose
        {
            get { return false; }
        }
    }
}
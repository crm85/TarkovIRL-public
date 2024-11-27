using EFT;
using RealismMod;
using UnityEngine;
using static RootMotion.FinalIK.GenericPoser;

namespace TarkovIRL
{
    internal class RealismWrapper
    {
        static RealismHealthController _realHealth = null;

        public static float GetRealismReloadSpeed()
        {
            float reloadSpeed = Mathf.Clamp(WeaponStats.CurrentMagReloadSpeed * PlayerState.ReloadInjuryMulti * PlayerState.ReloadSkillMulti * PlayerState.GearReloadMulti * StanceController.HighReadyManipBuff * StanceController.ActiveAimManipBuff * Plugin.RealHealthController.AdrenalineReloadBonus * Mathf.Max(PlayerState.RemainingArmStamFactor, 0.8f), 0.65f, 1.35f);
            return reloadSpeed;
        }

        public static float GetRealismCheckMagSpeed()
        {
            float value = PluginConfig.GlobalCheckAmmoMulti.Value;
            if (WeaponStats._WeapClass == "pistol")
            {
                value = PluginConfig.GlobalCheckAmmoPistolSpeedMulti.Value;
            }
            float animationSpeed = Mathf.Clamp(WeaponStats.CurrentMagReloadSpeed * PlayerState.ReloadSkillMulti * PlayerState.ReloadInjuryMulti * StanceController.HighReadyManipBuff * PlayerState.RemainingArmStamReloadFactor * Plugin.RealHealthController.AdrenalineReloadBonus * value, 0.7f, 1.35f);
            return animationSpeed;
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
                    return _realHealth.HasAdrenalineEffect;
                }
                else
                {
                    // do some error here
                    return false;
                }
            } 
        }
    }
}
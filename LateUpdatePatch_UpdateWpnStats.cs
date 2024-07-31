using System.Collections.Generic;
using System.Reflection;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using TarkovIRL;
using UnityEngine;

public class LateUpdatePatch_UpdateWpnStats : ModulePatch
{
    // readyonlys
    static readonly float _UpdateStatsTime = 1f;
    
    // vars
    static float _updateWeightTimer = 0;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.Public);
    }

    [PatchPostfix]
	private static void PatchPostfix(Player __instance)
    {
        _updateWeightTimer += PrimeMover.Instance.DeltaTime;
        if (!__instance.IsYourPlayer)
        {
            return;
        }
        Player.FirearmController fc = __instance.HandsController as Player.FirearmController;
        if (_updateWeightTimer > _UpdateStatsTime)
        {
            if (fc != null)
            {
                WeaponsHandlingController.CurrentWeaponWeight = fc.Weapon.GetSingleItemTotalWeight();
                WeaponsHandlingController.CurrentWeaponErgo = fc.TotalErgonomics / 100f;
            }
            else
            {
                WeaponsHandlingController.CurrentWeaponWeight = 0;
                WeaponsHandlingController.CurrentWeaponErgo = 1f;
            }
            _updateWeightTimer = 0;
        }
        WeaponsHandlingController.SwayThisFrame = false;
    }
}
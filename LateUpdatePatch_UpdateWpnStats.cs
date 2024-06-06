using System.Collections.Generic;
using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using TarkovIRL;
using UnityEngine;

public class LateUpdatePatch_UpdateWpnStats : ModulePatch
{
    static float _updateWeightTimer = 0;
    static readonly float _updateWeightTime = 1f;
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.Public);
    }

    [PatchPostfix]
	private static void PatchPostfix(Player __instance)
    {
        _updateWeightTimer += Time.deltaTime;
        if (!__instance.IsYourPlayer)
        {
            return;
        }
        Player.FirearmController fc = __instance.HandsController as Player.FirearmController;
        if (_updateWeightTimer > _updateWeightTime)
        {
            if (fc != null)
            {
                WeaponHandlingController.TotalWeaponWeight = fc.Weapon.GetSingleItemTotalWeight();
                WeaponHandlingController.TargetErgo = fc.TotalErgonomics / 100f;
            }
            else
            {
                WeaponHandlingController.TotalWeaponWeight = 0;
                WeaponHandlingController.TargetErgo = 1f;

            }
            _updateWeightTimer = 0;
        }

    }
}
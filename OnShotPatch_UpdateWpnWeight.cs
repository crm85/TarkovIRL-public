using Aki.Reflection.Patching;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static EFT.Player;

namespace TarkovIRL
{
    public class OnShotPatch_UpdateWpnWeight : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnMakingShot", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (!__instance.IsYourPlayer)
            {
                return;
            }

            FirearmController fc = __instance.HandsController as FirearmController;
            WeaponHandlingController.TotalWeaponWeight = fc.Weapon.GetSingleItemTotalWeight();
        }
    }
}

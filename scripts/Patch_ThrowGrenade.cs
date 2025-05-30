using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static EFT.Player;

namespace TarkovIRL
{
    public class Patch_ThrowGrenade : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(PlayerInventoryController), "player_0");
            return typeof(PlayerInventoryController).GetMethod("ThrowItem", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(PlayerInventoryController __instance, bool downDirection)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if ((UnityEngine.Object)(object)__instance != (UnityEngine.Object)null && player.IsYourPlayer)
            {
                ThrowController.NewThrow(downDirection);
            }
        }
    }
}

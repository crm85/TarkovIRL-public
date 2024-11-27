using BepInEx.Configuration;
using EFT.InputSystem;
using EFT;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using static EFT.Player;

namespace TarkovIRL
{
    internal class Patch_TranslateCommand : ModulePatch
    {
        static FieldInfo _playerField;
        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(Class1475), "player_0");
            return typeof(Class1475).GetMethod("TranslateCommand", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Class1475 __instance, ECommand command)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player == null)
            {
                return;
            }

            if (command == ECommand.ReloadWeapon)
            {
                //UtilsTIRL.Log("reload called");
                AugmentedReloadController.ToggleAugmentedMode();
            }

            if (command == ECommand.SelectFirstPrimaryWeapon || command == ECommand.SelectSecondPrimaryWeapon || command == ECommand.QuickSelectSecondaryWeapon || command == ECommand.SelectSecondaryWeapon)
            {
                //player.QuickdrawWeaponFast = false;
                WeaponSelectionController.Process(command, player);
            }

            if (command == ECommand.ToggleBreathing)
            {
                PlayerMotionController.IsHoldingBreath = true;
            }
            if (command == ECommand.EndBreathing)
            {
                PlayerMotionController.IsHoldingBreath = false;
            }
        }
    }
}

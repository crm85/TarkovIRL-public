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

namespace TarkovIRL
{
    internal class Patch_TranslateCommand : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Class1475).GetMethod("TranslateCommand", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Class1475 __instance, ECommand command)
        {
            if (command == ECommand.ReloadWeapon)
            {
                //UtilsTIRL.Log("reload called");
                AugmentedReloadController.ToggleAugmentedMode();
            }
            if (command == ECommand.CheckAmmo)
            {
                //UtilsTIRL.Log("ammo check called");
            }
            if (command == ECommand.SelectSecondPrimaryWeapon)
            {
                //UtilsTIRL.Log("shoulder weapon selected");
            }
        }
    }
}

using SPT.Reflection.Patching;

namespace TarkovIRL
{
    internal static class PatchRegistry
    {
        public static void EnableCorePatches()
        {
            Enable(new Patch_LerpCamera_ForceUpdateSway());
            Enable(new Patch_UpdateSwayFactors());
            Enable(new Patch_LateUpdate_UpdateWpnStats());
            Enable(new Patch_OnShot());
            Enable(new Patch_CalculateCameraPosition_HandLayers());
            Enable(new Patch_ShiftWeaponRoot_RunPose());
            Enable(new Patch_PlayStepSound());
            Enable(new Patch_TranslateCommand());
            Enable(new StaminaRegenRatePatch());
            Enable(new Patch_ReloadMag());
            Enable(new Patch_QuickReloadMag());
            Enable(new Patch_CheckAmmo());
            Enable(new Patch_SetHeadRotation());
            Enable(new Patch_Look());
        }

        static void Enable(ModulePatch patch)
        {
            patch.Enable();
        }
    }
}

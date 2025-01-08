using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using RealismMod;
using SPT.Reflection.Patching;
using System.Reflection;
using static EFT.Player;
using static EFT.Player.FirearmController;
using static EFT.SkillManager;

namespace TarkovIRL
{
    internal class Patch_SetAnimatorAndProceduralValues : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("SetAnimatorAndProceduralValues", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Player.FirearmController __instance)
        {/*
            Player player = (Player)playerField.GetValue(__instance);
            float num = GClass1485.PastTime - player.QuickdrawTime;
            if (player.Inventory.Equipment.GetSlot(EquipmentSlot.Holster).Equals(Item.Parent.Container) && player.QuickdrawWeaponFast && num < 1f)
            {
                float fastWeaponSwitchStaminaLack = player.Physical.FastWeaponSwitchStaminaLack;
                player.Skills.WeaponSkills.TryGetValue(Item.GetType(), out var value);
                player.Physical.OnWeaponSwitchFast(value?.Level ?? 0);
                float lackOfStamina = fastWeaponSwitchStaminaLack + _player.Physical.FastWeaponSwitchStaminaLack;
                float fullStaminaCost = Singleton<BackendConfigSettingsClass>.Instance.Stamina.WeaponFastSwitchConsumption * 2f;
                if (player.Physical.HandsStamina.Current <= 0f)
                {
                    player.ProceduralWeaponAnimation.StartHandShake(lackOfStamina, fullStaminaCost);
                }
                firearmsAnimator_0.SetSpeedParameters(1f, GetWeaponDrawSpeedMultiplier(Item, useFastDropAnimationSpeed: false));
                player.QuickdrawWeaponFast = false;
            }
            else
            {
                player.QuickdrawWeaponFast = false;
                if (player.MovementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged) || _player.MovementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged))
                {
                    firearmsAnimator_0.SetSpeedParameters();
                    player.MovementContext.PlayerAnimator.method_0();
                }
                else
                {
                    firearmsAnimator_0.SetSpeedParameters(gclass1783_0.ReloadSpeed, gclass1783_0.SwapSpeed);
                    player.MovementContext.PlayerAnimator.method_0(gclass1783_0.ReloadSpeed, gclass1783_0.SwapSpeed);
                }
            }
            */
            return false;
        }

        /*
        
        public void SetAnimatorAndProceduralValues()
		{
			float num = GClass1485.PastTime - _player.QuickdrawTime;
			if (_player.Inventory.Equipment.GetSlot(EquipmentSlot.Holster).Equals(Item.Parent.Container) && _player.QuickdrawWeaponFast && num < 1f)
			{
				float fastWeaponSwitchStaminaLack = _player.Physical.FastWeaponSwitchStaminaLack;
				_player.Skills.WeaponSkills.TryGetValue(Item.GetType(), out var value);
				_player.Physical.OnWeaponSwitchFast(value?.Level ?? 0);
				float lackOfStamina = fastWeaponSwitchStaminaLack + _player.Physical.FastWeaponSwitchStaminaLack;
				float fullStaminaCost = Singleton<BackendConfigSettingsClass>.Instance.Stamina.WeaponFastSwitchConsumption * 2f;
				if (_player.Physical.HandsStamina.Current <= 0f)
				{
					_player.ProceduralWeaponAnimation.StartHandShake(lackOfStamina, fullStaminaCost);
				}
				firearmsAnimator_0.SetSpeedParameters(1f, GetWeaponDrawSpeedMultiplier(Item, useFastDropAnimationSpeed: false));
				_player.QuickdrawWeaponFast = false;
			}
			else
			{
				_player.QuickdrawWeaponFast = false;
				if (_player.MovementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged) || _player.MovementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged))
				{
					firearmsAnimator_0.SetSpeedParameters();
					_player.MovementContext.PlayerAnimator.method_0();
				}
				else
				{
					firearmsAnimator_0.SetSpeedParameters(gclass1783_0.ReloadSpeed, gclass1783_0.SwapSpeed);
					_player.MovementContext.PlayerAnimator.method_0(gclass1783_0.ReloadSpeed, gclass1783_0.SwapSpeed);
				}
			}
		}

		*/
    }
}
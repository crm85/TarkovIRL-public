using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TarkovIRL
{
    public static class HandParallaxController
    {
        public static Vector3 GetModifiedHandPosParallax()
        {


            return Vector3.zero;
        }

        public static Vector3 GetModifiedHandRotParallax()
        {



            return Vector3.zero;
        }

        /*
        
        / testing
        Vector2 rotationalMotionThisFrame = _playerRotationLastFrame - player.Rotation;
        _playerRotationLastFrame = player.Rotation;

        _rotAvgXSet += rotationalMotionThisFrame.x;
        _rotAvgYSet += rotationalMotionThisFrame.y;
        _rotAvgXSet -= _rotAvgX;
        _rotAvgYSet -= _rotAvgY;
        _rotAvgXSet = Mathf.Clamp(_rotAvgXSet, -PrimeMover.DevTestFloat1.Value, PrimeMover.DevTestFloat1.Value);
        _rotAvgYSet = Mathf.Clamp(_rotAvgYSet, -PrimeMover.DevTestFloat1.Value, PrimeMover.DevTestFloat1.Value);
        _rotAvgX = _rotAvgXSet * player.DeltaTime;
        _rotAvgY = _rotAvgYSet * player.DeltaTime;

        bool isPistol;
        _finalParallaxMulti = Mathf.Lerp(_finalParallaxMulti, player.ProceduralWeaponAnimation.IsAiming && WeaponsHandlingController.IsStockedWeapon(firearmController.Weapon, out isPistol) ? 0.2f : 1f, Time.deltaTime);

        float lerpRate = player.DeltaTime * PrimeMover.DevTestFloat3.Value;
        float lerpRate2 = player.DeltaTime * PrimeMover.DevTestFloat5.Value;


        float lerpRate = player.DeltaTime * 2f;
        _rotLerpX = Mathf.Lerp(_rotLerpX, _rotAvgX, lerpRate * PrimeMover.DevTestFloat3.Value);
        _rotLerpX = Mathf.Lerp(_rotLerpX, _rotAvgX, lerpRate);
        _rotLerpX = Mathf.Lerp(_rotLerpX, 0, lerpRate2);
        //_rotLerpY = Mathf.Lerp(_rotLerpY, _rotAvgY, lerpRate);
        __instance.HandsContainer.WeaponRoot.localPosition = __instance.HandsContainer.WeaponRoot.localPosition + new Vector3(_rotLerpX * PrimeMover.DevTestFloat4.Value, 0, 0);
        __instance.HandsContainer.WeaponRoot.localPosition = __instance.HandsContainer.WeaponRoot.localPosition + new Vector3(_rotLerpX * PrimeMover.DevTestFloat4.Value * _finalParallaxMulti, 0, 0);
        // this works ^ 


        _rotLerpXforRot = Mathf.Lerp(_rotLerpXforRot, _rotAvgX, lerpRate * PrimeMover.DevTestFloat.Value);
        _rotLerpXforRot = Mathf.Lerp(_rotLerpXforRot, 0, lerpRate2);
        //__instance.HandsContainer.Weapon.localRotation = __instance.HandsContainer.Weapon.localRotation * Quaternion.Euler(0, PrimeMover.DevTestFloat1.Value, 0);
        __instance.HandsContainer.Weapon.localRotation = __instance.HandsContainer.Weapon.localRotation * Quaternion.Euler(0, 0, -_rotLerpXforRot * PrimeMover.DevTestFloat2.Value);
        __instance.HandsContainer.Weapon.localRotation = __instance.HandsContainer.Weapon.localRotation * Quaternion.Euler(0, 0, -_rotLerpXforRot * PrimeMover.DevTestFloat2.Value * _finalParallaxMulti);




        */
    }
}

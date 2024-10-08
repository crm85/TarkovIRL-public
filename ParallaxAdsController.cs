﻿using EFT.InventoryLogic;
using UnityEngine;

namespace TarkovIRL
{
    public static class ParallaxAdsController
    {
        static float _parallaxWeight = 1f;

        static bool _intoAds = false;
        static float _adsSpeedMod = 1f;
        static float _adsLerpWeight = 1f;

        static bool _shotSwitch = false;
        static bool _intoShot = false;
        static float _shotLerp = 1f;
        static float _shotWeight = 1f;

        public static void UpdateLerps()
        {
            LerpAds();
            LerpShot();
            DefineParallaxWeight();
        }

        public static void StartNewAds(bool intoAds, float wpnMulti)
        {
            _intoAds = intoAds;
            _adsSpeedMod = wpnMulti;
        }

        static void LerpAds()
        {
            float targetWeight = _intoAds ? PrimeMover.ParallaxInAds.Value : 1f;
            _adsLerpWeight = Mathf.Lerp(_adsLerpWeight, targetWeight, PrimeMover.Instance.DeltaTime * _adsSpeedMod * PrimeMover.AdsParallaxTimeMulti.Value);
        }

        static void LerpShot()
        {
            if (_intoShot)
            {
                _shotLerp = Mathf.Lerp(_shotLerp, _shotWeight * 1.05f, PrimeMover.Instance.DeltaTime * _shotWeight * PrimeMover.ShotParallaxResetTimeMulti.Value * 3f);
                if (_shotLerp >= _shotWeight * 0.95f) _intoShot = false;
            }
            else
            {
                _shotLerp = Mathf.Lerp(_shotLerp, _adsLerpWeight * 0.95f, PrimeMover.Instance.DeltaTime * (1f / _shotWeight) * PrimeMover.ShotParallaxResetTimeMulti.Value);
                if (_shotLerp <= _adsLerpWeight) _shotSwitch = false;
            }
        }

        static public void StartNewShot(Weapon weapon)
        {
            float weaponWeight = weapon.GetSingleItemTotalWeight() * PrimeMover.ShotParallaxWeaponWeightMulti.Value;
            float cartridgeWeight = weapon.CurrentAmmoTemplate.BulletMassGram;
            float newShotWeight = cartridgeWeight / weaponWeight;
            _shotWeight = newShotWeight;
            _shotSwitch = true;
            _intoShot = true;
        }

        static void DefineParallaxWeight()
        {
            _parallaxWeight = _shotSwitch ? _shotLerp : _adsLerpWeight;
        }

        //
        // sole output of class
        //
        public static float ParallaxWeight { get { return _parallaxWeight; } }
    }
}

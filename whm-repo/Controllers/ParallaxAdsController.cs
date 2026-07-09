using EFT.InventoryLogic;
using EFT.UI;
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

        static readonly float _HeadTiltValY = 2f;
        static readonly float _HeadTiltValZ = 6f;

        public static void Reset()
        {
            _parallaxWeight = 1f;
            _intoAds = false;
            _adsSpeedMod = 1f;
            _adsLerpWeight = 1f;
            _shotSwitch = false;
            _intoShot = false;
            _shotLerp = 1f;
            _shotWeight = 1f;
        }

        public static void UpdateLerps(float dt)
        {
            LerpAds(dt);
            LerpShot(dt);
            DefineParallaxWeight();
        }

        public static void StartNewAds(bool intoAds, float wpnMulti)
        {
            _intoAds = intoAds;
            _adsSpeedMod = wpnMulti;
        }

        static void LerpAds(float dt)
        {
            float targetWeight = _intoAds ? PrimeMover.ParallaxInAds.Value : 1f;
            _adsLerpWeight = Mathf.Lerp(_adsLerpWeight, targetWeight, dt * _adsSpeedMod * PrimeMover.AdsParallaxTimeMulti.Value);
        }

        static void LerpShot(float dt)
        {
            if (_intoShot)
            {
                _shotLerp = Mathf.Lerp(_shotLerp, _shotWeight * 1.05f, dt * _shotWeight * PrimeMover.ShotParallaxResetTimeMulti.Value * 3f);
                if (_shotLerp >= _shotWeight * 0.95f) _intoShot = false;
            }
            else
            {
                _shotLerp = Mathf.Lerp(_shotLerp, _adsLerpWeight * 0.95f, dt * (1f / _shotWeight) * PrimeMover.ShotParallaxResetTimeMulti.Value);
                if (_shotLerp <= _adsLerpWeight) _shotSwitch = false;
            }
        }

        static public void StartNewShot(Weapon weapon)
        {
            if (weapon == null || weapon.CurrentAmmoTemplate == null)
            {
                return;
            }

            float weaponWeight = weapon.Weight * PrimeMover.ShotParallaxWeaponWeightMulti.Value;
            if (!TIRLUtils.IsFinite(weaponWeight) || weaponWeight <= 0.0001f)
            {
                weaponWeight = 1f;
            }

            float cartridgeWeight = weapon.CurrentAmmoTemplate.BulletMassGram;
            float newShotWeight = cartridgeWeight / weaponWeight;
            if (weapon.WeapClass == "pistol")
            {
                newShotWeight = Mathf.Min(newShotWeight, PrimeMover.PistolShotParallaxWeightClamp.Value);
            }

            newShotWeight = Mathf.Max(0f, TIRLUtils.FiniteOr(newShotWeight, 1f));
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

        public static void GetParallaxADSHeadTilt(out float Y, out float Z)
        {
            if (PlayerMotionController.IsAiming && WeaponController.HasCheekWeld())
            {
                float shoulderSwap = RealismMod.StanceController.IsLeftShoulder ? -1f : 1f;
                Y = _HeadTiltValY * _adsLerpWeight * shoulderSwap;
                Z = _HeadTiltValZ * _adsLerpWeight * shoulderSwap;
                return;
            }

            Y = 0;
            Z = 0;
        }
    }
}

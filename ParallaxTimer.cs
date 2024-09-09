using EFT.InventoryLogic;
using UnityEngine;

namespace TarkovIRL
{
    public static class ParallaxTimer
    {
        static readonly float _MinWeight = 0.2f;

        static float _parallaxWeight = 1f;

        static bool _intoAds = false;
        static float _weaponMulti = 1f;
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
            _weaponMulti = wpnMulti;
        }

        static void LerpAds()
        {
            float targetWeight = _intoAds ? _MinWeight : 1f;
            _adsLerpWeight = Mathf.Lerp(_adsLerpWeight, targetWeight, PrimeMover.Instance.DeltaTime * _weaponMulti * PrimeMover.AdsParallaxTaperMulti.Value);
        }

        static void LerpShot()
        {
            if (!_shotSwitch)
            {
                return;
            }

            if (_intoShot)
            {
                _shotLerp = Mathf.Lerp(_shotLerp, _shotWeight, PrimeMover.Instance.DeltaTime * _shotWeight * PrimeMover.ShotParallaxTaperMulti.Value * 3f);
                if (_shotLerp >= _shotWeight) _intoShot = false;
            }
            else
            {
                _shotLerp = Mathf.Lerp(_shotLerp, _adsLerpWeight, PrimeMover.Instance.DeltaTime * (1f / _shotWeight) * PrimeMover.ShotParallaxTaperMulti.Value);
            }
            if (_shotLerp <= _adsLerpWeight)
            {
                _shotSwitch = false;
            }
        }

        static public void StartNewShot(Weapon weapon)
        {
            float weaponWeight = weapon.GetSingleItemTotalWeight();
            float cartridgeWeight = weapon.CurrentAmmoTemplate.BulletMassGram;
            float newShotWeight = cartridgeWeight / weaponWeight;
            _shotWeight = newShotWeight * PrimeMover.ShotParallaxMulti.Value;
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

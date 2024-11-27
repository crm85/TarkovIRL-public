﻿using System;
using SPT.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using EFT.UI;
using HarmonyLib;

namespace TarkovIRL
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class PrimeMover : BaseUnityPlugin
    {
        const string modGUID = "TarkovIRL";
        const string modName = "TarkovIRL - WHM";
        const string modVersion = "0.7";

        public static PrimeMover Instance;

        //
        // Curves
        //
        public AnimationCurve BreathCurve;
        public AnimationCurve PoseChangeCurve;
        public AnimationCurve HandsShakeCurve;
        public AnimationCurve SmoothEdgesCurve;
        public AnimationCurve FootStepCurve;
        public AnimationCurve ErgoAttenuationCurve;
        public AnimationCurve ThrowVisualCurveX;
        public AnimationCurve ThrowVisualCurveY;
        public AnimationCurve ThrowVisualUnderhandCurveX;
        public AnimationCurve ThrowVisualUnderhandCurveY;
        public AnimationCurve WeightAttenuationCurve;
        public AnimationCurve ThrowAlphaCurve;
        public AnimationCurve NewSwayCurve;
        public AnimationCurve SideToSideCurve;

        //
        // Dts
        //
        public float DeltaTime = 0;
        public float Time = 0;
        public float FixedDeltaTime = 0;

        // new sway sliders

        // parallax sliders

        // deadzone sliders

        // misc fine tuning sliders


        const string BASE_FEATURES_SECTION = "1 - Toggle base features";
        const string GENERAL_SLIDERS = "2 - Adjust main feature values";
        const string NEW_SWAY_SLIDERS = "3 - Sway multipliers";
        const string PARALLAX_SLIDERS = "4 - Parallax multipliers";
        const string EFFICIENCY_SLIDERS = "5 - Efficiency multipliers";
        const string ROTATION_ENGINE_SLIDERS = "6 - Rotation engine multipliers";
        const string MISC_SLIDERS = "7 - Misc multipliers";
        const string DIRECTIONAL_SWAY = "8 - Directional Sway Values";
        const string AUGMENTED_RELOAD = "9 - Augmented Reload Values";
        const string WEAPON_TRANSITIONS = "99 - Weapon Transition Values";

        const string DEV_SECTION = "999 - Only for dev/testing";


        //
        // config items
        //
        public static ConfigEntry<bool> IsWeaponDeadzone;
        public static ConfigEntry<bool> IsWeaponSway;
        public static ConfigEntry<bool> IsBreathingEffect;
        public static ConfigEntry<bool> IsPoseEffect;
        public static ConfigEntry<bool> IsPoseChangeEffect;
        public static ConfigEntry<bool> IsArmShakeEffect;
        public static ConfigEntry<bool> IsSmallMovementsEffect;
        public static ConfigEntry<bool> IsFootstepEffect;
        public static ConfigEntry<bool> IsParallaxEffect;
        public static ConfigEntry<bool> IsDirectionalSway;
        public static ConfigEntry<bool> IsHeadTiltADS;
        
        public static ConfigEntry<bool> IsLogging;
        public static ConfigEntry<bool> DebugSpam;

        public static ConfigEntry<float> WeaponDeadzoneMulti;
        public static ConfigEntry<float> WeaponSwayMulti;
        public static ConfigEntry<float> BreathingEffectMulti;
        public static ConfigEntry<float> ArmShakeMulti;
        public static ConfigEntry<float> ArmShakeRateMulti;
        public static ConfigEntry<float> ParallaxMulti;
        public static ConfigEntry<float> AdsParallaxTimeMulti;
        public static ConfigEntry<float> ShotParallaxResetTimeMulti;
        public static ConfigEntry<float> EfficiencyLerpMulti;
        public static ConfigEntry<float> ShotParallaxWeaponWeightMulti;
        public static ConfigEntry<float> ParallaxSetSizeMulti;
        public static ConfigEntry<float> CameraUpdateMulti;
        public static ConfigEntry<float> FootstepLerpMulti;
        public static ConfigEntry<float> FootstepIntesnityMulti;
        public static ConfigEntry<float> ParallaxInAds;
        public static ConfigEntry<float> PistolSpecificParallax;
        public static ConfigEntry<float> ThrowStrengthMulti;
        public static ConfigEntry<float> ThrowSpeedMulti;
        public static ConfigEntry<float> EfficiencyInjuryDebuffMulti;
        public static ConfigEntry<float> ParallaxRecenterFactor;
        public static ConfigEntry<float> NewSwayPositionMulti;
        public static ConfigEntry<float> NewSwayRotationMulti;
        public static ConfigEntry<float> NewSwayPositionDTMulti;
        public static ConfigEntry<float> NewSwayRotationDTMulti;
        public static ConfigEntry<float> NewSwayWpnUnstockedDropValue;
        public static ConfigEntry<float> NewSwayWpnUnstockedDropSpeed;
        public static ConfigEntry<float> WeaponCantValue;
        public static ConfigEntry<float> SideToSideSwayMulti;
        public static ConfigEntry<float> SideToSideRotationDTMulti;
        public static ConfigEntry<float> SideToSidePositionDTMulti;
        public static ConfigEntry<float> FootstepCutoffRatio;
        public static ConfigEntry<float> MotionTrackingThreshold;
        public static ConfigEntry<float> LeanExtraVerticalMulti;
        public static ConfigEntry<float> NewSwayWpnDropFromRotMulti;
        public static ConfigEntry<float> RotationAverageDTMulti;
        public static ConfigEntry<float> ParallaxDTMulti;
        public static ConfigEntry<float> ParallaxRotationSmoothingMulti;
        public static ConfigEntry<float> DevTestFloat7;
        public static ConfigEntry<float> RotationHistoryClamp;
        public static ConfigEntry<float> HyperVerticalClamp;
        public static ConfigEntry<float> HyperVerticalMulti;
        public static ConfigEntry<float> HyperVerticalDT;
        public static ConfigEntry<float> NewSwayADSRotClamp;
        public static ConfigEntry<float> NewSwayRotDeltaClamp;
        public static ConfigEntry<float> NewSwayRotFinalClamp;
        public static ConfigEntry<float> LeanCounterRotateMod;
        public static ConfigEntry<float> ParallaxHardClamp;

        // directional sway
        public static ConfigEntry<float> DirectionalSwayLateralPosValue;
        public static ConfigEntry<float> DirectionalSwayProjectedPosValue;
        public static ConfigEntry<float> DirectionalSwayLateralRotValue;
        public static ConfigEntry<float> DirectionalSwayVerticalRotValue;
        public static ConfigEntry<float> DirectionalSwayMulti;
        public static ConfigEntry<float> DirectionalSwayLerpSpeed;
        public static ConfigEntry<float> DirectionalSwayLerpOnAds;

        // augmented reloads
        public static ConfigEntry<float> AugmentedReloadSpeed;
        public static ConfigEntry<float> AugmentedReloadSprintingDebuff;

        // weapon transitions
        public static ConfigEntry<float> TransitionSpeedPhase1;
        public static ConfigEntry<float> TransitionSpeedPhase2;

        // sling vals
        public static ConfigEntry<float> SlingPositionStartX;
        public static ConfigEntry<float> SlingPositionStartY;
        public static ConfigEntry<float> SlingPositionStartZ;

        public static ConfigEntry<float> SlingRotationStartX;
        public static ConfigEntry<float> SlingRotationStartY;
        public static ConfigEntry<float> SlingRotationStartZ;

        public static ConfigEntry<float> SlingPositionEndX;
        public static ConfigEntry<float> SlingPositionEndY;
        public static ConfigEntry<float> SlingPositionEndZ;

        public static ConfigEntry<float> SlingRotationEndX;
        public static ConfigEntry<float> SlingRotationEndY;
        public static ConfigEntry<float> SlingRotationEndZ;

        // shoulder vals
        public static ConfigEntry<float> ShoulderPositionStartX;
        public static ConfigEntry<float> ShoulderPositionStartY;
        public static ConfigEntry<float> ShoulderPositionStartZ;

        public static ConfigEntry<float> ShoulderRotationStartX;
        public static ConfigEntry<float> ShoulderRotationStartY;
        public static ConfigEntry<float> ShoulderRotationStartZ;

        public static ConfigEntry<float> ShoulderPositionEndX;
        public static ConfigEntry<float> ShoulderPositionEndY;
        public static ConfigEntry<float> ShoulderPositionEndZ;

        public static ConfigEntry<float> ShoulderRotationEndX;
        public static ConfigEntry<float> ShoulderRotationEndY;
        public static ConfigEntry<float> ShoulderRotationEndZ;

        // holster vals
        public static ConfigEntry<float> HolsterPositionStartX;
        public static ConfigEntry<float> HolsterPositionStartY;
        public static ConfigEntry<float> HolsterPositionStartZ;

        public static ConfigEntry<float> HolsterRotationStartX;
        public static ConfigEntry<float> HolsterRotationStartY;
        public static ConfigEntry<float> HolsterRotationStartZ;

        public static ConfigEntry<float> HolsterPositionEndX;
        public static ConfigEntry<float> HolsterPositionEndY;
        public static ConfigEntry<float> HolsterPositionEndZ;

        public static ConfigEntry<float> HolsterRotationEndX;
        public static ConfigEntry<float> HolsterRotationEndY;
        public static ConfigEntry<float> HolsterRotationEndZ;

        // transition curves
        public static ConfigEntry<float> Sling2ShoulderSpeed1;
        public static ConfigEntry<float> Sling2ShoulderSpeed2;

        public static ConfigEntry<float> Sling2ShoulderCurve1_1;
        public static ConfigEntry<float> Sling2ShoulderCurve2_1;
        public static ConfigEntry<float> Sling2ShoulderCurve3_1;
        public static ConfigEntry<float> Sling2ShoulderCurve4_1;
        public static ConfigEntry<float> Sling2ShoulderCurve5_1;
        public static ConfigEntry<float> Sling2ShoulderCurve6_1;

        public static ConfigEntry<float> Sling2ShoulderCurve1_2;
        public static ConfigEntry<float> Sling2ShoulderCurve2_2;
        public static ConfigEntry<float> Sling2ShoulderCurve3_2;
        public static ConfigEntry<float> Sling2ShoulderCurve4_2;
        public static ConfigEntry<float> Sling2ShoulderCurve5_2;
        public static ConfigEntry<float> Sling2ShoulderCurve6_2;

        public static ConfigEntry<float> Shoulder2SlingSpeed1;
        public static ConfigEntry<float> Shoulder2SlingSpeed2;

        public static ConfigEntry<float> Shoulder2SlingCurve1_1;
        public static ConfigEntry<float> Shoulder2SlingCurve2_1;
        public static ConfigEntry<float> Shoulder2SlingCurve3_1;
        public static ConfigEntry<float> Shoulder2SlingCurve4_1;
        public static ConfigEntry<float> Shoulder2SlingCurve5_1;
        public static ConfigEntry<float> Shoulder2SlingCurve6_1;
        public static ConfigEntry<float> Shoulder2SlingCurve1_2;
        public static ConfigEntry<float> Shoulder2SlingCurve2_2;
        public static ConfigEntry<float> Shoulder2SlingCurve3_2;
        public static ConfigEntry<float> Shoulder2SlingCurve4_2;
        public static ConfigEntry<float> Shoulder2SlingCurve5_2;
        public static ConfigEntry<float> Shoulder2SlingCurve6_2;

        public static ConfigEntry<float> TransformSmoothingDTMulti;


        // debug 
        public static ConfigEntry<float> DebugHeadRotX;
        public static ConfigEntry<float> DebugHeadRotY;
        public static ConfigEntry<float> DebugHeadRotZ;

        public static ConfigEntry<float> DebugHandsRotX;
        public static ConfigEntry<float> DebugHandsRotY;
        public static ConfigEntry<float> DebugHandsRotZ;

        public static ConfigEntry<float> DebugHandsRotX2;
        public static ConfigEntry<float> DebugHandsRotY2;
        public static ConfigEntry<float> DebugHandsRotZ2;

        public static ConfigEntry<float> DebugHandsPosX;
        public static ConfigEntry<float> DebugHandsPosY;
        public static ConfigEntry<float> DebugHandsPosZ;

        public static ConfigEntry<float> DebugFloat1;
        public static ConfigEntry<float> DebugFloat2;

        // anim curve builder
        public static ConfigEntry<float> AnimCurveTime0;
        public static ConfigEntry<float> AnimCurveTime0_2;
        public static ConfigEntry<float> AnimCurveTime0_4;
        public static ConfigEntry<float> AnimCurveTime0_6;
        public static ConfigEntry<float> AnimCurveTime0_8;
        public static ConfigEntry<float> AnimCurveTime1;

        public static ConfigEntry<float> AnimCurveTime0p2;
        public static ConfigEntry<float> AnimCurveTime0_2p2;
        public static ConfigEntry<float> AnimCurveTime0_4p2;
        public static ConfigEntry<float> AnimCurveTime0_6p2;
        public static ConfigEntry<float> AnimCurveTime0_8p2;
        public static ConfigEntry<float> AnimCurveTime1p2;

        // config defaults
        readonly float AdsParallaxTimeMultiDefault = 30f;
        readonly float ArmShakeMultiDefault = 1f;
        readonly float ArmShakeRateMultiDefault = 1f;
        readonly float BreathingEffectMultiDefault = 1f;
        readonly float EfficiencyLerpMultiDefault = 0.8f;
        readonly float FootstepLerpMultiDefault = 0.1f;
        readonly float ParallaxInAdsDefault = 0.3f;
        readonly float ShotParallaxWeaponWeightMultiDefault = 5f;
        readonly float ShotParallaxResetTimeMultiDefault = 10f;
        readonly float WeaponDeadzoneMultiDefault = 0.3f;
        readonly float ThrowStrengthMultiDefault = 14;
        readonly float ThrowSpeedMultiDefault = 2.25f;

        // head rot smoothing
        public static ConfigEntry<float> HeadRotationSmoothing;
        public static ConfigEntry<float> HeadRotationSmoothing2;
        public static ConfigEntry<float> HeadRotationSmoothing3;


        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            Initialize();
            LoadConfigValues();
        }

        void Initialize()
        {
            UtilsTIRL.Logger = this.Logger;

            BreathCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0.001708984f), new Keyframe(0.01f, 0.07340872f), new Keyframe(0.02f, 0.1447571f), new Keyframe(0.03f, 0.2159234f), new Keyframe(0.04f, 0.287077f), new Keyframe(0.05f, 0.3583873f), new Keyframe(0.05999999f, 0.4300235f), new Keyframe(0.06999999f, 0.5021549f), new Keyframe(0.07999999f, 0.5749511f), new Keyframe(0.08999999f, 0.6485812f), new Keyframe(0.09999999f, 0.7190837f), new Keyframe(0.11f, 0.7796323f), new Keyframe(0.12f, 0.8308698f), new Keyframe(0.13f, 0.8735839f), new Keyframe(0.14f, 0.9085627f), new Keyframe(0.15f, 0.9365937f), new Keyframe(0.16f, 0.958465f), new Keyframe(0.17f, 0.9749643f), new Keyframe(0.18f, 0.9868796f), new Keyframe(0.19f, 0.9949986f), new Keyframe(0.2f, 1.000109f), new Keyframe(0.21f, 1.003f), new Keyframe(0.22f, 1.004457f), new Keyframe(0.23f, 1.00527f), new Keyframe(0.24f, 1.006216f), new Keyframe(0.25f, 0.9934806f), new Keyframe(0.26f, 0.9566338f), new Keyframe(0.27f, 0.8993476f), new Keyframe(0.28f, 0.825294f), new Keyframe(0.29f, 0.7381451f), new Keyframe(0.3f, 0.6415731f), new Keyframe(0.31f, 0.5392498f), new Keyframe(0.32f, 0.4348473f), new Keyframe(0.33f, 0.3327447f), new Keyframe(0.3399999f, 0.2382682f), new Keyframe(0.3499999f, 0.1512108f), new Keyframe(0.3599999f, 0.07075208f), new Keyframe(0.3699999f, -0.003928483f), new Keyframe(0.3799999f, -0.07365131f), new Keyframe(0.3899999f, -0.1392368f), new Keyframe(0.3999999f, -0.2015056f), new Keyframe(0.4099999f, -0.2613338f), new Keyframe(0.4199999f, -0.3202845f), new Keyframe(0.4299999f, -0.378557f), new Keyframe(0.4399998f, -0.4358587f), new Keyframe(0.4499998f, -0.4918973f), new Keyframe(0.4599998f, -0.5463802f), new Keyframe(0.4699998f, -0.5990149f), new Keyframe(0.4799998f, -0.6495088f), new Keyframe(0.4899998f, -0.6975697f), new Keyframe(0.4999998f, -0.7429048f), new Keyframe(0.5099998f, -0.7852218f), new Keyframe(0.5199998f, -0.824228f), new Keyframe(0.5299998f, -0.8596311f), new Keyframe(0.5399998f, -0.8911383f), new Keyframe(0.5499998f, -0.9184575f), new Keyframe(0.5599998f, -0.941296f), new Keyframe(0.5699998f, -0.9593612f), new Keyframe(0.5799997f, -0.9723607f), new Keyframe(0.5899997f, -0.9800022f), new Keyframe(0.5999997f, -0.9830432f), new Keyframe(0.6099997f, -0.9852618f), new Keyframe(0.6199997f, -0.9870723f), new Keyframe(0.6299997f, -0.988526f), new Keyframe(0.6399997f, -0.9896744f), new Keyframe(0.6499997f, -0.9905687f), new Keyframe(0.6599997f, -0.9912603f), new Keyframe(0.6699997f, -0.9918006f), new Keyframe(0.6799996f, -0.992241f), new Keyframe(0.6899996f, -0.9926327f), new Keyframe(0.6999996f, -0.9930272f), new Keyframe(0.7099996f, -0.9934759f), new Keyframe(0.7199996f, -0.9940174f), new Keyframe(0.7299996f, -0.9938536f), new Keyframe(0.7399996f, -0.9923377f), new Keyframe(0.7499996f, -0.9892635f), new Keyframe(0.7599996f, -0.9844247f), new Keyframe(0.7699996f, -0.9776151f), new Keyframe(0.7799996f, -0.9686285f), new Keyframe(0.7899995f, -0.9572586f), new Keyframe(0.7999995f, -0.9433164f), new Keyframe(0.8099995f, -0.9273839f), new Keyframe(0.8199995f, -0.909653f), new Keyframe(0.8299995f, -0.8898755f), new Keyframe(0.8399995f, -0.8678032f), new Keyframe(0.8499995f, -0.8431879f), new Keyframe(0.8599995f, -0.8157812f), new Keyframe(0.8699995f, -0.7853352f), new Keyframe(0.8799995f, -0.7516014f), new Keyframe(0.8899994f, -0.7143317f), new Keyframe(0.8999994f, -0.673278f), new Keyframe(0.9099994f, -0.6281918f), new Keyframe(0.9199994f, -0.5788252f), new Keyframe(0.9299994f, -0.5249298f), new Keyframe(0.9399994f, -0.4662574f), new Keyframe(0.9499994f, -0.4025598f), new Keyframe(0.9599994f, -0.3335888f), new Keyframe(0.9699994f, -0.2590962f), new Keyframe(0.9799994f, -0.1788337f), new Keyframe(0.9899994f, -0.09255308f), new Keyframe(0.9999993f, -6.377697E-06f)
            );

            PoseChangeCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0f), new Keyframe(0.01f, 0.04753859f), new Keyframe(0.02f, 0.06824711f), new Keyframe(0.03f, 0.07190572f), new Keyframe(0.04f, 0.06472864f), new Keyframe(0.05f, 0.03672462f), new Keyframe(0.05999999f, -0.01143076f), new Keyframe(0.06999999f, -0.07712235f), new Keyframe(0.07999999f, -0.157735f), new Keyframe(0.08999999f, -0.2506537f), new Keyframe(0.09999999f, -0.3532633f), new Keyframe(0.11f, -0.4620121f), new Keyframe(0.12f, -0.5536414f), new Keyframe(0.13f, -0.6220356f), new Keyframe(0.14f, -0.6711998f), new Keyframe(0.15f, -0.7051392f), new Keyframe(0.16f, -0.7278589f), new Keyframe(0.17f, -0.7433642f), new Keyframe(0.18f, -0.7578313f), new Keyframe(0.19f, -0.7805023f), new Keyframe(0.2f, -0.8117735f), new Keyframe(0.21f, -0.8513885f), new Keyframe(0.22f, -0.8990905f), new Keyframe(0.23f, -0.9521281f), new Keyframe(0.24f, -0.9863883f), new Keyframe(0.25f, -0.998183f), new Keyframe(0.26f, -0.9904182f), new Keyframe(0.27f, -0.9660002f), new Keyframe(0.28f, -0.9278349f), new Keyframe(0.29f, -0.8788286f), new Keyframe(0.3f, -0.8218873f), new Keyframe(0.31f, -0.759917f), new Keyframe(0.32f, -0.695824f), new Keyframe(0.33f, -0.6325832f), new Keyframe(0.3399999f, -0.5721059f), new Keyframe(0.3499999f, -0.5145938f), new Keyframe(0.3599999f, -0.4601647f), new Keyframe(0.3699999f, -0.4089365f), new Keyframe(0.3799999f, -0.361027f), new Keyframe(0.3899999f, -0.316554f), new Keyframe(0.3999999f, -0.2756355f), new Keyframe(0.4099999f, -0.2383893f), new Keyframe(0.4199999f, -0.2049332f), new Keyframe(0.4299999f, -0.1753851f), new Keyframe(0.4399998f, -0.1498628f), new Keyframe(0.4499998f, -0.1284841f), new Keyframe(0.4599998f, -0.1113669f), new Keyframe(0.4699998f, -0.09862924f), new Keyframe(0.4799998f, -0.08953421f), new Keyframe(0.4899998f, -0.07422893f), new Keyframe(0.4999998f, -0.05229831f), new Keyframe(0.5099998f, -0.02683907f), new Keyframe(0.5199998f, -0.0009481311f), new Keyframe(0.5299998f, 0.02227778f), new Keyframe(0.5399998f, 0.03974189f), new Keyframe(0.5499998f, 0.04943786f), new Keyframe(0.5599998f, 0.05533161f), new Keyframe(0.5699998f, 0.05844303f), new Keyframe(0.5799997f, 0.05908555f), new Keyframe(0.5899997f, 0.05757258f), new Keyframe(0.5999997f, 0.05421757f), new Keyframe(0.6099997f, 0.04933393f), new Keyframe(0.6199997f, 0.0432351f), new Keyframe(0.6299997f, 0.03623449f), new Keyframe(0.6399997f, 0.02864554f), new Keyframe(0.6499997f, 0.02078166f), new Keyframe(0.6599997f, 0.01295629f), new Keyframe(0.6699997f, 0.00548286f), new Keyframe(0.6799996f, -0.001325212f), new Keyframe(0.6899996f, -0.007154491f), new Keyframe(0.6999996f, -0.01169154f), new Keyframe(0.7099996f, -0.01462297f), new Keyframe(0.7199996f, -0.01570082f), new Keyframe(0.7299996f, -0.01604912f), new Keyframe(0.7399996f, -0.01618946f), new Keyframe(0.7499996f, -0.01613806f), new Keyframe(0.7599996f, -0.01591115f), new Keyframe(0.7699996f, -0.01552495f), new Keyframe(0.7799996f, -0.01499569f), new Keyframe(0.7899995f, -0.01433959f), new Keyframe(0.7999995f, -0.01357289f), new Keyframe(0.8099995f, -0.01271181f), new Keyframe(0.8199995f, -0.01177257f), new Keyframe(0.8299995f, -0.01077141f), new Keyframe(0.8399995f, -0.009724541f), new Keyframe(0.8499995f, -0.008648196f), new Keyframe(0.8599995f, -0.007558605f), new Keyframe(0.8699995f, -0.006471992f), new Keyframe(0.8799995f, -0.005404581f), new Keyframe(0.8899994f, -0.0043726f), new Keyframe(0.8999994f, -0.003392277f), new Keyframe(0.9099994f, -0.002479835f), new Keyframe(0.9199994f, -0.001651503f), new Keyframe(0.9299994f, -0.0009235078f), new Keyframe(0.9399994f, -0.0003120769f), new Keyframe(0.9499994f, 0.0001665689f), new Keyframe(0.9599994f, 0.0004961994f), new Keyframe(0.9699994f, 0.0006605871f), new Keyframe(0.9799994f, 0.0006435066f), new Keyframe(0.9899994f, 0.0004287343f), new Keyframe(0.9999993f, 3.539026E-08f)
            );

            HandsShakeCurve = new AnimationCurve
            (
                 new Keyframe(0f, 0.003456681f), new Keyframe(0.01f, 0.01467377f), new Keyframe(0.02f, 0.02527229f), new Keyframe(0.03f, 0.03407215f), new Keyframe(0.04f, 0.03989322f), new Keyframe(0.05f, 0.0415554f), new Keyframe(0.05999999f, 0.04440531f), new Keyframe(0.06999999f, 0.051765f), new Keyframe(0.07999999f, 0.05902842f), new Keyframe(0.08999999f, 0.0615889f), new Keyframe(0.09999999f, 0.05141038f), new Keyframe(0.11f, 0.01802858f), new Keyframe(0.12f, 0.0317804f), new Keyframe(0.13f, 0.04252484f), new Keyframe(0.14f, 0.03777219f), new Keyframe(0.15f, 0.02463009f), new Keyframe(0.16f, 0.01020622f), new Keyframe(0.17f, 0.001608178f), new Keyframe(0.18f, 0.001896187f), new Keyframe(0.19f, -0.007047208f), new Keyframe(0.2f, -0.02585391f), new Keyframe(0.21f, -0.03216255f), new Keyframe(0.22f, -0.03334584f), new Keyframe(0.23f, -0.05267931f), new Keyframe(0.24f, -0.04703839f), new Keyframe(0.25f, -0.04069633f), new Keyframe(0.26f, -0.04325364f), new Keyframe(0.27f, -0.04701382f), new Keyframe(0.28f, -0.04307806f), new Keyframe(0.29f, -0.03145951f), new Keyframe(0.3f, -0.03458425f), new Keyframe(0.31f, -0.05242091f), new Keyframe(0.32f, -0.03461784f), new Keyframe(0.33f, -0.03887944f), new Keyframe(0.3399999f, -0.04251146f), new Keyframe(0.3499999f, -0.03486739f), new Keyframe(0.3599999f, -0.02544173f), new Keyframe(0.3699999f, -0.01010045f), new Keyframe(0.3799999f, -0.02236057f), new Keyframe(0.3899999f, 0.001355158f), new Keyframe(0.3999999f, 0.01982367f), new Keyframe(0.4099999f, 0.04136363f), new Keyframe(0.4199999f, 0.028871f), new Keyframe(0.4299999f, 0.03566539f), new Keyframe(0.4399998f, 0.03741714f), new Keyframe(0.4499998f, 0.03172546f), new Keyframe(0.4599998f, 0.0288302f), new Keyframe(0.4699998f, 0.0284416f), new Keyframe(0.4799998f, 0.01997722f), new Keyframe(0.4899998f, 0.008944051f), new Keyframe(0.4999998f, 0.001161236f), new Keyframe(0.5099998f, 0.00244794f), new Keyframe(0.5199998f, 0.009561044f), new Keyframe(0.5299998f, 0.0011282f), new Keyframe(0.5399998f, -0.01759064f), new Keyframe(0.5499998f, -0.03935509f), new Keyframe(0.5599998f, -0.05692476f), new Keyframe(0.5699998f, -0.06214953f), new Keyframe(0.5799997f, -0.05987017f), new Keyframe(0.5899997f, -0.0629185f), new Keyframe(0.5999997f, -0.07895129f), new Keyframe(0.6099997f, -0.0832698f), new Keyframe(0.6199997f, -0.08200739f), new Keyframe(0.6299997f, -0.07845679f), new Keyframe(0.6399997f, -0.06902104f), new Keyframe(0.6499997f, -0.04983614f), new Keyframe(0.6599997f, -0.04493317f), new Keyframe(0.6699997f, -0.05027279f), new Keyframe(0.6799996f, -0.03982494f), new Keyframe(0.6899996f, -0.0257119f), new Keyframe(0.6999996f, -0.01667746f), new Keyframe(0.7099996f, 0.001633378f), new Keyframe(0.7199996f, 0.02325357f), new Keyframe(0.7299996f, 0.03712844f), new Keyframe(0.7399996f, 0.03921978f), new Keyframe(0.7499996f, 0.03961103f), new Keyframe(0.7599996f, 0.04532627f), new Keyframe(0.7699996f, 0.03232246f), new Keyframe(0.7799996f, 0.009538032f), new Keyframe(0.7899995f, -0.005662378f), new Keyframe(0.7999995f, -0.01157239f), new Keyframe(0.8099995f, -0.02694051f), new Keyframe(0.8199995f, -0.04455786f), new Keyframe(0.8299995f, -0.05643301f), new Keyframe(0.8399995f, -0.05457452f), new Keyframe(0.8499995f, -0.04635786f), new Keyframe(0.8599995f, -0.04785218f), new Keyframe(0.8699995f, -0.05541039f), new Keyframe(0.8799995f, -0.06524507f), new Keyframe(0.8899994f, -0.07518715f), new Keyframe(0.8999994f, -0.08971603f), new Keyframe(0.9099994f, -0.07793824f), new Keyframe(0.9199994f, -0.06209349f), new Keyframe(0.9299994f, -0.05362688f), new Keyframe(0.9399994f, -0.06287797f), new Keyframe(0.9499994f, -0.06029455f), new Keyframe(0.9599994f, -0.03613466f), new Keyframe(0.9699994f, -0.01750777f), new Keyframe(0.9799994f, -0.00874934f), new Keyframe(0.9899994f, -0.003794672f), new Keyframe(0.9999993f, 0.003456004f)
            );

            SmoothEdgesCurve = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, 0.0001326637f), new Keyframe(0.02f, 0.0004161092f), new Keyframe(0.03f, 0.0008607136f), new Keyframe(0.04f, 0.001476854f), new Keyframe(0.05f, 0.002274909f), new Keyframe(0.05999999f, 0.003265254f), new Keyframe(0.06999999f, 0.004458267f), new Keyframe(0.07999999f, 0.005864326f), new Keyframe(0.08999999f, 0.007493807f), new Keyframe(0.09999999f, 0.00935709f), new Keyframe(0.11f, 0.01146455f), new Keyframe(0.12f, 0.01382656f), new Keyframe(0.13f, 0.01645351f), new Keyframe(0.14f, 0.01935577f), new Keyframe(0.15f, 0.02254371f), new Keyframe(0.16f, 0.02602772f), new Keyframe(0.17f, 0.02981817f), new Keyframe(0.18f, 0.03392544f), new Keyframe(0.19f, 0.03835991f), new Keyframe(0.2f, 0.04313195f), new Keyframe(0.21f, 0.04825195f), new Keyframe(0.22f, 0.05373026f), new Keyframe(0.23f, 0.05957729f), new Keyframe(0.24f, 0.0658034f), new Keyframe(0.25f, 0.07241896f), new Keyframe(0.26f, 0.07943435f), new Keyframe(0.27f, 0.08685997f), new Keyframe(0.28f, 0.09470617f), new Keyframe(0.29f, 0.1029833f), new Keyframe(0.3f, 0.1117019f), new Keyframe(0.31f, 0.1208721f), new Keyframe(0.32f, 0.1305044f), new Keyframe(0.33f, 0.1406093f), new Keyframe(0.3399999f, 0.1511969f), new Keyframe(0.3499999f, 0.1622778f), new Keyframe(0.3599999f, 0.1738624f), new Keyframe(0.3699999f, 0.1859608f), new Keyframe(0.3799999f, 0.1985837f), new Keyframe(0.3899999f, 0.2117413f), new Keyframe(0.3999999f, 0.225444f), new Keyframe(0.4099999f, 0.2406231f), new Keyframe(0.4199999f, 0.258181f), new Keyframe(0.4299999f, 0.2778718f), new Keyframe(0.4399998f, 0.2994483f), new Keyframe(0.4499998f, 0.3226634f), new Keyframe(0.4599998f, 0.34727f), new Keyframe(0.4699998f, 0.3730207f), new Keyframe(0.4799998f, 0.3996685f), new Keyframe(0.4899998f, 0.4269661f), new Keyframe(0.4999998f, 0.4546664f), new Keyframe(0.5099998f, 0.4825224f), new Keyframe(0.5199998f, 0.5102865f), new Keyframe(0.5299998f, 0.5377118f), new Keyframe(0.5399998f, 0.5645511f), new Keyframe(0.5499998f, 0.5905572f), new Keyframe(0.5599998f, 0.6154829f), new Keyframe(0.5699998f, 0.6390811f), new Keyframe(0.5799997f, 0.6611046f), new Keyframe(0.5899997f, 0.6813061f), new Keyframe(0.5999997f, 0.6994448f), new Keyframe(0.6099997f, 0.7163168f), new Keyframe(0.6199997f, 0.7326097f), new Keyframe(0.6299997f, 0.7483313f), new Keyframe(0.6399997f, 0.7634894f), new Keyframe(0.6499997f, 0.7780919f), new Keyframe(0.6599997f, 0.7921466f), new Keyframe(0.6699997f, 0.8056613f), new Keyframe(0.6799996f, 0.8186439f), new Keyframe(0.6899996f, 0.8311023f), new Keyframe(0.6999996f, 0.843044f), new Keyframe(0.7099996f, 0.8544773f), new Keyframe(0.7199996f, 0.8654097f), new Keyframe(0.7299996f, 0.8758491f), new Keyframe(0.7399996f, 0.8858035f), new Keyframe(0.7499996f, 0.8952805f), new Keyframe(0.7599996f, 0.9042881f), new Keyframe(0.7699996f, 0.9128339f), new Keyframe(0.7799996f, 0.9209261f), new Keyframe(0.7899995f, 0.9285723f), new Keyframe(0.7999995f, 0.9357804f), new Keyframe(0.8099995f, 0.9425582f), new Keyframe(0.8199995f, 0.9489135f), new Keyframe(0.8299995f, 0.9548541f), new Keyframe(0.8399995f, 0.9603881f), new Keyframe(0.8499995f, 0.965523f), new Keyframe(0.8599995f, 0.9702669f), new Keyframe(0.8699995f, 0.9746274f), new Keyframe(0.8799995f, 0.9786125f), new Keyframe(0.8899994f, 0.98223f), new Keyframe(0.8999994f, 0.9854877f), new Keyframe(0.9099994f, 0.9883934f), new Keyframe(0.9199994f, 0.9909551f), new Keyframe(0.9299994f, 0.9931804f), new Keyframe(0.9399994f, 0.9950773f), new Keyframe(0.9499994f, 0.9966536f), new Keyframe(0.9599994f, 0.9979171f), new Keyframe(0.9699994f, 0.9988757f), new Keyframe(0.9799994f, 0.9995371f), new Keyframe(0.9899994f, 0.9999093f), new Keyframe(0.9999993f, 1f)
            );

            FootStepCurve = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, -0.0007582751f), new Keyframe(0.02f, -0.002873959f), new Keyframe(0.03f, -0.006510817f), new Keyframe(0.04f, -0.01183261f), new Keyframe(0.05f, -0.0190031f), new Keyframe(0.05999999f, -0.02818605f), new Keyframe(0.06999999f, -0.07559707f), new Keyframe(0.07999999f, -0.1779692f), new Keyframe(0.08999999f, -0.2486039f), new Keyframe(0.09999999f, -0.3393618f), new Keyframe(0.11f, -0.4510941f), new Keyframe(0.12f, -0.5388983f), new Keyframe(0.13f, -0.7058206f), new Keyframe(0.14f, -0.7770632f), new Keyframe(0.15f, -0.8418975f), new Keyframe(0.16f, -0.9043021f), new Keyframe(0.17f, -0.9277012f), new Keyframe(0.18f, -0.9331181f), new Keyframe(0.19f, -0.9377514f), new Keyframe(0.2f, -0.9411163f), new Keyframe(0.21f, -0.9427284f), new Keyframe(0.22f, -0.9421031f), new Keyframe(0.23f, -0.9316437f), new Keyframe(0.24f, -0.8840157f), new Keyframe(0.25f, -0.8100975f), new Keyframe(0.26f, -0.7255509f), new Keyframe(0.27f, -0.6460374f), new Keyframe(0.28f, -0.5872189f), new Keyframe(0.29f, -0.5632753f), new Keyframe(0.3f, -0.5436768f), new Keyframe(0.31f, -0.5142716f), new Keyframe(0.32f, -0.4799984f), new Keyframe(0.33f, -0.4457957f), new Keyframe(0.3399999f, -0.4164272f), new Keyframe(0.3499999f, -0.3825241f), new Keyframe(0.3599999f, -0.3399866f), new Keyframe(0.3699999f, -0.293821f), new Keyframe(0.3799999f, -0.2490342f), new Keyframe(0.3899999f, -0.2106325f), new Keyframe(0.3999999f, -0.1835791f), new Keyframe(0.4099999f, -0.1638359f), new Keyframe(0.4199999f, -0.1460326f), new Keyframe(0.4299999f, -0.1300441f), new Keyframe(0.4399998f, -0.1157452f), new Keyframe(0.4499998f, -0.1030107f), new Keyframe(0.4599998f, -0.09171553f), new Keyframe(0.4699998f, -0.08173455f), new Keyframe(0.4799998f, -0.07294258f), new Keyframe(0.4899998f, -0.0652145f), new Keyframe(0.4999998f, -0.05842514f), new Keyframe(0.5099998f, -0.05244936f), new Keyframe(0.5199998f, -0.04716203f), new Keyframe(0.5299998f, -0.04243797f), new Keyframe(0.5399998f, -0.03815205f), new Keyframe(0.5499998f, -0.03417915f), new Keyframe(0.5599998f, -0.03039409f), new Keyframe(0.5699998f, -0.02676406f), new Keyframe(0.5799997f, -0.02337446f), new Keyframe(0.5899997f, -0.02021899f), new Keyframe(0.5999997f, -0.01729017f), new Keyframe(0.6099997f, -0.01458049f), new Keyframe(0.6199997f, -0.01208249f), new Keyframe(0.6299997f, -0.009788649f), new Keyframe(0.6399997f, -0.007691495f), new Keyframe(0.6499997f, -0.005783532f), new Keyframe(0.6599997f, -0.004057273f), new Keyframe(0.6699997f, -0.002505228f), new Keyframe(0.6799996f, -0.001119906f), new Keyframe(0.6899996f, 0.0001061838f), new Keyframe(0.6999996f, 0.00118053f), new Keyframe(0.7099996f, 0.002110619f), new Keyframe(0.7199996f, 0.002903949f), new Keyframe(0.7299996f, 0.003568005f), new Keyframe(0.7399996f, 0.004110277f), new Keyframe(0.7499996f, 0.004538249f), new Keyframe(0.7599996f, 0.004859425f), new Keyframe(0.7699996f, 0.005081285f), new Keyframe(0.7799996f, 0.005211316f), new Keyframe(0.7899995f, 0.005257014f), new Keyframe(0.7999995f, 0.005225871f), new Keyframe(0.8099995f, 0.005125374f), new Keyframe(0.8199995f, 0.004963003f), new Keyframe(0.8299995f, 0.004746262f), new Keyframe(0.8399995f, 0.004482638f), new Keyframe(0.8499995f, 0.004179619f), new Keyframe(0.8599995f, 0.003844697f), new Keyframe(0.8699995f, 0.003485348f), new Keyframe(0.8799995f, 0.003109086f), new Keyframe(0.8899994f, 0.002723388f), new Keyframe(0.8999994f, 0.002335731f), new Keyframe(0.9099994f, 0.001953624f), new Keyframe(0.9199994f, 0.00158456f), new Keyframe(0.9299994f, 0.001236018f), new Keyframe(0.9399994f, 0.0009154733f), new Keyframe(0.9499994f, 0.0006304383f), new Keyframe(0.9599994f, 0.0003884193f), new Keyframe(0.9699994f, 0.0001968667f), new Keyframe(0.9799994f, 6.327219E-05f), new Keyframe(0.9899994f, -4.839152E-06f), new Keyframe(0.9999993f, 0f)
            );

            ErgoAttenuationCurve = new AnimationCurve
            (
                new Keyframe(0f, 0.25f), new Keyframe(0.01f, 0.2506315f), new Keyframe(0.02f, 0.2517748f), new Keyframe(0.03f, 0.2534092f), new Keyframe(0.04f, 0.2555136f), new Keyframe(0.05f, 0.2580674f), new Keyframe(0.05999999f, 0.2610497f), new Keyframe(0.06999999f, 0.2644395f), new Keyframe(0.07999999f, 0.2682161f), new Keyframe(0.08999999f, 0.2723586f), new Keyframe(0.09999999f, 0.2768462f), new Keyframe(0.11f, 0.281658f), new Keyframe(0.12f, 0.2867732f), new Keyframe(0.13f, 0.2921709f), new Keyframe(0.14f, 0.2978303f), new Keyframe(0.15f, 0.3037306f), new Keyframe(0.16f, 0.3098509f), new Keyframe(0.17f, 0.3161703f), new Keyframe(0.18f, 0.322668f), new Keyframe(0.19f, 0.3293232f), new Keyframe(0.2f, 0.336115f), new Keyframe(0.21f, 0.3430226f), new Keyframe(0.22f, 0.3500251f), new Keyframe(0.23f, 0.3571016f), new Keyframe(0.24f, 0.3642314f), new Keyframe(0.25f, 0.3713936f), new Keyframe(0.26f, 0.3785673f), new Keyframe(0.27f, 0.3857317f), new Keyframe(0.28f, 0.392866f), new Keyframe(0.29f, 0.3999492f), new Keyframe(0.3f, 0.4069606f), new Keyframe(0.31f, 0.4138794f), new Keyframe(0.32f, 0.4206846f), new Keyframe(0.33f, 0.4273554f), new Keyframe(0.3399999f, 0.433871f), new Keyframe(0.3499999f, 0.4402105f), new Keyframe(0.3599999f, 0.4463531f), new Keyframe(0.3699999f, 0.452278f), new Keyframe(0.3799999f, 0.4579642f), new Keyframe(0.3899999f, 0.463391f), new Keyframe(0.3999999f, 0.4685375f), new Keyframe(0.4099999f, 0.4733828f), new Keyframe(0.4199999f, 0.4779062f), new Keyframe(0.4299999f, 0.4820867f), new Keyframe(0.4399998f, 0.4859036f), new Keyframe(0.4499998f, 0.4893359f), new Keyframe(0.4599998f, 0.4923628f), new Keyframe(0.4699998f, 0.4949635f), new Keyframe(0.4799998f, 0.4971172f), new Keyframe(0.4899998f, 0.4988029f), new Keyframe(0.4999998f, 0.4999999f), new Keyframe(0.5099998f, 0.5012925f), new Keyframe(0.5199998f, 0.5032603f), new Keyframe(0.5299998f, 0.5058756f), new Keyframe(0.5399998f, 0.509111f), new Keyframe(0.5499998f, 0.5129387f), new Keyframe(0.5599998f, 0.5173312f), new Keyframe(0.5699998f, 0.5222607f), new Keyframe(0.5799997f, 0.5276995f), new Keyframe(0.5899997f, 0.5336201f), new Keyframe(0.5999997f, 0.5399949f), new Keyframe(0.6099997f, 0.5467961f), new Keyframe(0.6199997f, 0.5539961f), new Keyframe(0.6299997f, 0.5615673f), new Keyframe(0.6399997f, 0.5694821f), new Keyframe(0.6499997f, 0.5777127f), new Keyframe(0.6599997f, 0.5862316f), new Keyframe(0.6699997f, 0.5950111f), new Keyframe(0.6799996f, 0.6040236f), new Keyframe(0.6899996f, 0.6132413f), new Keyframe(0.6999996f, 0.6226368f), new Keyframe(0.7099996f, 0.6321822f), new Keyframe(0.7199996f, 0.6418501f), new Keyframe(0.7299996f, 0.6516127f), new Keyframe(0.7399996f, 0.6614424f), new Keyframe(0.7499996f, 0.6713115f), new Keyframe(0.7599996f, 0.6811924f), new Keyframe(0.7699996f, 0.6910575f), new Keyframe(0.7799996f, 0.7008791f), new Keyframe(0.7899995f, 0.7106296f), new Keyframe(0.7999995f, 0.7202812f), new Keyframe(0.8099995f, 0.7298065f), new Keyframe(0.8199995f, 0.7391778f), new Keyframe(0.8299995f, 0.7483672f), new Keyframe(0.8399995f, 0.7573473f), new Keyframe(0.8499995f, 0.7660906f), new Keyframe(0.8599995f, 0.774569f), new Keyframe(0.8699995f, 0.7827553f), new Keyframe(0.8799995f, 0.7906216f), new Keyframe(0.8899994f, 0.7981403f), new Keyframe(0.8999994f, 0.8052838f), new Keyframe(0.9099994f, 0.8120244f), new Keyframe(0.9199994f, 0.8183345f), new Keyframe(0.9299994f, 0.8241866f), new Keyframe(0.9399994f, 0.8295527f), new Keyframe(0.9499994f, 0.8344054f), new Keyframe(0.9599994f, 0.8387171f), new Keyframe(0.9699994f, 0.84246f), new Keyframe(0.9799994f, 0.8456065f), new Keyframe(0.9899994f, 0.848129f), new Keyframe(0.9999993f, 0.8499999f)
            );

            ThrowVisualCurveX = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, 0.002978058f), new Keyframe(0.02f, 0.009249885f), new Keyframe(0.03f, 0.01808028f), new Keyframe(0.04f, 0.02873405f), new Keyframe(0.05f, 0.04047598f), new Keyframe(0.05999999f, 0.05257087f), new Keyframe(0.06999999f, 0.06428354f), new Keyframe(0.07999999f, 0.07487878f), new Keyframe(0.08999999f, 0.08362138f), new Keyframe(0.09999999f, 0.08977617f), new Keyframe(0.11f, 0.07975212f), new Keyframe(0.12f, 0.04375243f), new Keyframe(0.13f, -0.009889014f), new Keyframe(0.14f, -0.07281638f), new Keyframe(0.15f, -0.1366737f), new Keyframe(0.16f, -0.1931049f), new Keyframe(0.17f, -0.2337542f), new Keyframe(0.18f, -0.2526112f), new Keyframe(0.19f, -0.2665491f), new Keyframe(0.2f, -0.2806153f), new Keyframe(0.21f, -0.294713f), new Keyframe(0.22f, -0.3087457f), new Keyframe(0.23f, -0.3226167f), new Keyframe(0.24f, -0.3362293f), new Keyframe(0.25f, -0.3494868f), new Keyframe(0.26f, -0.3622926f), new Keyframe(0.27f, -0.37455f), new Keyframe(0.28f, -0.3861625f), new Keyframe(0.29f, -0.3970333f), new Keyframe(0.3f, -0.4070658f), new Keyframe(0.31f, -0.4159627f), new Keyframe(0.32f, -0.4211259f), new Keyframe(0.33f, -0.422064f), new Keyframe(0.3399999f, -0.4191369f), new Keyframe(0.3499999f, -0.4127041f), new Keyframe(0.3599999f, -0.4031253f), new Keyframe(0.3699999f, -0.3907602f), new Keyframe(0.3799999f, -0.3759684f), new Keyframe(0.3899999f, -0.3591096f), new Keyframe(0.3999999f, -0.3405434f), new Keyframe(0.4099999f, -0.3206295f), new Keyframe(0.4199999f, -0.2997276f), new Keyframe(0.4299999f, -0.2781972f), new Keyframe(0.4399998f, -0.256398f), new Keyframe(0.4499998f, -0.2346898f), new Keyframe(0.4599998f, -0.2134321f), new Keyframe(0.4699998f, -0.1929846f), new Keyframe(0.4799998f, -0.1737069f), new Keyframe(0.4899998f, -0.1559589f), new Keyframe(0.4999998f, -0.1400999f), new Keyframe(0.5099998f, -0.1264897f), new Keyframe(0.5199998f, -0.1150528f), new Keyframe(0.5299998f, -0.1044564f), new Keyframe(0.5399998f, -0.09455517f), new Keyframe(0.5499998f, -0.08532649f), new Keyframe(0.5599998f, -0.07674783f), new Keyframe(0.5699998f, -0.06879658f), new Keyframe(0.5799997f, -0.06145016f), new Keyframe(0.5899997f, -0.05468599f), new Keyframe(0.5999997f, -0.04848151f), new Keyframe(0.6099997f, -0.04281412f), new Keyframe(0.6199997f, -0.03766123f), new Keyframe(0.6299997f, -0.03300028f), new Keyframe(0.6399997f, -0.02880868f), new Keyframe(0.6499997f, -0.02506384f), new Keyframe(0.6599997f, -0.0217432f), new Keyframe(0.6699997f, -0.01882415f), new Keyframe(0.6799996f, -0.01628414f), new Keyframe(0.6899996f, -0.01410055f), new Keyframe(0.6999996f, -0.01225084f), new Keyframe(0.7099996f, -0.01071239f), new Keyframe(0.7199996f, -0.009462655f), new Keyframe(0.7299996f, -0.008479021f), new Keyframe(0.7399996f, -0.007738933f), new Keyframe(0.7499996f, -0.007219799f), new Keyframe(0.7599996f, -0.006899022f), new Keyframe(0.7699996f, -0.006754041f), new Keyframe(0.7799996f, -0.006762281f), new Keyframe(0.7899995f, -0.006901138f), new Keyframe(0.7999995f, -0.007148027f), new Keyframe(0.8099995f, -0.007480398f), new Keyframe(0.8199995f, -0.007875651f), new Keyframe(0.8299995f, -0.008311182f), new Keyframe(0.8399995f, -0.008764438f), new Keyframe(0.8499995f, -0.009212844f), new Keyframe(0.8599995f, -0.009633817f), new Keyframe(0.8699995f, -0.01000475f), new Keyframe(0.8799995f, -0.01030309f), new Keyframe(0.8899994f, -0.01050624f), new Keyframe(0.8999994f, -0.01059162f), new Keyframe(0.9099994f, -0.01053666f), new Keyframe(0.9199994f, -0.01031873f), new Keyframe(0.9299994f, -0.009915337f), new Keyframe(0.9399994f, -0.009303771f), new Keyframe(0.9499994f, -0.008461602f), new Keyframe(0.9599994f, -0.007366158f), new Keyframe(0.9699994f, -0.005994886f), new Keyframe(0.9799994f, -0.004325181f), new Keyframe(0.9899994f, -0.002334483f), new Keyframe(0.9999993f, -1.937151E-07f)
            );

            ThrowVisualCurveY = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, -0.002133129f), new Keyframe(0.02f, -0.0007746345f), new Keyframe(0.03f, 0.003373357f), new Keyframe(0.04f, 0.009608719f), new Keyframe(0.05f, 0.01722933f), new Keyframe(0.05999999f, 0.02553305f), new Keyframe(0.06999999f, 0.03381777f), new Keyframe(0.07999999f, 0.04138135f), new Keyframe(0.08999999f, 0.04752167f), new Keyframe(0.09999999f, 0.05153698f), new Keyframe(0.11f, 0.0535779f), new Keyframe(0.12f, 0.05426566f), new Keyframe(0.13f, 0.05369683f), new Keyframe(0.14f, 0.0519679f), new Keyframe(0.15f, 0.04917544f), new Keyframe(0.16f, 0.04541595f), new Keyframe(0.17f, 0.04078599f), new Keyframe(0.18f, 0.03538207f), new Keyframe(0.19f, 0.02930073f), new Keyframe(0.2f, 0.0226385f), new Keyframe(0.21f, 0.01549193f), new Keyframe(0.22f, 0.007957522f), new Keyframe(0.23f, 0.0001318268f), new Keyframe(0.24f, -0.007888626f), new Keyframe(0.25f, -0.0160073f), new Keyframe(0.26f, -0.02412766f), new Keyframe(0.27f, -0.03215319f), new Keyframe(0.28f, -0.03998734f), new Keyframe(0.29f, -0.04753359f), new Keyframe(0.3f, -0.05469544f), new Keyframe(0.31f, -0.06137628f), new Keyframe(0.32f, -0.06747967f), new Keyframe(0.33f, -0.07290903f), new Keyframe(0.3399999f, -0.07756777f), new Keyframe(0.3499999f, -0.08135948f), new Keyframe(0.3599999f, -0.08418755f), new Keyframe(0.3699999f, -0.08595549f), new Keyframe(0.3799999f, -0.08663395f), new Keyframe(0.3899999f, -0.08696654f), new Keyframe(0.3999999f, -0.08718631f), new Keyframe(0.4099999f, -0.08729605f), new Keyframe(0.4199999f, -0.08729858f), new Keyframe(0.4299999f, -0.0871967f), new Keyframe(0.4399998f, -0.08699321f), new Keyframe(0.4499998f, -0.08669093f), new Keyframe(0.4599998f, -0.08629265f), new Keyframe(0.4699998f, -0.08580118f), new Keyframe(0.4799998f, -0.08521932f), new Keyframe(0.4899998f, -0.0845499f), new Keyframe(0.4999998f, -0.08379569f), new Keyframe(0.5099998f, -0.08295952f), new Keyframe(0.5199998f, -0.08204418f), new Keyframe(0.5299998f, -0.0810525f), new Keyframe(0.5399998f, -0.07998727f), new Keyframe(0.5499998f, -0.07885128f), new Keyframe(0.5599998f, -0.07764735f), new Keyframe(0.5699998f, -0.07637829f), new Keyframe(0.5799997f, -0.0750469f), new Keyframe(0.5899997f, -0.07365599f), new Keyframe(0.5999997f, -0.07220837f), new Keyframe(0.6099997f, -0.07070682f), new Keyframe(0.6199997f, -0.06915417f), new Keyframe(0.6299997f, -0.06755322f), new Keyframe(0.6399997f, -0.06590676f), new Keyframe(0.6499997f, -0.06421763f), new Keyframe(0.6599997f, -0.0624886f), new Keyframe(0.6699997f, -0.06072249f), new Keyframe(0.6799996f, -0.05892211f), new Keyframe(0.6899996f, -0.05709026f), new Keyframe(0.6999996f, -0.05522974f), new Keyframe(0.7099996f, -0.05334336f), new Keyframe(0.7199996f, -0.05143393f), new Keyframe(0.7299996f, -0.04950425f), new Keyframe(0.7399996f, -0.04755713f), new Keyframe(0.7499996f, -0.04559538f), new Keyframe(0.7599996f, -0.04362179f), new Keyframe(0.7699996f, -0.04163917f), new Keyframe(0.7799996f, -0.03965033f), new Keyframe(0.7899995f, -0.03765807f), new Keyframe(0.7999995f, -0.03566522f), new Keyframe(0.8099995f, -0.03367455f), new Keyframe(0.8199995f, -0.03168888f), new Keyframe(0.8299995f, -0.02971101f), new Keyframe(0.8399995f, -0.02774376f), new Keyframe(0.8499995f, -0.02578992f), new Keyframe(0.8599995f, -0.02385231f), new Keyframe(0.8699995f, -0.02193373f), new Keyframe(0.8799995f, -0.02003698f), new Keyframe(0.8899994f, -0.01816487f), new Keyframe(0.8999994f, -0.01632018f), new Keyframe(0.9099994f, -0.01450577f), new Keyframe(0.9199994f, -0.01272441f), new Keyframe(0.9299994f, -0.01097888f), new Keyframe(0.9399994f, -0.009272054f), new Keyframe(0.9499994f, -0.007606678f), new Keyframe(0.9599994f, -0.005985588f), new Keyframe(0.9699994f, -0.004411571f), new Keyframe(0.9799994f, -0.002887443f), new Keyframe(0.9899994f, -0.001416005f), new Keyframe(0.9999993f, -7.450581E-08f)
            );

            ThrowVisualUnderhandCurveX = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, 0.001810697f), new Keyframe(0.02f, 0.002448291f), new Keyframe(0.03f, 0.001997397f), new Keyframe(0.04f, 0.0005426306f), new Keyframe(0.05f, -0.001831393f), new Keyframe(0.05999999f, -0.005040056f), new Keyframe(0.06999999f, -0.008998747f), new Keyframe(0.07999999f, -0.01362285f), new Keyframe(0.08999999f, -0.01882775f), new Keyframe(0.09999999f, -0.02453342f), new Keyframe(0.11f, -0.03063369f), new Keyframe(0.12f, -0.03508093f), new Keyframe(0.13f, -0.03511371f), new Keyframe(0.14f, -0.03031639f), new Keyframe(0.15f, -0.02415645f), new Keyframe(0.16f, -0.01682964f), new Keyframe(0.17f, -0.008419836f), new Keyframe(0.18f, 0.0009890869f), new Keyframe(0.19f, 0.01131325f), new Keyframe(0.2f, 0.02246878f), new Keyframe(0.21f, 0.0343718f), new Keyframe(0.22f, 0.04693845f), new Keyframe(0.23f, 0.06008482f), new Keyframe(0.24f, 0.07372707f), new Keyframe(0.25f, 0.08778128f), new Keyframe(0.26f, 0.1021636f), new Keyframe(0.27f, 0.1167902f), new Keyframe(0.28f, 0.1315771f), new Keyframe(0.29f, 0.1464405f), new Keyframe(0.3f, 0.1612965f), new Keyframe(0.31f, 0.1760613f), new Keyframe(0.32f, 0.1906509f), new Keyframe(0.33f, 0.2049816f), new Keyframe(0.3399999f, 0.2189693f), new Keyframe(0.3499999f, 0.231978f), new Keyframe(0.3599999f, 0.2430236f), new Keyframe(0.3699999f, 0.2521778f), new Keyframe(0.3799999f, 0.2595338f), new Keyframe(0.3899999f, 0.2651844f), new Keyframe(0.3999999f, 0.2692229f), new Keyframe(0.4099999f, 0.2717423f), new Keyframe(0.4199999f, 0.2728357f), new Keyframe(0.4299999f, 0.2725961f), new Keyframe(0.4399998f, 0.2711166f), new Keyframe(0.4499998f, 0.2684903f), new Keyframe(0.4599998f, 0.2648103f), new Keyframe(0.4699998f, 0.2601696f), new Keyframe(0.4799998f, 0.2546613f), new Keyframe(0.4899998f, 0.2483786f), new Keyframe(0.4999998f, 0.2414143f), new Keyframe(0.5099998f, 0.2338617f), new Keyframe(0.5199998f, 0.2258138f), new Keyframe(0.5299998f, 0.2173637f), new Keyframe(0.5399998f, 0.2086044f), new Keyframe(0.5499998f, 0.199629f), new Keyframe(0.5599998f, 0.1905307f), new Keyframe(0.5699998f, 0.1814024f), new Keyframe(0.5799997f, 0.1723373f), new Keyframe(0.5899997f, 0.1634283f), new Keyframe(0.5999997f, 0.1547686f), new Keyframe(0.6099997f, 0.1464514f), new Keyframe(0.6199997f, 0.1385695f), new Keyframe(0.6299997f, 0.1312161f), new Keyframe(0.6399997f, 0.1244844f), new Keyframe(0.6499997f, 0.1182909f), new Keyframe(0.6599997f, 0.1122041f), new Keyframe(0.6699997f, 0.1062043f), new Keyframe(0.6799996f, 0.100298f), new Keyframe(0.6899996f, 0.09449176f), new Keyframe(0.6999996f, 0.0887922f), new Keyframe(0.7099996f, 0.08320588f), new Keyframe(0.7199996f, 0.07773937f), new Keyframe(0.7299996f, 0.07239924f), new Keyframe(0.7399996f, 0.06719206f), new Keyframe(0.7499996f, 0.06212441f), new Keyframe(0.7599996f, 0.05720284f), new Keyframe(0.7699996f, 0.05243395f), new Keyframe(0.7799996f, 0.04782429f), new Keyframe(0.7899995f, 0.04338043f), new Keyframe(0.7999995f, 0.03910896f), new Keyframe(0.8099995f, 0.03501646f), new Keyframe(0.8199995f, 0.03110947f), new Keyframe(0.8299995f, 0.02739457f), new Keyframe(0.8399995f, 0.02387834f), new Keyframe(0.8499995f, 0.02056735f), new Keyframe(0.8599995f, 0.01746818f), new Keyframe(0.8699995f, 0.01458739f), new Keyframe(0.8799995f, 0.01193154f), new Keyframe(0.8899994f, 0.009507224f), new Keyframe(0.8999994f, 0.007321008f), new Keyframe(0.9099994f, 0.005379461f), new Keyframe(0.9199994f, 0.003689155f), new Keyframe(0.9299994f, 0.002256647f), new Keyframe(0.9399994f, 0.001088545f), new Keyframe(0.9499994f, 0.0001913831f), new Keyframe(0.9599994f, -0.000428237f), new Keyframe(0.9699994f, -0.0007637739f), new Keyframe(0.9799994f, -0.0008086339f), new Keyframe(0.9899994f, -0.0005562529f), new Keyframe(0.9999993f, -6.705523E-08f)
            );

            ThrowVisualUnderhandCurveY = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, 2.776347E-05f), new Keyframe(0.02f, 0.0001206479f), new Keyframe(0.03f, 0.0002929796f), new Keyframe(0.04f, 0.0005590851f), new Keyframe(0.05f, 0.0009332909f), new Keyframe(0.05999999f, 0.001429923f), new Keyframe(0.06999999f, 0.002063308f), new Keyframe(0.07999999f, 0.002847773f), new Keyframe(0.08999999f, 0.003797644f), new Keyframe(0.09999999f, 0.004927247f), new Keyframe(0.11f, 0.006250909f), new Keyframe(0.12f, 0.007782956f), new Keyframe(0.13f, 0.009537715f), new Keyframe(0.14f, 0.01153968f), new Keyframe(0.15f, 0.01394965f), new Keyframe(0.16f, 0.01679166f), new Keyframe(0.17f, 0.02003028f), new Keyframe(0.18f, 0.02363011f), new Keyframe(0.19f, 0.02755572f), new Keyframe(0.2f, 0.03177169f), new Keyframe(0.21f, 0.0362426f), new Keyframe(0.22f, 0.04093304f), new Keyframe(0.23f, 0.04580759f), new Keyframe(0.24f, 0.05083083f), new Keyframe(0.25f, 0.05596733f), new Keyframe(0.26f, 0.06118168f), new Keyframe(0.27f, 0.06643847f), new Keyframe(0.28f, 0.07167383f), new Keyframe(0.29f, 0.07649778f), new Keyframe(0.3f, 0.08080909f), new Keyframe(0.31f, 0.08462422f), new Keyframe(0.32f, 0.08795962f), new Keyframe(0.33f, 0.09083176f), new Keyframe(0.3399999f, 0.09325711f), new Keyframe(0.3499999f, 0.09525213f), new Keyframe(0.3599999f, 0.09683327f), new Keyframe(0.3699999f, 0.09801699f), new Keyframe(0.3799999f, 0.09881977f), new Keyframe(0.3899999f, 0.09925805f), new Keyframe(0.3999999f, 0.09934831f), new Keyframe(0.4099999f, 0.099107f), new Keyframe(0.4199999f, 0.09855059f), new Keyframe(0.4299999f, 0.09769553f), new Keyframe(0.4399998f, 0.09655828f), new Keyframe(0.4499998f, 0.09515533f), new Keyframe(0.4599998f, 0.0935031f), new Keyframe(0.4699998f, 0.09161809f), new Keyframe(0.4799998f, 0.08951674f), new Keyframe(0.4899998f, 0.0872155f), new Keyframe(0.4999998f, 0.08473087f), new Keyframe(0.5099998f, 0.08207927f), new Keyframe(0.5199998f, 0.07927719f), new Keyframe(0.5299998f, 0.07634109f), new Keyframe(0.5399998f, 0.07328741f), new Keyframe(0.5499998f, 0.07013264f), new Keyframe(0.5599998f, 0.06689321f), new Keyframe(0.5699998f, 0.0635856f), new Keyframe(0.5799997f, 0.06022627f), new Keyframe(0.5899997f, 0.0568317f), new Keyframe(0.5999997f, 0.05341832f), new Keyframe(0.6099997f, 0.05000261f), new Keyframe(0.6199997f, 0.046601f), new Keyframe(0.6299997f, 0.04323001f), new Keyframe(0.6399997f, 0.03990604f), new Keyframe(0.6499997f, 0.03664561f), new Keyframe(0.6599997f, 0.03346514f), new Keyframe(0.6699997f, 0.0303811f), new Keyframe(0.6799996f, 0.02740996f), new Keyframe(0.6899996f, 0.02457382f), new Keyframe(0.6999996f, 0.0219612f), new Keyframe(0.7099996f, 0.01958843f), new Keyframe(0.7199996f, 0.01744326f), new Keyframe(0.7299996f, 0.01551343f), new Keyframe(0.7399996f, 0.01378667f), new Keyframe(0.7499996f, 0.01225072f), new Keyframe(0.7599996f, 0.01089332f), new Keyframe(0.7699996f, 0.009702204f), new Keyframe(0.7799996f, 0.008665115f), new Keyframe(0.7899995f, 0.007769793f), new Keyframe(0.7999995f, 0.007003974f), new Keyframe(0.8099995f, 0.006355396f), new Keyframe(0.8199995f, 0.005811796f), new Keyframe(0.8299995f, 0.005360913f), new Keyframe(0.8399995f, 0.004990485f), new Keyframe(0.8499995f, 0.004688254f), new Keyframe(0.8599995f, 0.004441952f), new Keyframe(0.8699995f, 0.004239323f), new Keyframe(0.8799995f, 0.004068097f), new Keyframe(0.8899994f, 0.003916027f), new Keyframe(0.8999994f, 0.003770838f), new Keyframe(0.9099994f, 0.003620273f), new Keyframe(0.9199994f, 0.003452064f), new Keyframe(0.9299994f, 0.003253961f), new Keyframe(0.9399994f, 0.003013697f), new Keyframe(0.9499994f, 0.002719009f), new Keyframe(0.9599994f, 0.002357632f), new Keyframe(0.9699994f, 0.001917314f), new Keyframe(0.9799994f, 0.001385782f), new Keyframe(0.9899994f, 0.0007507838f), new Keyframe(0.9999993f, 5.029142E-08f)
            );

            WeightAttenuationCurve = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, 0.01004331f), new Keyframe(0.02f, 0.02008907f), new Keyframe(0.03f, 0.03013724f), new Keyframe(0.04f, 0.04018782f), new Keyframe(0.05f, 0.05024078f), new Keyframe(0.05999999f, 0.06029612f), new Keyframe(0.06999999f, 0.07035381f), new Keyframe(0.07999999f, 0.08041386f), new Keyframe(0.08999999f, 0.09047622f), new Keyframe(0.09999999f, 0.1005409f), new Keyframe(0.11f, 0.1106079f), new Keyframe(0.12f, 0.1206771f), new Keyframe(0.13f, 0.1307486f), new Keyframe(0.14f, 0.1408224f), new Keyframe(0.15f, 0.1508984f), new Keyframe(0.16f, 0.1609766f), new Keyframe(0.17f, 0.171057f), new Keyframe(0.18f, 0.1811396f), new Keyframe(0.19f, 0.1912244f), new Keyframe(0.2f, 0.2013113f), new Keyframe(0.21f, 0.2114004f), new Keyframe(0.22f, 0.2214916f), new Keyframe(0.23f, 0.2315849f), new Keyframe(0.24f, 0.2416803f), new Keyframe(0.25f, 0.2517777f), new Keyframe(0.26f, 0.2618772f), new Keyframe(0.27f, 0.2719788f), new Keyframe(0.28f, 0.2820823f), new Keyframe(0.29f, 0.2921879f), new Keyframe(0.3f, 0.3022955f), new Keyframe(0.31f, 0.3124051f), new Keyframe(0.32f, 0.3225166f), new Keyframe(0.33f, 0.3326301f), new Keyframe(0.3399999f, 0.3427455f), new Keyframe(0.3499999f, 0.3528628f), new Keyframe(0.3599999f, 0.362982f), new Keyframe(0.3699999f, 0.3731032f), new Keyframe(0.3799999f, 0.3832261f), new Keyframe(0.3899999f, 0.393351f), new Keyframe(0.3999999f, 0.4034776f), new Keyframe(0.4099999f, 0.4136062f), new Keyframe(0.4199999f, 0.4237365f), new Keyframe(0.4299999f, 0.4338686f), new Keyframe(0.4399998f, 0.4440025f), new Keyframe(0.4499998f, 0.4541381f), new Keyframe(0.4599998f, 0.4642755f), new Keyframe(0.4699998f, 0.4744146f), new Keyframe(0.4799998f, 0.4845554f), new Keyframe(0.4899998f, 0.494698f), new Keyframe(0.4999998f, 0.5048422f), new Keyframe(0.5099998f, 0.5149881f), new Keyframe(0.5199998f, 0.5251356f), new Keyframe(0.5299998f, 0.5352848f), new Keyframe(0.5399998f, 0.5454355f), new Keyframe(0.5499998f, 0.5555879f), new Keyframe(0.5599998f, 0.5657419f), new Keyframe(0.5699998f, 0.5758974f), new Keyframe(0.5799997f, 0.5860545f), new Keyframe(0.5899997f, 0.5962132f), new Keyframe(0.5999997f, 0.6063733f), new Keyframe(0.6099997f, 0.616535f), new Keyframe(0.6199997f, 0.6266981f), new Keyframe(0.6299997f, 0.6368628f), new Keyframe(0.6399997f, 0.6470289f), new Keyframe(0.6499997f, 0.6571964f), new Keyframe(0.6599997f, 0.6673654f), new Keyframe(0.6699997f, 0.6775358f), new Keyframe(0.6799996f, 0.6877076f), new Keyframe(0.6899996f, 0.6978807f), new Keyframe(0.6999996f, 0.7080553f), new Keyframe(0.7099996f, 0.7182312f), new Keyframe(0.7199996f, 0.7284085f), new Keyframe(0.7299996f, 0.738587f), new Keyframe(0.7399996f, 0.7487668f), new Keyframe(0.7499996f, 0.758948f), new Keyframe(0.7599996f, 0.7691304f), new Keyframe(0.7699996f, 0.779314f), new Keyframe(0.7799996f, 0.789499f), new Keyframe(0.7899995f, 0.7996851f), new Keyframe(0.7999995f, 0.8098724f), new Keyframe(0.8099995f, 0.820061f), new Keyframe(0.8199995f, 0.8302507f), new Keyframe(0.8299995f, 0.8404416f), new Keyframe(0.8399995f, 0.8506336f), new Keyframe(0.8499995f, 0.8608268f), new Keyframe(0.8599995f, 0.8710211f), new Keyframe(0.8699995f, 0.8812165f), new Keyframe(0.8799995f, 0.8914129f), new Keyframe(0.8899994f, 0.9016104f), new Keyframe(0.8999994f, 0.9118091f), new Keyframe(0.9099994f, 0.9220086f), new Keyframe(0.9199994f, 0.9322093f), new Keyframe(0.9299994f, 0.9424109f), new Keyframe(0.9399994f, 0.9526136f), new Keyframe(0.9499994f, 0.9628173f), new Keyframe(0.9599994f, 0.9730218f), new Keyframe(0.9699994f, 0.9832273f), new Keyframe(0.9799994f, 0.9934338f), new Keyframe(0.9899994f, 1.003641f), new Keyframe(0.9999993f, 1.013849f), new Keyframe(1.009999f, 1.024059f), new Keyframe(1.019999f, 1.034268f), new Keyframe(1.029999f, 1.044479f), new Keyframe(1.039999f, 1.054691f), new Keyframe(1.049999f, 1.064904f), new Keyframe(1.059999f, 1.075117f), new Keyframe(1.069999f, 1.085331f), new Keyframe(1.079999f, 1.095546f), new Keyframe(1.089999f, 1.105761f), new Keyframe(1.099999f, 1.115978f), new Keyframe(1.109999f, 1.126195f), new Keyframe(1.119999f, 1.136413f), new Keyframe(1.129999f, 1.146631f), new Keyframe(1.139999f, 1.15685f), new Keyframe(1.149999f, 1.16707f), new Keyframe(1.159999f, 1.17729f), new Keyframe(1.169999f, 1.187511f), new Keyframe(1.179999f, 1.197733f), new Keyframe(1.189999f, 1.207955f), new Keyframe(1.199999f, 1.218178f), new Keyframe(1.209999f, 1.228402f), new Keyframe(1.219999f, 1.238626f), new Keyframe(1.229999f, 1.24885f), new Keyframe(1.239999f, 1.259075f), new Keyframe(1.249999f, 1.269301f), new Keyframe(1.259999f, 1.279527f), new Keyframe(1.269999f, 1.289753f), new Keyframe(1.279999f, 1.29998f), new Keyframe(1.289999f, 1.310207f), new Keyframe(1.299999f, 1.320435f), new Keyframe(1.309999f, 1.330664f), new Keyframe(1.319999f, 1.340892f), new Keyframe(1.329999f, 1.351121f), new Keyframe(1.339999f, 1.36135f), new Keyframe(1.349999f, 1.37158f), new Keyframe(1.359999f, 1.38181f), new Keyframe(1.369999f, 1.392041f), new Keyframe(1.379999f, 1.402271f), new Keyframe(1.389999f, 1.412502f), new Keyframe(1.399999f, 1.422733f), new Keyframe(1.409999f, 1.432965f), new Keyframe(1.419999f, 1.443196f), new Keyframe(1.429999f, 1.453428f), new Keyframe(1.439999f, 1.46366f), new Keyframe(1.449999f, 1.473893f), new Keyframe(1.459999f, 1.484125f), new Keyframe(1.469999f, 1.494358f), new Keyframe(1.479999f, 1.50459f), new Keyframe(1.489999f, 1.514823f), new Keyframe(1.499999f, 1.525056f), new Keyframe(1.509999f, 1.535289f), new Keyframe(1.519999f, 1.545523f), new Keyframe(1.529999f, 1.555756f), new Keyframe(1.539999f, 1.565989f), new Keyframe(1.549999f, 1.576222f), new Keyframe(1.559999f, 1.586456f), new Keyframe(1.569999f, 1.596689f), new Keyframe(1.579999f, 1.606922f), new Keyframe(1.589999f, 1.617156f), new Keyframe(1.599999f, 1.627389f), new Keyframe(1.609999f, 1.637622f), new Keyframe(1.619999f, 1.647855f), new Keyframe(1.629999f, 1.658088f), new Keyframe(1.639999f, 1.668321f), new Keyframe(1.649999f, 1.678553f), new Keyframe(1.659999f, 1.688786f), new Keyframe(1.669999f, 1.699019f), new Keyframe(1.679999f, 1.709251f), new Keyframe(1.689999f, 1.719483f), new Keyframe(1.699999f, 1.729715f), new Keyframe(1.709999f, 1.739946f), new Keyframe(1.719999f, 1.750178f), new Keyframe(1.729999f, 1.760409f), new Keyframe(1.739999f, 1.77064f), new Keyframe(1.749999f, 1.780871f), new Keyframe(1.759999f, 1.791101f), new Keyframe(1.769999f, 1.801331f), new Keyframe(1.779999f, 1.81156f), new Keyframe(1.789999f, 1.82179f), new Keyframe(1.799999f, 1.832019f), new Keyframe(1.809999f, 1.842247f), new Keyframe(1.819999f, 1.852476f), new Keyframe(1.829999f, 1.862703f), new Keyframe(1.839999f, 1.872931f), new Keyframe(1.849999f, 1.883157f), new Keyframe(1.859999f, 1.893384f), new Keyframe(1.869999f, 1.90361f), new Keyframe(1.879999f, 1.913835f), new Keyframe(1.889999f, 1.92406f), new Keyframe(1.899999f, 1.934285f), new Keyframe(1.909999f, 1.944509f), new Keyframe(1.919999f, 1.954732f), new Keyframe(1.929999f, 1.964955f), new Keyframe(1.939999f, 1.975177f), new Keyframe(1.949998f, 1.985399f), new Keyframe(1.959998f, 1.99562f), new Keyframe(1.969998f, 2.00584f), new Keyframe(1.979998f, 2.01606f), new Keyframe(1.989998f, 2.026279f), new Keyframe(1.999998f, 2.036497f), new Keyframe(2.009999f, 2.046715f), new Keyframe(2.019999f, 2.056932f), new Keyframe(2.029999f, 2.067149f), new Keyframe(2.039999f, 2.077364f), new Keyframe(2.049999f, 2.087579f), new Keyframe(2.059999f, 2.097793f), new Keyframe(2.069999f, 2.108006f), new Keyframe(2.079998f, 2.118219f), new Keyframe(2.089998f, 2.12843f), new Keyframe(2.099998f, 2.138641f), new Keyframe(2.109998f, 2.148851f), new Keyframe(2.119998f, 2.15906f), new Keyframe(2.129998f, 2.169268f), new Keyframe(2.139998f, 2.179475f), new Keyframe(2.149998f, 2.189682f), new Keyframe(2.159998f, 2.199887f), new Keyframe(2.169998f, 2.210092f), new Keyframe(2.179998f, 2.220295f), new Keyframe(2.189998f, 2.230498f), new Keyframe(2.199998f, 2.240699f), new Keyframe(2.209998f, 2.2509f), new Keyframe(2.219998f, 2.2611f), new Keyframe(2.229998f, 2.271298f), new Keyframe(2.239998f, 2.281495f), new Keyframe(2.249998f, 2.291692f), new Keyframe(2.259998f, 2.301887f), new Keyframe(2.269998f, 2.312081f), new Keyframe(2.279998f, 2.322274f), new Keyframe(2.289998f, 2.332466f), new Keyframe(2.299998f, 2.342657f), new Keyframe(2.309998f, 2.352847f), new Keyframe(2.319998f, 2.363035f), new Keyframe(2.329998f, 2.373223f), new Keyframe(2.339998f, 2.383408f), new Keyframe(2.349998f, 2.393593f), new Keyframe(2.359998f, 2.403777f), new Keyframe(2.369998f, 2.413959f), new Keyframe(2.379998f, 2.42414f), new Keyframe(2.389998f, 2.43432f), new Keyframe(2.399998f, 2.444499f), new Keyframe(2.409998f, 2.454676f), new Keyframe(2.419998f, 2.464851f), new Keyframe(2.429998f, 2.475026f), new Keyframe(2.439998f, 2.485199f), new Keyframe(2.449998f, 2.495371f), new Keyframe(2.459998f, 2.505541f), new Keyframe(2.469998f, 2.515709f), new Keyframe(2.479998f, 2.525877f), new Keyframe(2.489998f, 2.536043f), new Keyframe(2.499998f, 2.546207f), new Keyframe(2.509998f, 2.556371f), new Keyframe(2.519998f, 2.566532f), new Keyframe(2.529998f, 2.576692f), new Keyframe(2.539998f, 2.586851f), new Keyframe(2.549998f, 2.597008f), new Keyframe(2.559998f, 2.607163f), new Keyframe(2.569998f, 2.617317f), new Keyframe(2.579998f, 2.627469f), new Keyframe(2.589998f, 2.63762f), new Keyframe(2.599998f, 2.647769f), new Keyframe(2.609998f, 2.657916f), new Keyframe(2.619998f, 2.668062f), new Keyframe(2.629998f, 2.678206f), new Keyframe(2.639998f, 2.688349f), new Keyframe(2.649998f, 2.698489f), new Keyframe(2.659998f, 2.708628f), new Keyframe(2.669998f, 2.718765f), new Keyframe(2.679998f, 2.728901f), new Keyframe(2.689998f, 2.739035f), new Keyframe(2.699998f, 2.749167f), new Keyframe(2.709998f, 2.759297f), new Keyframe(2.719998f, 2.769425f), new Keyframe(2.729998f, 2.779552f), new Keyframe(2.739998f, 2.789676f), new Keyframe(2.749998f, 2.799799f), new Keyframe(2.759998f, 2.80992f), new Keyframe(2.769998f, 2.820039f), new Keyframe(2.779998f, 2.830157f), new Keyframe(2.789998f, 2.840272f), new Keyframe(2.799998f, 2.850385f), new Keyframe(2.809998f, 2.860496f), new Keyframe(2.819998f, 2.870606f), new Keyframe(2.829998f, 2.880713f), new Keyframe(2.839998f, 2.890818f), new Keyframe(2.849998f, 2.900922f), new Keyframe(2.859998f, 2.911023f), new Keyframe(2.869998f, 2.921123f), new Keyframe(2.879998f, 2.93122f), new Keyframe(2.889998f, 2.941315f), new Keyframe(2.899998f, 2.951408f), new Keyframe(2.909998f, 2.961499f), new Keyframe(2.919998f, 2.971588f), new Keyframe(2.929998f, 2.981675f), new Keyframe(2.939998f, 2.991759f), new Keyframe(2.949998f, 3.001842f), new Keyframe(2.959998f, 3.011922f), new Keyframe(2.969998f, 3.022f), new Keyframe(2.979998f, 3.032076f), new Keyframe(2.989998f, 3.04215f), new Keyframe(2.999998f, 3.052221f), new Keyframe(3.009998f, 3.06229f), new Keyframe(3.019998f, 3.072357f), new Keyframe(3.029998f, 3.082421f), new Keyframe(3.039998f, 3.092483f), new Keyframe(3.049998f, 3.102543f), new Keyframe(3.059998f, 3.112601f), new Keyframe(3.069998f, 3.122656f), new Keyframe(3.079998f, 3.132708f), new Keyframe(3.089998f, 3.142759f), new Keyframe(3.099998f, 3.152807f), new Keyframe(3.109998f, 3.162853f), new Keyframe(3.119998f, 3.172896f), new Keyframe(3.129997f, 3.182936f), new Keyframe(3.139997f, 3.192975f), new Keyframe(3.149997f, 3.20301f), new Keyframe(3.159997f, 3.213043f), new Keyframe(3.169997f, 3.223074f), new Keyframe(3.179997f, 3.233102f), new Keyframe(3.189997f, 3.243128f), new Keyframe(3.199997f, 3.253151f), new Keyframe(3.209997f, 3.263171f), new Keyframe(3.219997f, 3.273189f), new Keyframe(3.229997f, 3.283204f), new Keyframe(3.239997f, 3.293217f), new Keyframe(3.249997f, 3.303227f), new Keyframe(3.259997f, 3.313234f), new Keyframe(3.269997f, 3.323239f), new Keyframe(3.279997f, 3.333241f), new Keyframe(3.289997f, 3.34324f), new Keyframe(3.299997f, 3.353237f), new Keyframe(3.309997f, 3.36323f), new Keyframe(3.319997f, 3.373221f), new Keyframe(3.329997f, 3.38321f), new Keyframe(3.339997f, 3.393195f), new Keyframe(3.349997f, 3.403178f), new Keyframe(3.359997f, 3.413158f), new Keyframe(3.369997f, 3.423135f), new Keyframe(3.379997f, 3.433109f), new Keyframe(3.389997f, 3.44308f), new Keyframe(3.399997f, 3.453049f), new Keyframe(3.409997f, 3.463015f), new Keyframe(3.419997f, 3.472977f), new Keyframe(3.429997f, 3.482937f), new Keyframe(3.439997f, 3.492894f), new Keyframe(3.449997f, 3.502848f), new Keyframe(3.459997f, 3.512798f), new Keyframe(3.469997f, 3.522746f), new Keyframe(3.479997f, 3.532691f), new Keyframe(3.489997f, 3.542633f), new Keyframe(3.499997f, 3.552572f), new Keyframe(3.509997f, 3.562508f), new Keyframe(3.519997f, 3.57244f), new Keyframe(3.529997f, 3.58237f), new Keyframe(3.539997f, 3.592296f), new Keyframe(3.549997f, 3.60222f), new Keyframe(3.559997f, 3.61214f), new Keyframe(3.569997f, 3.622057f), new Keyframe(3.579997f, 3.631971f), new Keyframe(3.589997f, 3.641882f), new Keyframe(3.599997f, 3.651789f), new Keyframe(3.609997f, 3.661694f), new Keyframe(3.619997f, 3.671595f), new Keyframe(3.629997f, 3.681493f), new Keyframe(3.639997f, 3.691388f), new Keyframe(3.649997f, 3.701279f), new Keyframe(3.659997f, 3.711167f), new Keyframe(3.669997f, 3.721052f), new Keyframe(3.679997f, 3.730934f), new Keyframe(3.689997f, 3.740812f), new Keyframe(3.699997f, 3.750686f), new Keyframe(3.709997f, 3.760558f), new Keyframe(3.719997f, 3.770426f), new Keyframe(3.729997f, 3.780291f), new Keyframe(3.739997f, 3.790152f), new Keyframe(3.749997f, 3.80001f), new Keyframe(3.759997f, 3.809864f), new Keyframe(3.769997f, 3.819715f), new Keyframe(3.779997f, 3.829563f), new Keyframe(3.789997f, 3.839406f), new Keyframe(3.799997f, 3.849247f), new Keyframe(3.809997f, 3.859084f), new Keyframe(3.819997f, 3.868917f), new Keyframe(3.829997f, 3.878747f), new Keyframe(3.839997f, 3.888573f), new Keyframe(3.849997f, 3.898396f), new Keyframe(3.859997f, 3.908215f), new Keyframe(3.869997f, 3.918031f), new Keyframe(3.879997f, 3.927842f), new Keyframe(3.889997f, 3.93765f), new Keyframe(3.899997f, 3.947455f), new Keyframe(3.909997f, 3.957256f), new Keyframe(3.919997f, 3.967053f), new Keyframe(3.929997f, 3.976846f), new Keyframe(3.939997f, 3.986636f), new Keyframe(3.949997f, 3.996422f), new Keyframe(3.959997f, 4.006204f), new Keyframe(3.969997f, 4.015983f), new Keyframe(3.979997f, 4.025757f), new Keyframe(3.989997f, 4.035528f), new Keyframe(3.999997f, 4.045295f), new Keyframe(4.009997f, 4.055058f), new Keyframe(4.019997f, 4.064818f), new Keyframe(4.029997f, 4.074574f), new Keyframe(4.039998f, 4.084325f), new Keyframe(4.049998f, 4.094073f), new Keyframe(4.059998f, 4.103817f), new Keyframe(4.069998f, 4.113557f), new Keyframe(4.079998f, 4.123293f), new Keyframe(4.089999f, 4.133026f), new Keyframe(4.099999f, 4.142754f), new Keyframe(4.109999f, 4.152478f), new Keyframe(4.119999f, 4.162198f), new Keyframe(4.13f, 4.171914f), new Keyframe(4.14f, 4.181626f), new Keyframe(4.15f, 4.191334f), new Keyframe(4.16f, 4.201038f), new Keyframe(4.170001f, 4.210738f), new Keyframe(4.180001f, 4.220434f), new Keyframe(4.190001f, 4.230125f), new Keyframe(4.200001f, 4.239813f), new Keyframe(4.210001f, 4.249496f), new Keyframe(4.220002f, 4.259176f), new Keyframe(4.230002f, 4.268851f), new Keyframe(4.240002f, 4.278522f), new Keyframe(4.250002f, 4.288188f), new Keyframe(4.260003f, 4.29785f), new Keyframe(4.270003f, 4.307508f), new Keyframe(4.280003f, 4.317162f), new Keyframe(4.290003f, 4.326812f), new Keyframe(4.300004f, 4.336457f), new Keyframe(4.310004f, 4.346098f), new Keyframe(4.320004f, 4.355735f), new Keyframe(4.330004f, 4.365367f), new Keyframe(4.340004f, 4.374995f), new Keyframe(4.350005f, 4.384619f), new Keyframe(4.360005f, 4.394238f), new Keyframe(4.370005f, 4.403852f), new Keyframe(4.380005f, 4.413463f), new Keyframe(4.390006f, 4.423069f), new Keyframe(4.400006f, 4.432671f), new Keyframe(4.410006f, 4.442267f), new Keyframe(4.420006f, 4.45186f), new Keyframe(4.430007f, 4.461448f), new Keyframe(4.440007f, 4.471031f), new Keyframe(4.450007f, 4.48061f), new Keyframe(4.460007f, 4.490184f), new Keyframe(4.470007f, 4.499755f), new Keyframe(4.480008f, 4.50932f), new Keyframe(4.490008f, 4.51888f), new Keyframe(4.500008f, 4.528436f), new Keyframe(4.510008f, 4.537988f), new Keyframe(4.520009f, 4.547535f), new Keyframe(4.530009f, 4.557077f), new Keyframe(4.540009f, 4.566615f), new Keyframe(4.550009f, 4.576148f), new Keyframe(4.560009f, 4.585675f), new Keyframe(4.57001f, 4.595199f), new Keyframe(4.58001f, 4.604718f), new Keyframe(4.59001f, 4.614231f), new Keyframe(4.60001f, 4.623741f), new Keyframe(4.610011f, 4.633245f), new Keyframe(4.620011f, 4.642745f), new Keyframe(4.630011f, 4.652239f), new Keyframe(4.640011f, 4.661729f), new Keyframe(4.650012f, 4.671215f), new Keyframe(4.660012f, 4.680695f), new Keyframe(4.670012f, 4.69017f), new Keyframe(4.680012f, 4.69964f), new Keyframe(4.690012f, 4.709106f), new Keyframe(4.700013f, 4.718566f), new Keyframe(4.710013f, 4.728022f), new Keyframe(4.720013f, 4.737473f), new Keyframe(4.730013f, 4.746919f), new Keyframe(4.740014f, 4.75636f), new Keyframe(4.750014f, 4.765796f), new Keyframe(4.760014f, 4.775227f), new Keyframe(4.770014f, 4.784652f), new Keyframe(4.780015f, 4.794073f), new Keyframe(4.790015f, 4.803488f), new Keyframe(4.800015f, 4.812899f), new Keyframe(4.810015f, 4.822304f), new Keyframe(4.820015f, 4.831704f), new Keyframe(4.830016f, 4.8411f), new Keyframe(4.840016f, 4.85049f), new Keyframe(4.850016f, 4.859875f), new Keyframe(4.860016f, 4.869255f), new Keyframe(4.870017f, 4.878629f), new Keyframe(4.880017f, 4.887998f), new Keyframe(4.890017f, 4.897363f), new Keyframe(4.900017f, 4.906721f), new Keyframe(4.910017f, 4.916075f), new Keyframe(4.920018f, 4.925424f), new Keyframe(4.930018f, 4.934767f), new Keyframe(4.940018f, 4.944105f), new Keyframe(4.950018f, 4.953437f), new Keyframe(4.960019f, 4.962764f), new Keyframe(4.970019f, 4.972085f), new Keyframe(4.980019f, 4.981402f), new Keyframe(4.990019f, 4.990713f), new Keyframe(5.00002f, 5.000017f), new Keyframe(5.01002f, 5.008844f), new Keyframe(5.02002f, 5.017635f), new Keyframe(5.03002f, 5.02639f), new Keyframe(5.04002f, 5.035109f), new Keyframe(5.050021f, 5.043793f), new Keyframe(5.060021f, 5.052441f), new Keyframe(5.070021f, 5.061054f), new Keyframe(5.080021f, 5.069631f), new Keyframe(5.090022f, 5.078173f), new Keyframe(5.100022f, 5.086679f), new Keyframe(5.110022f, 5.095151f), new Keyframe(5.120022f, 5.103588f), new Keyframe(5.130023f, 5.111989f), new Keyframe(5.140023f, 5.120356f), new Keyframe(5.150023f, 5.128687f), new Keyframe(5.160023f, 5.136984f), new Keyframe(5.170023f, 5.145246f), new Keyframe(5.180024f, 5.153473f), new Keyframe(5.190024f, 5.161666f), new Keyframe(5.200024f, 5.169824f), new Keyframe(5.210024f, 5.177948f), new Keyframe(5.220025f, 5.186038f), new Keyframe(5.230025f, 5.194093f), new Keyframe(5.240025f, 5.202114f), new Keyframe(5.250025f, 5.210101f), new Keyframe(5.260026f, 5.218054f), new Keyframe(5.270026f, 5.225973f), new Keyframe(5.280026f, 5.233858f), new Keyframe(5.290026f, 5.241709f), new Keyframe(5.300026f, 5.249526f), new Keyframe(5.310027f, 5.25731f), new Keyframe(5.320027f, 5.26506f), new Keyframe(5.330027f, 5.272777f), new Keyframe(5.340027f, 5.28046f), new Keyframe(5.350028f, 5.28811f), new Keyframe(5.360028f, 5.295726f), new Keyframe(5.370028f, 5.303309f), new Keyframe(5.380028f, 5.31086f), new Keyframe(5.390028f, 5.318377f), new Keyframe(5.400029f, 5.325861f), new Keyframe(5.410029f, 5.333312f), new Keyframe(5.420029f, 5.340731f), new Keyframe(5.430029f, 5.348116f), new Keyframe(5.44003f, 5.355469f), new Keyframe(5.45003f, 5.362789f), new Keyframe(5.46003f, 5.370077f), new Keyframe(5.47003f, 5.377332f), new Keyframe(5.480031f, 5.384555f), new Keyframe(5.490031f, 5.391746f), new Keyframe(5.500031f, 5.398904f), new Keyframe(5.510031f, 5.40603f), new Keyframe(5.520031f, 5.413124f), new Keyframe(5.530032f, 5.420186f), new Keyframe(5.540032f, 5.427216f), new Keyframe(5.550032f, 5.434214f), new Keyframe(5.560032f, 5.441181f), new Keyframe(5.570033f, 5.448115f), new Keyframe(5.580033f, 5.455019f), new Keyframe(5.590033f, 5.46189f), new Keyframe(5.600033f, 5.46873f), new Keyframe(5.610034f, 5.475539f), new Keyframe(5.620034f, 5.482316f), new Keyframe(5.630034f, 5.489062f), new Keyframe(5.640034f, 5.495777f), new Keyframe(5.650034f, 5.50246f), new Keyframe(5.660035f, 5.509113f), new Keyframe(5.670035f, 5.515735f), new Keyframe(5.680035f, 5.522326f), new Keyframe(5.690035f, 5.528886f), new Keyframe(5.700036f, 5.535415f), new Keyframe(5.710036f, 5.541914f), new Keyframe(5.720036f, 5.548382f), new Keyframe(5.730036f, 5.554819f), new Keyframe(5.740036f, 5.561226f), new Keyframe(5.750037f, 5.567603f), new Keyframe(5.760037f, 5.57395f), new Keyframe(5.770037f, 5.580266f), new Keyframe(5.780037f, 5.586552f), new Keyframe(5.790038f, 5.592808f), new Keyframe(5.800038f, 5.599034f), new Keyframe(5.810038f, 5.60523f), new Keyframe(5.820038f, 5.611397f), new Keyframe(5.830039f, 5.617533f), new Keyframe(5.840039f, 5.62364f), new Keyframe(5.850039f, 5.629718f), new Keyframe(5.860039f, 5.635766f), new Keyframe(5.870039f, 5.641784f), new Keyframe(5.88004f, 5.647773f), new Keyframe(5.89004f, 5.653732f), new Keyframe(5.90004f, 5.659663f), new Keyframe(5.91004f, 5.665564f), new Keyframe(5.920041f, 5.671436f), new Keyframe(5.930041f, 5.677279f), new Keyframe(5.940041f, 5.683094f), new Keyframe(5.950041f, 5.688879f), new Keyframe(5.960042f, 5.694635f), new Keyframe(5.970042f, 5.700364f), new Keyframe(5.980042f, 5.706063f), new Keyframe(5.990042f, 5.711734f), new Keyframe(6.000042f, 5.717376f), new Keyframe(6.010043f, 5.72299f), new Keyframe(6.020043f, 5.728575f), new Keyframe(6.030043f, 5.734133f), new Keyframe(6.040043f, 5.739662f), new Keyframe(6.050044f, 5.745163f), new Keyframe(6.060044f, 5.750636f), new Keyframe(6.070044f, 5.756081f), new Keyframe(6.080044f, 5.761498f), new Keyframe(6.090044f, 5.766888f), new Keyframe(6.100045f, 5.772249f), new Keyframe(6.110045f, 5.777583f), new Keyframe(6.120045f, 5.782889f), new Keyframe(6.130045f, 5.788168f), new Keyframe(6.140046f, 5.79342f), new Keyframe(6.150046f, 5.798644f), new Keyframe(6.160046f, 5.803841f), new Keyframe(6.170046f, 5.809011f), new Keyframe(6.180047f, 5.814154f), new Keyframe(6.190047f, 5.819269f), new Keyframe(6.200047f, 5.824358f), new Keyframe(6.210047f, 5.829419f), new Keyframe(6.220047f, 5.834454f), new Keyframe(6.230048f, 5.839462f), new Keyframe(6.240048f, 5.844444f), new Keyframe(6.250048f, 5.849399f), new Keyframe(6.260048f, 5.854327f), new Keyframe(6.270049f, 5.859229f), new Keyframe(6.280049f, 5.864104f), new Keyframe(6.290049f, 5.868954f), new Keyframe(6.300049f, 5.873777f), new Keyframe(6.31005f, 5.878573f), new Keyframe(6.32005f, 5.883345f), new Keyframe(6.33005f, 5.888089f), new Keyframe(6.34005f, 5.892808f), new Keyframe(6.35005f, 5.897501f), new Keyframe(6.360051f, 5.902169f), new Keyframe(6.370051f, 5.90681f), new Keyframe(6.380051f, 5.911427f), new Keyframe(6.390051f, 5.916017f), new Keyframe(6.400052f, 5.920582f), new Keyframe(6.410052f, 5.925121f), new Keyframe(6.420052f, 5.929636f), new Keyframe(6.430052f, 5.934125f), new Keyframe(6.440053f, 5.938589f), new Keyframe(6.450053f, 5.943027f), new Keyframe(6.460053f, 5.947442f), new Keyframe(6.470053f, 5.95183f), new Keyframe(6.480053f, 5.956194f), new Keyframe(6.490054f, 5.960534f), new Keyframe(6.500054f, 5.964849f), new Keyframe(6.510054f, 5.969138f), new Keyframe(6.520054f, 5.973403f), new Keyframe(6.530055f, 5.977644f), new Keyframe(6.540055f, 5.981861f), new Keyframe(6.550055f, 5.986053f), new Keyframe(6.560055f, 5.990221f), new Keyframe(6.570055f, 5.994365f), new Keyframe(6.580056f, 5.998484f), new Keyframe(6.590056f, 6.00258f), new Keyframe(6.600056f, 6.006651f), new Keyframe(6.610056f, 6.010699f), new Keyframe(6.620057f, 6.014723f), new Keyframe(6.630057f, 6.018723f), new Keyframe(6.640057f, 6.022699f), new Keyframe(6.650057f, 6.026652f), new Keyframe(6.660058f, 6.030582f), new Keyframe(6.670058f, 6.034488f), new Keyframe(6.680058f, 6.038371f), new Keyframe(6.690058f, 6.04223f), new Keyframe(6.700058f, 6.046066f), new Keyframe(6.710059f, 6.049879f), new Keyframe(6.720059f, 6.053669f), new Keyframe(6.730059f, 6.057436f), new Keyframe(6.740059f, 6.061181f), new Keyframe(6.75006f, 6.064902f), new Keyframe(6.76006f, 6.0686f), new Keyframe(6.77006f, 6.072276f), new Keyframe(6.78006f, 6.07593f), new Keyframe(6.790061f, 6.07956f), new Keyframe(6.800061f, 6.083169f), new Keyframe(6.810061f, 6.086754f), new Keyframe(6.820061f, 6.090318f), new Keyframe(6.830061f, 6.093859f), new Keyframe(6.840062f, 6.097379f), new Keyframe(6.850062f, 6.100876f), new Keyframe(6.860062f, 6.104351f), new Keyframe(6.870062f, 6.107804f), new Keyframe(6.880063f, 6.111236f), new Keyframe(6.890063f, 6.114645f), new Keyframe(6.900063f, 6.118033f), new Keyframe(6.910063f, 6.121399f), new Keyframe(6.920063f, 6.124744f), new Keyframe(6.930064f, 6.128067f), new Keyframe(6.940064f, 6.13137f), new Keyframe(6.950064f, 6.13465f), new Keyframe(6.960064f, 6.137909f), new Keyframe(6.970065f, 6.141148f), new Keyframe(6.980065f, 6.144364f), new Keyframe(6.990065f, 6.147561f), new Keyframe(7.000065f, 6.150736f), new Keyframe(7.010066f, 6.15389f), new Keyframe(7.020066f, 6.157023f), new Keyframe(7.030066f, 6.160136f), new Keyframe(7.040066f, 6.163228f), new Keyframe(7.050066f, 6.1663f), new Keyframe(7.060067f, 6.169351f), new Keyframe(7.070067f, 6.172381f), new Keyframe(7.080067f, 6.175392f), new Keyframe(7.090067f, 6.178382f), new Keyframe(7.100068f, 6.181352f), new Keyframe(7.110068f, 6.184301f), new Keyframe(7.120068f, 6.187231f), new Keyframe(7.130068f, 6.190141f), new Keyframe(7.140069f, 6.19303f), new Keyframe(7.150069f, 6.1959f), new Keyframe(7.160069f, 6.19875f), new Keyframe(7.170069f, 6.201581f), new Keyframe(7.180069f, 6.204392f), new Keyframe(7.19007f, 6.207184f), new Keyframe(7.20007f, 6.209956f), new Keyframe(7.21007f, 6.212708f), new Keyframe(7.22007f, 6.215442f), new Keyframe(7.230071f, 6.218156f), new Keyframe(7.240071f, 6.220851f), new Keyframe(7.250071f, 6.223527f), new Keyframe(7.260071f, 6.226184f), new Keyframe(7.270072f, 6.228822f), new Keyframe(7.280072f, 6.231441f), new Keyframe(7.290072f, 6.234042f), new Keyframe(7.300072f, 6.236624f), new Keyframe(7.310072f, 6.239187f), new Keyframe(7.320073f, 6.241732f), new Keyframe(7.330073f, 6.244258f), new Keyframe(7.340073f, 6.246766f), new Keyframe(7.350073f, 6.249256f), new Keyframe(7.360074f, 6.251727f), new Keyframe(7.370074f, 6.25418f), new Keyframe(7.380074f, 6.256615f), new Keyframe(7.390074f, 6.259032f), new Keyframe(7.400074f, 6.261432f), new Keyframe(7.410075f, 6.263813f), new Keyframe(7.420075f, 6.266176f), new Keyframe(7.430075f, 6.268522f), new Keyframe(7.440075f, 6.27085f), new Keyframe(7.450076f, 6.273161f), new Keyframe(7.460076f, 6.275454f), new Keyframe(7.470076f, 6.27773f), new Keyframe(7.480076f, 6.279988f), new Keyframe(7.490077f, 6.282229f), new Keyframe(7.500077f, 6.284453f), new Keyframe(7.510077f, 6.28666f), new Keyframe(7.520077f, 6.28885f), new Keyframe(7.530077f, 6.291023f), new Keyframe(7.540078f, 6.293179f), new Keyframe(7.550078f, 6.295318f), new Keyframe(7.560078f, 6.297441f), new Keyframe(7.570078f, 6.299546f), new Keyframe(7.580079f, 6.301636f), new Keyframe(7.590079f, 6.303708f), new Keyframe(7.600079f, 6.305765f), new Keyframe(7.610079f, 6.307805f), new Keyframe(7.62008f, 6.309828f), new Keyframe(7.63008f, 6.311836f), new Keyframe(7.64008f, 6.313828f), new Keyframe(7.65008f, 6.315803f), new Keyframe(7.66008f, 6.317762f), new Keyframe(7.670081f, 6.319705f), new Keyframe(7.680081f, 6.321633f), new Keyframe(7.690081f, 6.323545f), new Keyframe(7.700081f, 6.325441f), new Keyframe(7.710082f, 6.327322f), new Keyframe(7.720082f, 6.329187f), new Keyframe(7.730082f, 6.331037f), new Keyframe(7.740082f, 6.332871f), new Keyframe(7.750082f, 6.33469f), new Keyframe(7.760083f, 6.336494f), new Keyframe(7.770083f, 6.338283f), new Keyframe(7.780083f, 6.340056f), new Keyframe(7.790083f, 6.341815f), new Keyframe(7.800084f, 6.343558f), new Keyframe(7.810084f, 6.345287f), new Keyframe(7.820084f, 6.347001f), new Keyframe(7.830084f, 6.348701f), new Keyframe(7.840085f, 6.350386f), new Keyframe(7.850085f, 6.352056f), new Keyframe(7.860085f, 6.353711f), new Keyframe(7.870085f, 6.355353f), new Keyframe(7.880085f, 6.35698f), new Keyframe(7.890086f, 6.358593f), new Keyframe(7.900086f, 6.360191f), new Keyframe(7.910086f, 6.361775f), new Keyframe(7.920086f, 6.363346f), new Keyframe(7.930087f, 6.364903f), new Keyframe(7.940087f, 6.366446f), new Keyframe(7.950087f, 6.367975f), new Keyframe(7.960087f, 6.36949f), new Keyframe(7.970088f, 6.370991f), new Keyframe(7.980088f, 6.372479f), new Keyframe(7.990088f, 6.373954f), new Keyframe(8.000088f, 6.375415f), new Keyframe(8.010088f, 6.376863f), new Keyframe(8.020088f, 6.378297f), new Keyframe(8.030088f, 6.379719f), new Keyframe(8.040089f, 6.381127f), new Keyframe(8.050089f, 6.382522f), new Keyframe(8.060089f, 6.383904f), new Keyframe(8.070089f, 6.385274f), new Keyframe(8.08009f, 6.38663f), new Keyframe(8.09009f, 6.387974f), new Keyframe(8.10009f, 6.389305f), new Keyframe(8.11009f, 6.390624f), new Keyframe(8.12009f, 6.39193f), new Keyframe(8.130091f, 6.393224f), new Keyframe(8.140091f, 6.394505f), new Keyframe(8.150091f, 6.395774f), new Keyframe(8.160091f, 6.397031f), new Keyframe(8.170092f, 6.398275f), new Keyframe(8.180092f, 6.399508f), new Keyframe(8.190092f, 6.400728f), new Keyframe(8.200092f, 6.401937f), new Keyframe(8.210093f, 6.403134f), new Keyframe(8.220093f, 6.404319f), new Keyframe(8.230093f, 6.405493f), new Keyframe(8.240093f, 6.406654f), new Keyframe(8.250093f, 6.407805f), new Keyframe(8.260094f, 6.408944f), new Keyframe(8.270094f, 6.410071f), new Keyframe(8.280094f, 6.411187f), new Keyframe(8.290094f, 6.412292f), new Keyframe(8.300095f, 6.413386f), new Keyframe(8.310095f, 6.414469f), new Keyframe(8.320095f, 6.415541f), new Keyframe(8.330095f, 6.416601f), new Keyframe(8.340096f, 6.417651f), new Keyframe(8.350096f, 6.418691f), new Keyframe(8.360096f, 6.419719f), new Keyframe(8.370096f, 6.420737f), new Keyframe(8.380096f, 6.421744f), new Keyframe(8.390097f, 6.422741f), new Keyframe(8.400097f, 6.423728f), new Keyframe(8.410097f, 6.424704f), new Keyframe(8.420097f, 6.42567f), new Keyframe(8.430098f, 6.426625f), new Keyframe(8.440098f, 6.42757f), new Keyframe(8.450098f, 6.428506f), new Keyframe(8.460098f, 6.429432f), new Keyframe(8.470098f, 6.430347f), new Keyframe(8.480099f, 6.431253f), new Keyframe(8.490099f, 6.43215f), new Keyframe(8.500099f, 6.433036f), new Keyframe(8.510099f, 6.433913f), new Keyframe(8.5201f, 6.434781f), new Keyframe(8.5301f, 6.435639f), new Keyframe(8.5401f, 6.436488f), new Keyframe(8.5501f, 6.437327f), new Keyframe(8.560101f, 6.438158f), new Keyframe(8.570101f, 6.438979f), new Keyframe(8.580101f, 6.439791f), new Keyframe(8.590101f, 6.440594f), new Keyframe(8.600101f, 6.441388f), new Keyframe(8.610102f, 6.442174f), new Keyframe(8.620102f, 6.44295f), new Keyframe(8.630102f, 6.443718f), new Keyframe(8.640102f, 6.444478f), new Keyframe(8.650103f, 6.445229f), new Keyframe(8.660103f, 6.445971f), new Keyframe(8.670103f, 6.446705f), new Keyframe(8.680103f, 6.447431f), new Keyframe(8.690104f, 6.448149f), new Keyframe(8.700104f, 6.448858f), new Keyframe(8.710104f, 6.44956f), new Keyframe(8.720104f, 6.450253f), new Keyframe(8.730104f, 6.450939f), new Keyframe(8.740105f, 6.451616f), new Keyframe(8.750105f, 6.452286f), new Keyframe(8.760105f, 6.452949f), new Keyframe(8.770105f, 6.453603f), new Keyframe(8.780106f, 6.45425f), new Keyframe(8.790106f, 6.45489f), new Keyframe(8.800106f, 6.455522f), new Keyframe(8.810106f, 6.456147f), new Keyframe(8.820107f, 6.456765f), new Keyframe(8.830107f, 6.457376f), new Keyframe(8.840107f, 6.457979f), new Keyframe(8.850107f, 6.458575f), new Keyframe(8.860107f, 6.459165f), new Keyframe(8.870108f, 6.459747f), new Keyframe(8.880108f, 6.460323f), new Keyframe(8.890108f, 6.460892f), new Keyframe(8.900108f, 6.461455f), new Keyframe(8.910109f, 6.462011f), new Keyframe(8.920109f, 6.46256f), new Keyframe(8.930109f, 6.463103f), new Keyframe(8.940109f, 6.46364f), new Keyframe(8.950109f, 6.46417f), new Keyframe(8.96011f, 6.464695f), new Keyframe(8.97011f, 6.465213f), new Keyframe(8.98011f, 6.465725f), new Keyframe(8.99011f, 6.466231f), new Keyframe(9.000111f, 6.466732f), new Keyframe(9.010111f, 6.467227f), new Keyframe(9.020111f, 6.467715f), new Keyframe(9.030111f, 6.468198f), new Keyframe(9.040112f, 6.468676f), new Keyframe(9.050112f, 6.469148f), new Keyframe(9.060112f, 6.469615f), new Keyframe(9.070112f, 6.470076f), new Keyframe(9.080112f, 6.470531f), new Keyframe(9.090113f, 6.470983f), new Keyframe(9.100113f, 6.471428f), new Keyframe(9.110113f, 6.471869f), new Keyframe(9.120113f, 6.472304f), new Keyframe(9.130114f, 6.472735f), new Keyframe(9.140114f, 6.473161f), new Keyframe(9.150114f, 6.473582f), new Keyframe(9.160114f, 6.473999f), new Keyframe(9.170115f, 6.474411f), new Keyframe(9.180115f, 6.474818f), new Keyframe(9.190115f, 6.475221f), new Keyframe(9.200115f, 6.47562f), new Keyframe(9.210115f, 6.476014f), new Keyframe(9.220116f, 6.476404f), new Keyframe(9.230116f, 6.476789f), new Keyframe(9.240116f, 6.477171f), new Keyframe(9.250116f, 6.47755f), new Keyframe(9.260117f, 6.477923f), new Keyframe(9.270117f, 6.478293f), new Keyframe(9.280117f, 6.47866f), new Keyframe(9.290117f, 6.479022f), new Keyframe(9.300117f, 6.479381f), new Keyframe(9.310118f, 6.479736f), new Keyframe(9.320118f, 6.480089f), new Keyframe(9.330118f, 6.480437f), new Keyframe(9.340118f, 6.480782f), new Keyframe(9.350119f, 6.481124f), new Keyframe(9.360119f, 6.481463f), new Keyframe(9.370119f, 6.481799f), new Keyframe(9.380119f, 6.482131f), new Keyframe(9.39012f, 6.482461f), new Keyframe(9.40012f, 6.482788f), new Keyframe(9.41012f, 6.483112f), new Keyframe(9.42012f, 6.483434f), new Keyframe(9.43012f, 6.483752f), new Keyframe(9.440121f, 6.484069f), new Keyframe(9.450121f, 6.484382f), new Keyframe(9.460121f, 6.484694f), new Keyframe(9.470121f, 6.485003f), new Keyframe(9.480122f, 6.485309f), new Keyframe(9.490122f, 6.485614f), new Keyframe(9.500122f, 6.485916f), new Keyframe(9.510122f, 6.486217f), new Keyframe(9.520123f, 6.486515f), new Keyframe(9.530123f, 6.486811f), new Keyframe(9.540123f, 6.487106f), new Keyframe(9.550123f, 6.487399f), new Keyframe(9.560123f, 6.48769f), new Keyframe(9.570124f, 6.48798f), new Keyframe(9.580124f, 6.488268f), new Keyframe(9.590124f, 6.488555f), new Keyframe(9.600124f, 6.48884f), new Keyframe(9.610125f, 6.489124f), new Keyframe(9.620125f, 6.489407f), new Keyframe(9.630125f, 6.489689f), new Keyframe(9.640125f, 6.489969f), new Keyframe(9.650126f, 6.490249f), new Keyframe(9.660126f, 6.490528f), new Keyframe(9.670126f, 6.490806f), new Keyframe(9.680126f, 6.491083f), new Keyframe(9.690126f, 6.49136f), new Keyframe(9.700127f, 6.491636f), new Keyframe(9.710127f, 6.491911f), new Keyframe(9.720127f, 6.492186f), new Keyframe(9.730127f, 6.49246f), new Keyframe(9.740128f, 6.492735f), new Keyframe(9.750128f, 6.493009f), new Keyframe(9.760128f, 6.493282f), new Keyframe(9.770128f, 6.493556f), new Keyframe(9.780128f, 6.49383f), new Keyframe(9.790129f, 6.494104f), new Keyframe(9.800129f, 6.494378f), new Keyframe(9.810129f, 6.494652f), new Keyframe(9.820129f, 6.494926f), new Keyframe(9.83013f, 6.495201f), new Keyframe(9.84013f, 6.495477f), new Keyframe(9.85013f, 6.495752f), new Keyframe(9.86013f, 6.496029f), new Keyframe(9.870131f, 6.496306f), new Keyframe(9.880131f, 6.496584f), new Keyframe(9.890131f, 6.496863f), new Keyframe(9.900131f, 6.497142f), new Keyframe(9.910131f, 6.497423f), new Keyframe(9.920132f, 6.497705f), new Keyframe(9.930132f, 6.497987f), new Keyframe(9.940132f, 6.498271f), new Keyframe(9.950132f, 6.498556f), new Keyframe(9.960133f, 6.498843f), new Keyframe(9.970133f, 6.499131f), new Keyframe(9.980133f, 6.49942f), new Keyframe(1f, 6.499711f)
            );

            ThrowAlphaCurve = new AnimationCurve
            (
                new Keyframe(0f, 1f), new Keyframe(0.15f, 0), new Keyframe(0.6f, 0), new Keyframe(0.95f, 1f)
            );

            NewSwayCurve = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, 0.0005299348f), new Keyframe(0.02f, 0.0008844695f), new Keyframe(0.03f, 0.001104074f), new Keyframe(0.04f, 0.001229217f), new Keyframe(0.05f, 0.001300369f), new Keyframe(0.05999999f, 0.001357998f), new Keyframe(0.06999999f, 0.001442575f), new Keyframe(0.07999999f, 0.001594569f), new Keyframe(0.08999999f, 0.001854448f), new Keyframe(0.09999999f, 0.002262684f), new Keyframe(0.11f, 0.002859745f), new Keyframe(0.12f, 0.003686101f), new Keyframe(0.13f, 0.004782219f), new Keyframe(0.14f, 0.006188573f), new Keyframe(0.15f, 0.007945633f), new Keyframe(0.16f, 0.01009386f), new Keyframe(0.17f, 0.01267374f), new Keyframe(0.18f, 0.01572572f), new Keyframe(0.19f, 0.01929029f), new Keyframe(0.2f, 0.02340791f), new Keyframe(0.21f, 0.02811905f), new Keyframe(0.22f, 0.03346418f), new Keyframe(0.23f, 0.03948377f), new Keyframe(0.24f, 0.04621829f), new Keyframe(0.25f, 0.0537082f), new Keyframe(0.26f, 0.06199397f), new Keyframe(0.27f, 0.07111607f), new Keyframe(0.28f, 0.08111499f), new Keyframe(0.29f, 0.09204137f), new Keyframe(0.3f, 0.104522f), new Keyframe(0.31f, 0.1187859f), new Keyframe(0.32f, 0.1346613f), new Keyframe(0.33f, 0.1519762f), new Keyframe(0.3399999f, 0.1705587f), new Keyframe(0.3499999f, 0.1902369f), new Keyframe(0.3599999f, 0.210839f), new Keyframe(0.3699999f, 0.2321929f), new Keyframe(0.3799999f, 0.2541268f), new Keyframe(0.3899999f, 0.2764688f), new Keyframe(0.3999999f, 0.2990471f), new Keyframe(0.4099999f, 0.3216895f), new Keyframe(0.4199999f, 0.3442244f), new Keyframe(0.4299999f, 0.3664797f), new Keyframe(0.4399998f, 0.3882836f), new Keyframe(0.4499998f, 0.4094641f), new Keyframe(0.4599998f, 0.4298493f), new Keyframe(0.4699998f, 0.4492674f), new Keyframe(0.4799998f, 0.4675464f), new Keyframe(0.4899998f, 0.4845145f), new Keyframe(0.4999998f, 0.4999996f), new Keyframe(0.5099998f, 0.5145006f), new Keyframe(0.5199998f, 0.5286377f), new Keyframe(0.5299998f, 0.5424225f), new Keyframe(0.5399998f, 0.555866f), new Keyframe(0.5499998f, 0.5689797f), new Keyframe(0.5599998f, 0.5817747f), new Keyframe(0.5699998f, 0.5942622f), new Keyframe(0.5799997f, 0.6064535f), new Keyframe(0.5899997f, 0.6183599f), new Keyframe(0.5999997f, 0.6299927f), new Keyframe(0.6099997f, 0.641363f), new Keyframe(0.6199997f, 0.6524821f), new Keyframe(0.6299997f, 0.6633613f), new Keyframe(0.6399997f, 0.6740117f), new Keyframe(0.6499997f, 0.6844448f), new Keyframe(0.6599997f, 0.6946716f), new Keyframe(0.6699997f, 0.7047035f), new Keyframe(0.6799996f, 0.7145516f), new Keyframe(0.6899996f, 0.7242273f), new Keyframe(0.6999996f, 0.7337418f), new Keyframe(0.7099996f, 0.7431063f), new Keyframe(0.7199996f, 0.7523321f), new Keyframe(0.7299996f, 0.7614304f), new Keyframe(0.7399996f, 0.7704126f), new Keyframe(0.7499996f, 0.7792897f), new Keyframe(0.7599996f, 0.7880731f), new Keyframe(0.7699996f, 0.796774f), new Keyframe(0.7799996f, 0.8054037f), new Keyframe(0.7899995f, 0.8139734f), new Keyframe(0.7999995f, 0.8224944f), new Keyframe(0.8099995f, 0.8309778f), new Keyframe(0.8199995f, 0.8394351f), new Keyframe(0.8299995f, 0.8478773f), new Keyframe(0.8399995f, 0.8563158f), new Keyframe(0.8499995f, 0.8647617f), new Keyframe(0.8599995f, 0.8732265f), new Keyframe(0.8699995f, 0.8817211f), new Keyframe(0.8799995f, 0.8902571f), new Keyframe(0.8899994f, 0.8988456f), new Keyframe(0.8999994f, 0.9074978f), new Keyframe(0.9099994f, 0.916225f), new Keyframe(0.9199994f, 0.9250383f), new Keyframe(0.9299994f, 0.9339492f), new Keyframe(0.9399994f, 0.9429687f), new Keyframe(0.9499994f, 0.9521084f), new Keyframe(0.9599994f, 0.9613791f), new Keyframe(0.9699994f, 0.9707924f), new Keyframe(0.9799994f, 0.9803593f), new Keyframe(0.9899994f, 0.9900912f), new Keyframe(0.9999993f, 0.9999993f)
            );

            SideToSideCurve = new AnimationCurve
            (
                new Keyframe(0f, 0f), new Keyframe(0.01f, 0.009280327f), new Keyframe(0.02f, 0.03429244f), new Keyframe(0.03f, 0.07303145f), new Keyframe(0.04f, 0.1234925f), new Keyframe(0.05f, 0.1836706f), new Keyframe(0.05999999f, 0.251561f), new Keyframe(0.06999999f, 0.3251587f), new Keyframe(0.07999999f, 0.4024589f), new Keyframe(0.08999999f, 0.4814567f), new Keyframe(0.09999999f, 0.5601471f), new Keyframe(0.11f, 0.6365254f), new Keyframe(0.12f, 0.7085866f), new Keyframe(0.13f, 0.7743258f), new Keyframe(0.14f, 0.8317383f), new Keyframe(0.15f, 0.878819f), new Keyframe(0.16f, 0.9152957f), new Keyframe(0.17f, 0.943934f), new Keyframe(0.18f, 0.9660725f), new Keyframe(0.19f, 0.9830189f), new Keyframe(0.2f, 0.9960806f), new Keyframe(0.21f, 1.006565f), new Keyframe(0.22f, 0.9853745f), new Keyframe(0.23f, 0.9190773f), new Keyframe(0.24f, 0.8488224f), new Keyframe(0.25f, 0.8125733f), new Keyframe(0.26f, 0.804961f), new Keyframe(0.27f, 0.796126f), new Keyframe(0.28f, 0.7565287f), new Keyframe(0.29f, 0.673138f), new Keyframe(0.3f, 0.5757917f), new Keyframe(0.31f, 0.5006f), new Keyframe(0.32f, 0.4532794f), new Keyframe(0.33f, 0.4102767f), new Keyframe(0.3399999f, 0.3713132f), new Keyframe(0.3499999f, 0.3361734f), new Keyframe(0.3599999f, 0.3046415f), new Keyframe(0.3699999f, 0.2765021f), new Keyframe(0.3799999f, 0.2515396f), new Keyframe(0.3899999f, 0.2295384f), new Keyframe(0.3999999f, 0.2102829f), new Keyframe(0.4099999f, 0.1935575f), new Keyframe(0.4199999f, 0.1791467f), new Keyframe(0.4299999f, 0.1668348f), new Keyframe(0.4399998f, 0.1564063f), new Keyframe(0.4499998f, 0.1476457f), new Keyframe(0.4599998f, 0.1403373f), new Keyframe(0.4699998f, 0.1342655f), new Keyframe(0.4799998f, 0.1292147f), new Keyframe(0.4899998f, 0.1249695f), new Keyframe(0.4999998f, 0.1213141f), new Keyframe(0.5099998f, 0.1180331f), new Keyframe(0.5199998f, 0.1149108f), new Keyframe(0.5299998f, 0.1117317f), new Keyframe(0.5399998f, 0.1082802f), new Keyframe(0.5499998f, 0.1043407f), new Keyframe(0.5599998f, 0.09969749f), new Keyframe(0.5699998f, 0.09471519f), new Keyframe(0.5799997f, 0.08987911f), new Keyframe(0.5899997f, 0.08518782f), new Keyframe(0.5999997f, 0.08063993f), new Keyframe(0.6099997f, 0.076234f), new Keyframe(0.6199997f, 0.0719686f), new Keyframe(0.6299997f, 0.06784233f), new Keyframe(0.6399997f, 0.06385377f), new Keyframe(0.6499997f, 0.0600015f), new Keyframe(0.6599997f, 0.05628408f), new Keyframe(0.6699997f, 0.05270011f), new Keyframe(0.6799996f, 0.04924817f), new Keyframe(0.6899996f, 0.04592683f), new Keyframe(0.6999996f, 0.04273468f), new Keyframe(0.7099996f, 0.0396703f), new Keyframe(0.7199996f, 0.03673226f), new Keyframe(0.7299996f, 0.03391915f), new Keyframe(0.7399996f, 0.03122954f), new Keyframe(0.7499996f, 0.02866203f), new Keyframe(0.7599996f, 0.02621518f), new Keyframe(0.7699996f, 0.02388757f), new Keyframe(0.7799996f, 0.02167781f), new Keyframe(0.7899995f, 0.01958445f), new Keyframe(0.7999995f, 0.01760607f), new Keyframe(0.8099995f, 0.01574127f), new Keyframe(0.8199995f, 0.01398861f), new Keyframe(0.8299995f, 0.01234668f), new Keyframe(0.8399995f, 0.01081408f), new Keyframe(0.8499995f, 0.009389348f), new Keyframe(0.8599995f, 0.008071095f), new Keyframe(0.8699995f, 0.006857894f), new Keyframe(0.8799995f, 0.005748332f), new Keyframe(0.8899994f, 0.004740976f), new Keyframe(0.8999994f, 0.003834412f), new Keyframe(0.9099994f, 0.003027216f), new Keyframe(0.9199994f, 0.002317972f), new Keyframe(0.9299994f, 0.001705267f), new Keyframe(0.9399994f, 0.001187667f), new Keyframe(0.9499994f, 0.0007637739f), new Keyframe(0.9599994f, 0.0004321411f), new Keyframe(0.9699994f, 0.0001913905f), new Keyframe(0.9799994f, 4.003942E-05f), new Keyframe(0.9899994f, -2.329051E-05f), new Keyframe(0.9999993f, 0f)
            );


            TryLoadPatch(new Patch_LerpCamera_ForceUpdateSway());
            TryLoadPatch(new Patch_UpdateSwayFactors());
            TryLoadPatch(new Patch_LateUpdate_UpdateWpnStats());
            TryLoadPatch(new Patch_OnShot());
            TryLoadPatch(new Patch_CalculateCameraPosition_HandLayers());
            TryLoadPatch(new Patch_PlayStepSound());
            TryLoadPatch(new Patch_ThrowGrenade());
            TryLoadPatch(new Patch_TranslateCommand());
            TryLoadPatch(new StaminaRegenRatePatch());
            //TryLoadPatch(new Patch_SetAnimatorAndProceduralValues());

            //TryLoadPatch(new Patch_ProcessRotation());
            //TryLoadPatch(new Patch_ProcessUpperbodyRotation());
            //TryLoadPatch(new Patch_ProcessRotation());

            TryLoadPatch(new Patch_SetHeadRotation());
            TryLoadPatch(new Patch_Look());
        }
        void LoadConfigValues()
        {
            // toggles
            IsWeaponDeadzone = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable weapon deadzone", "Aiming deadzone, like in other games but better.");
            IsWeaponSway = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable custom weapon sway", "Completely scratch-built weapon sway. Generally leads aimpoint instead of lagging behind it.");
            IsBreathingEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable breathing effect", "Adds a visual oscillation to your character's weapon, the intensity of which depends on your current stamina.");
            IsPoseEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable stance-dependent weapon position", "When you crouch, your weapon position is pulled in closer to your character.");
            IsPoseChangeEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable stance transition effect", "When you change your crouch position, you see a dip in your sight picture, the speed and intensity of which is driven by how much you change your stance (e.g. incrimental change versus full change.");
            IsArmShakeEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable extra arm stam shake", "Adds additional arm shake as arm stam decreases.");
            IsSmallMovementsEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable small visual effects", "Toggles small details: pulling the weapon in on rotation, lowering with unstocked weapon, new grenade throwing effects, alternative peek head position.");
            IsFootstepEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable footstep effect", "Player's weapon will bounce a bit more when taking steps, effect intesnity depends on several factors.");
            IsParallaxEffect = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable parallax feature", "Extensive feature when causes the weapon to rotate in the player's hand and thus un-align the sights, when the player is rotating. The intensity of the effect depends on your efficiency and the weight and ergo of the weapon.");
            IsDirectionalSway = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable directional sway feature", "Additional layer of weapon sway that is caused by forward-back-left-right movements.");
            IsHeadTiltADS = ConstructBoolConfig(true, BASE_FEATURES_SECTION, "Enable ADS head tilt", "Additional slight head tilt on ADS when using a stocked weapon stock.");

            // general sliders
            WeaponDeadzoneMulti = ConstructFloatConfig(WeaponDeadzoneMultiDefault, GENERAL_SLIDERS, "Deadzone multiplier", "Change overall deadzone effect strength.", 0, 5f);
            WeaponSwayMulti = ConstructFloatConfig(0.3f, GENERAL_SLIDERS, "Sway multiplier", "Change overall sway effect strength.", 0, 2f);
            ParallaxMulti = ConstructFloatConfig(30f, GENERAL_SLIDERS, "Parallax multiplier", "Change overall parallax effect strength.", 1f, 100f);
            DirectionalSwayMulti = ConstructFloatConfig(0.3f, GENERAL_SLIDERS, "Directional Sway Final Modifier", "", 0, 5f);
            EfficiencyInjuryDebuffMulti = ConstructFloatConfig(0.5f, GENERAL_SLIDERS, "Efficiency injury effect multi", "Controls how much injuries affect your Efficiency stat. This is to prevent noodle-arms as soon as you get hit. Bone breaks, bleeds, tremors, pain, and fresh wounds only are affected by this slider - not your overall HP.", 0f, 2f);

            // new sway sliders
            NewSwayPositionMulti = ConstructFloatConfig(0.5f, NEW_SWAY_SLIDERS, "Sway position multiplier", "", 0, 10f);
            NewSwayRotationMulti = ConstructFloatConfig(3f, NEW_SWAY_SLIDERS, "Sway rotation multiplier", "", 0, 10f);
            NewSwayPositionDTMulti = ConstructFloatConfig(20f, NEW_SWAY_SLIDERS, "Sway position change speed", "", 0, 40f);
            NewSwayRotationDTMulti = ConstructFloatConfig(2f, NEW_SWAY_SLIDERS, "Sway rotation change speed", "", 0, 10f);
            NewSwayWpnUnstockedDropValue = ConstructFloatConfig(0.3f, NEW_SWAY_SLIDERS, "Weapon drop for unstocked value", "Unstocked weapons will drop a little when rotating.", 0, 10f);
            NewSwayWpnUnstockedDropSpeed = ConstructFloatConfig(3f, NEW_SWAY_SLIDERS, "Weapon drop for unstocked speed", "", 0, 10f);
            WeaponCantValue = ConstructFloatConfig(0, NEW_SWAY_SLIDERS, "Weapon cant value", "", -1f, 1f);
            LeanExtraVerticalMulti = ConstructFloatConfig(0.01f, NEW_SWAY_SLIDERS, "Lean vertical change value", "When you peek, your weapon points up/down a little to complicate aiming.", 0, 0.05f);
            NewSwayWpnDropFromRotMulti = ConstructFloatConfig(0.15f, NEW_SWAY_SLIDERS, "Weapon aimpoint drop on rotation", "When rotating, your weapon will point down a little consonent with the rotation speed. A slight emulation of manipulating a physical object, stacks with weapon weight etc.", -10f, 10f);
            HyperVerticalClamp = ConstructFloatConfig(0.1f, NEW_SWAY_SLIDERS, "Hyper-vertical effect input clamp", "The 'hyper-vertical' effect emulates gravity, so to speak. Figure it out.", -1f, 1f);
            HyperVerticalMulti = ConstructFloatConfig(2f, NEW_SWAY_SLIDERS, "Hyper-vertical effect value", "The 'hyper-vertical' effect emulates gravity, so to speak. Figure it out.", 0, 30f);
            HyperVerticalDT = ConstructFloatConfig(2.5f, NEW_SWAY_SLIDERS, "Hyper-vertical change speed", "The 'hyper-vertical' effect emulates gravity, so to speak. Figure it out.", -10f, 10f);
            NewSwayADSRotClamp = ConstructFloatConfig(0.9f, NEW_SWAY_SLIDERS, "Clamp sway effect specifically in ADS", "", 0, 1f);
            NewSwayRotDeltaClamp = ConstructFloatConfig(0.01f, NEW_SWAY_SLIDERS, "Sway rotation input clamp", "Touch at your own risk kek", 0, 0.1f);
            NewSwayRotFinalClamp = ConstructFloatConfig(0.06f, NEW_SWAY_SLIDERS, "Sway input smoothing clamp", "Touch at your own risk kek", 0, 0.1f);

            // parallax sliders
            ParallaxSetSizeMulti = ConstructFloatConfig(0.5f, PARALLAX_SLIDERS, "Parallax set size", "Amount of parallax that is possible per player rotation.", 0, 20f);
            ParallaxInAds = ConstructFloatConfig(ParallaxInAdsDefault, PARALLAX_SLIDERS, "Parallax effect in ADS", "The % of parallax effect that you see in ADS (with a stocked weapon)", 0, 1f);
            PistolSpecificParallax = ConstructFloatConfig(3f, PARALLAX_SLIDERS, "Parallax effect value specifically for pistols", "", 0, 10f); 
            ShotParallaxResetTimeMulti = ConstructFloatConfig(ShotParallaxResetTimeMultiDefault, PARALLAX_SLIDERS, "Shot-parallax cooldown multiplier", "Rate of return to reduced parallax in the ADS, after a shot.", 0, 20f);
            AdsParallaxTimeMulti = ConstructFloatConfig(2f, PARALLAX_SLIDERS, "Ads-parallax cooldown multiplier", "", 0, 160f);
            ShotParallaxWeaponWeightMulti = ConstructFloatConfig(ShotParallaxWeaponWeightMultiDefault, PARALLAX_SLIDERS, "Shot parallax weapon weight factor multiplier", "After a shot in ADS, the parallax effect is momentarilly returned to full strength.", 0, 10f);
            ParallaxDTMulti = ConstructFloatConfig(10f, PARALLAX_SLIDERS, "Parallax main lerp multiplier", "How fast the main parallax routine processes (so, responsiveness we can say).", 0.05f, 1000f);
            ParallaxRotationSmoothingMulti = ConstructFloatConfig(10f, PARALLAX_SLIDERS, "ParallaxRotationSmoothingMulti", "The strength with which the parallax effect is bled-off when not rotating.", 0.05f, 1000f);
            ParallaxHardClamp = ConstructFloatConfig(0.2f, PARALLAX_SLIDERS, "Parallax Hard Stop", "Absolute maximum value of the parallax effect -- if you want to prevent noodle-arms, this is a good place to start.", 0, 1f);

            // efficiency sliders
            EfficiencyLerpMulti = ConstructFloatConfig(EfficiencyLerpMultiDefault, EFFICIENCY_SLIDERS, "Efficiency change rate multiplier", "", 0, 10f);

            // misc fine tuning sliders
            LeanCounterRotateMod = ConstructFloatConfig(-8f, MISC_SLIDERS, "Peek counter-rotate value", "Just a small visual effect so that your head does not rotate perfectly with the weapon when peeking; I find this more immersive.", -10f, 10f);
            BreathingEffectMulti = ConstructFloatConfig(BreathingEffectMultiDefault, MISC_SLIDERS, "Breathing effect multiplier", "Adjust the strength of the breathing effect which comes with non-full stamina.", 0, 5f);
            ArmShakeMulti = ConstructFloatConfig(ArmShakeMultiDefault, MISC_SLIDERS, "Arm shake multiplier", "", 0, 5f);
            ArmShakeRateMulti = ConstructFloatConfig(ArmShakeRateMultiDefault, MISC_SLIDERS, "Arm shake effect speed multiplier", "The arm shake effect is an animation, this changes the speed of the animation playback.", 0, 5f);
            FootstepLerpMulti = ConstructFloatConfig(FootstepLerpMultiDefault, MISC_SLIDERS, "Footstep lerp speed multiplier", "The footstep effect is an animation, this changes the speed of the animation playback", 0, 1f);
            FootstepIntesnityMulti = ConstructFloatConfig(6f, MISC_SLIDERS, "Footstep intensity multiplier", "", 0, 10f);
            ThrowStrengthMulti = ConstructFloatConfig(ThrowStrengthMultiDefault, MISC_SLIDERS, "Throw visual effect multi", "Change the strength of the throw animation", 0, 100f);
            ThrowSpeedMulti = ConstructFloatConfig(ThrowSpeedMultiDefault, MISC_SLIDERS, "Throw effect speed", "The throw effect is an animation, this changes the speed of the animation playback.", 0, 10f);
            SideToSideSwayMulti = ConstructFloatConfig(0.01f, MISC_SLIDERS, "Footstep side-to-side value", "", 0, 0.03f);
            SideToSideRotationDTMulti = ConstructFloatConfig(0.9f, MISC_SLIDERS, "Footstep side-to-side rotation speed", "", 0, 10f);
            SideToSidePositionDTMulti = ConstructFloatConfig(0.35f, MISC_SLIDERS, "Footstep side-to-side position speed", "", 0, 10f);
            FootstepCutoffRatio = ConstructFloatConfig(0.65f, MISC_SLIDERS, "Footstep animation cutoff", "Just don't touch this.", 0, 1f);
            MotionTrackingThreshold = ConstructFloatConfig(0.005f, MISC_SLIDERS, "Motion tracking threshold", "Just don't touch this.", 0, 0.01f);
                
            // rotation engine
            RotationAverageDTMulti = ConstructFloatConfig(80f, ROTATION_ENGINE_SLIDERS, "RotationAverageDTMulti", "Just don't touch this.", 0.1f, 100f);
            RotationHistoryClamp = ConstructFloatConfig(0.1f, ROTATION_ENGINE_SLIDERS, "RotationHistoryClamp", "Just don't touch this.", 0f, 1f);

            // directional sway
            DirectionalSwayLateralPosValue = ConstructFloatConfig(-0.02f, DIRECTIONAL_SWAY, "Directional Sway Lateral Pos Value", "", -0.1f, 0.1f);
            DirectionalSwayProjectedPosValue = ConstructFloatConfig(-0.04f, DIRECTIONAL_SWAY, "Directional Sway Projected Pos Value", "", -0.1f, 0.1f);
            DirectionalSwayLateralRotValue = ConstructFloatConfig(0.2f, DIRECTIONAL_SWAY, "Directional Sway Lateral Rot Value", "", -0.2f, 0.2f);
            DirectionalSwayVerticalRotValue = ConstructFloatConfig(0.008f, DIRECTIONAL_SWAY, "Directional Sway Vertical Rot Value", "", -0.1f, 0.1f);
            DirectionalSwayLerpSpeed = ConstructFloatConfig(3f, DIRECTIONAL_SWAY, "Directional Sway Lerp Speed", "", 1f, 20f);
            DirectionalSwayLerpOnAds = ConstructFloatConfig(0.25f, DIRECTIONAL_SWAY, "Directional Sway % During ADS", "", 0, 1f);

            // augmented reloads
            AugmentedReloadSpeed = ConstructFloatConfig(1.3f, AUGMENTED_RELOAD, "Augmented reload speed modifier", "", 1f, 5f);
            AugmentedReloadSprintingDebuff = ConstructFloatConfig(0.7f, AUGMENTED_RELOAD, "Reloading while sprinting debuff %", "", 0.1f, 1f);

            // weapon transitions
            TransitionSpeedPhase1 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "TransitionSpeedPhase1", "", 0.1f, 5f);
            TransitionSpeedPhase2 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "TransitionSpeedPhase2", "", 0.1f, 5f);

            // sling transforms
            SlingPositionStartX = ConstructFloatConfig(0.06f, WEAPON_TRANSITIONS, "SlingPositionStartX", "", -1f, 1f);
            SlingPositionStartY = ConstructFloatConfig(-0.16f, WEAPON_TRANSITIONS, "SlingPositionStartY", "", -1f, 1f);
            SlingPositionStartZ = ConstructFloatConfig(-0.1f, WEAPON_TRANSITIONS, "SlingPositionStartZ", "", -1f, 1f);

            SlingRotationStartX = ConstructFloatConfig(0, WEAPON_TRANSITIONS, "SlingRotationStartX", "", -1f, 1f);
            SlingRotationStartY = ConstructFloatConfig(-0.75f, WEAPON_TRANSITIONS, "SlingRotationStartY", "", -1f, 1f);
            SlingRotationStartZ = ConstructFloatConfig(-0.24f, WEAPON_TRANSITIONS, "SlingRotationStartZ", "", -1f, 1f);

            SlingPositionEndX = ConstructFloatConfig(0.37f, WEAPON_TRANSITIONS, "SlingPositionEndX", "", -1f, 1f);
            SlingPositionEndY = ConstructFloatConfig(-0.16f, WEAPON_TRANSITIONS, "SlingPositionEndY", "", -1f, 1f);
            SlingPositionEndZ = ConstructFloatConfig(-0.46f, WEAPON_TRANSITIONS, "SlingPositionEndZ", "", -1f, 1f);

            SlingRotationEndX = ConstructFloatConfig(0, WEAPON_TRANSITIONS, "SlingRotationEndX", "", -1f, 1f);
            SlingRotationEndY = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "SlingRotationEndY", "", -1f, 1f);
            SlingRotationEndZ = ConstructFloatConfig(-0.24f, WEAPON_TRANSITIONS, "SlingRotationEndZ", "", -1f, 1f);

            // shoulder transforms
            ShoulderPositionStartX = ConstructFloatConfig(0.27f, WEAPON_TRANSITIONS, "ShoulderPositionStartX", "", -1f, 1f);
            ShoulderPositionStartY = ConstructFloatConfig(-0.5f, WEAPON_TRANSITIONS, "ShoulderPositionStartY", "", -1f, 1f);
            ShoulderPositionStartZ = ConstructFloatConfig(-0.12f, WEAPON_TRANSITIONS, "ShoulderPositionStartZ", "", -1f, 1f);

            ShoulderRotationStartX = ConstructFloatConfig(-0.4f, WEAPON_TRANSITIONS, "ShoulderRotationStartX", "", -1f, 1f);
            ShoulderRotationStartY = ConstructFloatConfig(-0.56f, WEAPON_TRANSITIONS, "ShoulderRotationStartY", "", -1f, 1f);
            ShoulderRotationStartZ = ConstructFloatConfig(0, WEAPON_TRANSITIONS, "ShoulderRotationStartZ", "", -1f, 1f);

            ShoulderPositionEndX = ConstructFloatConfig(0.25f, WEAPON_TRANSITIONS, "ShoulderPositionEndX", "", -1f, 1f);
            ShoulderPositionEndY = ConstructFloatConfig(-0.50f, WEAPON_TRANSITIONS, "ShoulderPositionEndY", "", -1f, 1f);
            ShoulderPositionEndZ = ConstructFloatConfig(-0.19f, WEAPON_TRANSITIONS, "ShoulderPositionEndZ", "", -1f, 1f);

            ShoulderRotationEndX = ConstructFloatConfig(-0.4f, WEAPON_TRANSITIONS, "ShoulderRotationEndX", "", -1f, 1f);
            ShoulderRotationEndY = ConstructFloatConfig(-0.57f, WEAPON_TRANSITIONS, "ShoulderRotationEndY", "", -1f, 1f);
            ShoulderRotationEndZ = ConstructFloatConfig(0f, WEAPON_TRANSITIONS, "ShoulderRotationEndZ", "", -1f, 1f);

            // holster transforms
            HolsterPositionStartX = ConstructFloatConfig(0, WEAPON_TRANSITIONS, "HolsterPositionStartX", "", -1f, 1f);
            HolsterPositionStartY = ConstructFloatConfig(-0.05f, WEAPON_TRANSITIONS, "HolsterPositionStartY", "", -1f, 1f);
            HolsterPositionStartZ = ConstructFloatConfig(-0.2f, WEAPON_TRANSITIONS, "HolsterPositionStartZ", "", -1f, 1f);

            HolsterRotationStartX = ConstructFloatConfig(-0.1f, WEAPON_TRANSITIONS, "HolsterRotationStartX", "", -1f, 1f);
            HolsterRotationStartY = ConstructFloatConfig(-0.2f, WEAPON_TRANSITIONS, "HolsterRotationStartY", "", -1f, 1f);
            HolsterRotationStartZ = ConstructFloatConfig(0, WEAPON_TRANSITIONS, "HolsterRotationStartZ", "", -1f, 1f);

            HolsterPositionEndX = ConstructFloatConfig(0, WEAPON_TRANSITIONS, "HolsterPositionEndX", "", -1f, 1f);
            HolsterPositionEndY = ConstructFloatConfig(0, WEAPON_TRANSITIONS, "HolsterPositionEndY", "", -1f, 1f);
            HolsterPositionEndZ = ConstructFloatConfig(0, WEAPON_TRANSITIONS, "HolsterPositionEndZ", "", -1f, 1f);

            HolsterRotationEndX = ConstructFloatConfig(0, WEAPON_TRANSITIONS, "HolsterRotationEndX", "", -1f, 1f);
            HolsterRotationEndY = ConstructFloatConfig(0, WEAPON_TRANSITIONS, "HolsterRotationEndY", "", -1f, 1f);
            HolsterRotationEndZ = ConstructFloatConfig(0, WEAPON_TRANSITIONS, "HolsterRotationEndZ", "", -1f, 1f);

            // sling-to-shoulder curve
            Sling2ShoulderSpeed1 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "Sling2ShoulderSpeed1", "", -10f, 10f);
            Sling2ShoulderSpeed2 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "Sling2ShoulderSpeed2", "", -10f, 10f);

            Sling2ShoulderCurve1_1 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "1 - Sling2ShoulderCurve1_1", "", 0.1f, 3f);
            Sling2ShoulderCurve2_1 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "2 - Sling2ShoulderCurve2_1", "", 0.1f, 3f);
            Sling2ShoulderCurve3_1 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "3 - Sling2ShoulderCurve3_1", "", 0.1f, 3f);
            Sling2ShoulderCurve4_1 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "4 - Sling2ShoulderCurve4_1", "", 0.1f, 3f);
            Sling2ShoulderCurve5_1 = ConstructFloatConfig(0.75f, WEAPON_TRANSITIONS, "5 - Sling2ShoulderCurve5_1", "", 0.1f, 3f);
            Sling2ShoulderCurve6_1 = ConstructFloatConfig(0.2f, WEAPON_TRANSITIONS, "6 - Sling2ShoulderCurve6_1", "", 0.1f, 3f);

            Sling2ShoulderCurve1_2 = ConstructFloatConfig(0.3f, WEAPON_TRANSITIONS, "7 - Sling2ShoulderCurve1_2", "", 0.1f, 3f);
            Sling2ShoulderCurve2_2 = ConstructFloatConfig(1.5f, WEAPON_TRANSITIONS, "8 - Sling2ShoulderCurve2_2", "", 0.1f, 3f);
            Sling2ShoulderCurve3_2 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "9 - Sling2ShoulderCurve3_2", "", 0.1f, 3f);
            Sling2ShoulderCurve4_2 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "99 - Sling2ShoulderCurve4_2", "", 0.1f, 3f);
            Sling2ShoulderCurve5_2 = ConstructFloatConfig(2f, WEAPON_TRANSITIONS, "999 - Sling2ShoulderCurve5_2", "", 0.1f, 3f);
            Sling2ShoulderCurve6_2 = ConstructFloatConfig(3f, WEAPON_TRANSITIONS, "9999 - Sling2ShoulderCurve6_2", "", 0.1f, 3f);

            // shoulder-to-sling curve
            Shoulder2SlingSpeed1 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "Shoulder2SlingSpeed1", "", -10f, 10f);
            Shoulder2SlingSpeed2 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "Shoulder2SlingSpeed2", "", -10f, 10f);

            Shoulder2SlingCurve1_1 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "1 - Shoulder2SlingCurve1_1", "", 0.1f, 3f);
            Shoulder2SlingCurve2_1 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "2 - Shoulder2SlingCurve2_1", "", 0.1f, 3f);
            Shoulder2SlingCurve3_1 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "3 - Shoulder2SlingCurve3_1", "", 0.1f, 3f);
            Shoulder2SlingCurve4_1 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "4 - Shoulder2SlingCurve4_1", "", 0.1f, 3f);
            Shoulder2SlingCurve5_1 = ConstructFloatConfig(0.75f, WEAPON_TRANSITIONS, "5 - Shoulder2SlingCurve5_1", "", 0.1f, 3f);
            Shoulder2SlingCurve6_1 = ConstructFloatConfig(0.2f, WEAPON_TRANSITIONS, "6 - Shoulder2SlingCurve6_1", "", 0.1f, 3f);

            Shoulder2SlingCurve1_2 = ConstructFloatConfig(0.3f, WEAPON_TRANSITIONS, "7 - Shoulder2SlingCurve1_2", "", 0.1f, 3f);
            Shoulder2SlingCurve2_2 = ConstructFloatConfig(1.5f, WEAPON_TRANSITIONS, "8 - Shoulder2SlingCurve2_2", "", 0.1f, 3f);
            Shoulder2SlingCurve3_2 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "9 - Shoulder2SlingCurve3_2", "", 0.1f, 3f);
            Shoulder2SlingCurve4_2 = ConstructFloatConfig(1f, WEAPON_TRANSITIONS, "99 - Shoulder2SlingCurve4_2", "", 0.1f, 3f);
            Shoulder2SlingCurve5_2 = ConstructFloatConfig(2f, WEAPON_TRANSITIONS, "999 - Shoulder2SlingCurve5_2", "", 0.1f, 3f);
            Shoulder2SlingCurve6_2 = ConstructFloatConfig(3f, WEAPON_TRANSITIONS, "9999 - Shoulder2SlingCurve6_2", "", 0.1f, 3f);

            TransformSmoothingDTMulti = ConstructFloatConfig(12f, WEAPON_TRANSITIONS, "99999 - TransformSmoothingDTMulti", "", 0.1f, 100f);


            // dev
            DebugHeadRotX = ConstructFloatConfig(0, DEV_SECTION, "DebugHeadRotX", "", -20f, 20f);
            DebugHeadRotY = ConstructFloatConfig(0, DEV_SECTION, "DebugHeadRotY", "", -20f, 20f);
            DebugHeadRotZ = ConstructFloatConfig(0, DEV_SECTION, "DebugHeadRotZ", "", -20f, 20f);

            DebugHandsRotX = ConstructFloatConfig(0, DEV_SECTION, "DebugHandsRotX", "", -1.5f, 1.5f);
            DebugHandsRotY = ConstructFloatConfig(0, DEV_SECTION, "DebugHandsRotY", "", -1.5f, 1.5f);
            DebugHandsRotZ = ConstructFloatConfig(0, DEV_SECTION, "DebugHandsRotZ", "", -1.5f, 1.5f);

            DebugHandsPosX = ConstructFloatConfig(0, DEV_SECTION, "DebugHandsPosX", "", -0.5f, 0.5f);
            DebugHandsPosY = ConstructFloatConfig(0, DEV_SECTION, "DebugHandsPosY", "", -0.5f, 0.5f);
            DebugHandsPosZ = ConstructFloatConfig(0, DEV_SECTION, "DebugHandsPosZ", "", -0.5f, 0.5f);

            IsLogging = ConstructBoolConfig(false, DEV_SECTION, "Enable debug logging", "");
            DebugSpam = ConstructBoolConfig(false, DEV_SECTION, "Enable debug spam", "");


            // anim curve

            DebugFloat1 = ConstructFloatConfig(0, ANIM_CURVE, "DebugFloat1", "", -10f, 10f);
            DebugFloat2 = ConstructFloatConfig(0, ANIM_CURVE, "DebugFloat2", "", -10f, 10f);

            AnimCurveTime0 = ConstructFloatConfig(1f, ANIM_CURVE, "1 - AnimCurveTime0", "", 0.1f, 3f);
            AnimCurveTime0_2 = ConstructFloatConfig(1f, ANIM_CURVE, "2 - AnimCurveTime0_2", "", 0.1f, 3f);
            AnimCurveTime0_4 = ConstructFloatConfig(1f, ANIM_CURVE, "3 - AnimCurveTime0_4", "", 0.1f, 3f);
            AnimCurveTime0_6 = ConstructFloatConfig(1f, ANIM_CURVE, "4 - AnimCurveTime0_6", "", 0.1f, 3f);
            AnimCurveTime0_8 = ConstructFloatConfig(1f, ANIM_CURVE, "5 - AnimCurveTime0_8", "", 0.1f, 3f);
            AnimCurveTime1 = ConstructFloatConfig(1f, ANIM_CURVE, "6 - AnimCurveTime1", "", 0.1f, 3f);

            AnimCurveTime0p2 = ConstructFloatConfig(1f, ANIM_CURVE, "7 - AnimCurveTime0p2", "", 0.1f, 10f);
            AnimCurveTime0_2p2 = ConstructFloatConfig(1f, ANIM_CURVE, "8 - AnimCurveTime0_2p2", "", 0.1f, 10f);
            AnimCurveTime0_4p2 = ConstructFloatConfig(1f, ANIM_CURVE, "9 - AnimCurveTime0_4p2", "", 0.1f, 10f);
            AnimCurveTime0_6p2 = ConstructFloatConfig(1f, ANIM_CURVE, "10 - AnimCurveTime0_6p2", "", 0.1f, 10f);
            AnimCurveTime0_8p2 = ConstructFloatConfig(1f, ANIM_CURVE, "11 - AnimCurveTime0_8p2", "", 0.1f, 10f);
            AnimCurveTime1p2 = ConstructFloatConfig(1f, ANIM_CURVE, "12 - AnimCurveTime1p2", "", 0.1f, 10f);
        }

        static readonly string ANIM_CURVE = "999 - Anim curve builder";

        void Update()
        {
            DeltaTime += (UnityEngine.Time.unscaledDeltaTime - DeltaTime);
            Time += UnityEngine.Time.unscaledDeltaTime;

            DriveLerps();
        }

        //
        // drive lerps
        //
        void DriveLerps()
        {
            UtilsTIRL.Update(DeltaTime);
            EfficiencyController.UpdateEfficiencyLerp(DeltaTime);
            FootstepController.UpdateStep(DeltaTime);
            SwayController.UpdateLerp(DeltaTime);
            ThrowController.UpdateLerp(DeltaTime);
            HeadRotController.UpdateLerp(DeltaTime);
            DirectionalSwayController.UpdateDirectionalSwayLerp(DeltaTime);
        }

        void FixedUpdate()
        {
            FixedDeltaTime += (UnityEngine.Time.unscaledDeltaTime - FixedDeltaTime);

            PlayerMotionController.UpdateMovementMeasurementsInFDT(FixedDeltaTime);
            WeaponSelectionController.UpdateAnimationPump(FixedDeltaTime);
            NewDeadzoneController.Update(FixedDeltaTime);
            NewSwayController.UpdateLerp(FixedDeltaTime);
        }

        void LateUpdate()
        {
            SwayController.IsSwayUpdatedThisFrame = false;
            DeadzoneController.DeadzoneUpdatedThisFrame = false;
        }

        void TryLoadPatch(ModulePatch patch)
        {
            try
            {
                ((ModulePatch)patch).Enable();
            }
            catch (Exception e)
            {
                string patchName = patch.ToString();
                if (UtilsTIRL.IsPriority(1)) UtilsTIRL.Log("could not load " + patchName + " -- " + e);
                throw;
            }
        }

        ConfigEntry<float> ConstructFloatConfig(float defaultValue, string category, string descriptionShort, string descriptionFull, float min, float max)
        {
            ConfigEntry<float> result = ((BaseUnityPlugin)this).Config.Bind<float>(category, descriptionShort, defaultValue, new ConfigDescription(descriptionFull, (AcceptableValueBase)(object)new AcceptableValueRange<float>(min, max), Array.Empty<object>()));
            return result;
        }

        ConfigEntry<bool> ConstructBoolConfig(bool defaultValue, string category, string descriptionShort, string descriptionFull)
        {
            ConfigEntry<bool> result = ((BaseUnityPlugin)this).Config.Bind<bool>(category, descriptionShort, defaultValue, new ConfigDescription(descriptionFull, (AcceptableValueBase)null, Array.Empty<object>()));
            return result;
        }
    }
}
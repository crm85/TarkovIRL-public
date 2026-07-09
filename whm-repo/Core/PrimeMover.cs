using BepInEx;
using BepInEx.Configuration;
using RealismMod;
using System;
using UnityEngine;

namespace TarkovIRL
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class PrimeMover : BaseUnityPlugin
    {
        const string modGUID = "TarkovIRL";
        const string modName = "TarkovIRL - WHM";
        const string modVersion = "1.1";

        public static PrimeMover Instance;

        public readonly float FTDThresh = 1f / 30f;

        GUIStyle efficiencyIndicatorStyle = new GUIStyle();
        bool _wasInRaid = false;

        //
        // Curves
        //
        public AnimationCurve BreathCurve;
        public AnimationCurve PoseChangeCurve;
        public AnimationCurve PoseChangeDownRadialCurve;
        public AnimationCurve PoseChangeCurveUp;
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
        public AnimationCurve SlowReloadCurve;
        public AnimationCurve PresentShoulderCurve;
        public AnimationCurve OrderShoulderCurve;

        public enum ERunPoseDebugOverride
        {
            Off,
            NormalSprintTransition,
            HighReadyTransition
        }

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


        const string BASE_FEATURES_SECTION = "a - Toggle base features";
        const string GENERAL_SLIDERS = "b - Adjust main feature values";
        const string NEW_SWAY_SLIDERS = "c - Sway multipliers";
        const string PARALLAX_SLIDERS = "d - Parallax multipliers";
        const string EFFICIENCY_SLIDERS = "e - Efficiency multipliers";
        const string ROTATION_ENGINE_SLIDERS = "f - Rotation engine multipliers";
        const string MISC_SLIDERS = "g - Misc multipliers";
        const string DIRECTIONAL_SWAY = "h - Directional Sway Values";
        const string AUGMENTED_RELOAD = "i - Augmented Reload Values";
        const string DEADZONE = "j - Deadzone Values";
        const string DEBUG_OPTIONS = "k - Debug logs";
        const string RUN_POSE = "l - Run Pose Values";

        const string DEV_SECTION = "0 - Only for dev/testing";


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
        public static ConfigEntry<bool> IsAugmentedReloadDefault;
        public static ConfigEntry<bool> IsAugmentedReload;
        public static ConfigEntry<bool> IsWeaponTrans;
        public static ConfigEntry<bool> IsRunPoseCorrection;
        public static ConfigEntry<bool> IsEfficiencyIndicator;

        public static ConfigEntry<bool> IsLogging;
        public static ConfigEntry<bool> DebugSpam;
        public static ConfigEntry<ERunPoseDebugOverride> DebugRunPoseOverride;

        public static ConfigEntry<float> NewSwayFinalLerpSpeed;
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
        public static ConfigEntry<float> PistolShotParallaxWeightClamp;
        public static ConfigEntry<float> ThrowStrengthMulti;
        public static ConfigEntry<float> ThrowSpeedMulti;
        public static ConfigEntry<float> EfficiencyInjuryImpact;
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
        public static ConfigEntry<float> ParallaxHardClampPistols;
        public static ConfigEntry<float> DeadzoneInShortStock;
        public static ConfigEntry<float> DeadzoneInVanilla;
        public static ConfigEntry<float> DeadzoneInActiveAim;
        public static ConfigEntry<float> DeadzoneInADS;
        public static ConfigEntry<float> DeadzoneInHighReady;
        public static ConfigEntry<float> DeadzoneInLowReady;
        public static ConfigEntry<float> DeadzoneHeadFollowSpeedMulti;
        public static ConfigEntry<float> TransitionSpeedMulti;
        public static ConfigEntry<float> RunFadeDTMulti;
        public static ConfigEntry<float> RunPoseEnterSpeed;
        public static ConfigEntry<float> RunPoseExitSpeed;
        public static ConfigEntry<Vector3> RunPoseSprintTransitionPosition;
        public static ConfigEntry<Vector3> RunPoseSprintTransitionRotation;
        public static ConfigEntry<Vector3> RunPoseSprintPosition;
        public static ConfigEntry<Vector3> RunPoseSprintRotation;
        public static ConfigEntry<Vector3> RunPoseHighReadyTransitionPosition;
        public static ConfigEntry<Vector3> RunPoseHighReadyTransitionRotation;
        public static ConfigEntry<Vector3> RunPoseHighReadyPosition;
        public static ConfigEntry<Vector3> RunPoseHighReadyRotation;
        public static ConfigEntry<float> LaggingSwayNorm;
        public static ConfigEntry<float> LaggingSwayMulti;
        public static ConfigEntry<float> LaggingSwayClamp;
        public static ConfigEntry<float> HandFinalSmooth;

        public static ConfigEntry<bool> DeadzoneWeightForEfficiency;
        public static ConfigEntry<bool> DebugEfficiency;

        public static ConfigEntry<float> AnimatorLayerInt;

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

        // debug
        public static ConfigEntry<float> EfficiencyOverweightIpmact;
        public static ConfigEntry<float> DebugFloat1;
        public static ConfigEntry<float> LoggingUpdateFrequency;

        // config defaults
        const float ArmShakeRateMultiDefault = 1f;
        const float BreathingEffectMultiDefault = 1f;
        const float FootstepLerpMultiDefault = 0.1f;
        const float ParallaxInAdsDefault = 0.3f;
        const float ShotParallaxWeaponWeightMultiDefault = 6f;
        const float ShotParallaxResetTimeMultiDefault = 5f;
        const float ThrowStrengthMultiDefault = 14f;
        const float ThrowSpeedMultiDefault = 2.25f;

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
            TIRLUtils.Logger = this.Logger;

            ModAnimationCurves.AssignTo(this);
            PatchRegistry.EnableCorePatches();
        }
        private void LoadConfigValues()
        {
            IsWeaponDeadzone = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable weapon deadzone", "Aiming deadzone, like in other games but better.");
            IsEfficiencyIndicator = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable efficiency indicator", "When this toggle is enabled, you see two dots at the bottom of your screen whose distance apart indicates how efficient your character currently is (or isn't). Closer is better.");
            IsWeaponSway = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable custom weapon sway", "Completely scratch-built weapon sway. Generally leads aimpoint instead of lagging behind it.");
            IsBreathingEffect = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable breathing effect", "Adds a visual oscillation to your character's weapon, the intensity of which depends on your current stamina.");
            IsPoseEffect = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable stance-dependent weapon position", "When you crouch, your weapon position is pulled in closer to your character.");
            IsPoseChangeEffect = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable stance transition effect", "When you change your crouch position, you see a dip in your sight picture, the speed and intensity of which is driven by how much you change your stance (e.g. incrimental change versus full change.");
            IsArmShakeEffect = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable extra arm stam shake", "Adds additional arm shake as arm stam decreases.");
            IsSmallMovementsEffect = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable small visual effects", "Toggles small details: pulling the weapon in on rotation, lowering with unstocked weapon, new grenade throwing effects, alternative peek head position.");
            IsFootstepEffect = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable footstep effect", "Player's weapon will bounce a bit more when taking steps, effect intesnity depends on several factors.");
            IsParallaxEffect = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable aiming misalignment feature", "Extensive feature when causes the weapon to rotate in the player's hand and thus un-align the sights, when the player is rotating. The intensity of the effect depends on your efficiency and the weight and ergo of the weapon.");
            IsDirectionalSway = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable directional sway feature", "Additional layer of weapon sway that is caused by forward-back-left-right movements.");
            IsHeadTiltADS = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable ADS head tilt", "Additional slight head tilt on ADS when using a stocked weapon stock.");
            IsAugmentedReload = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable Augmented Reload", "This feature slows standard reloads, and also allows the player to increase reload speed by looking at the reload operation.");
            IsAugmentedReloadDefault = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Reloads use Augmented Reload by default", "Whenever reloading, your character will automatically enter augmented mode - can be cancelled during reload");
            IsWeaponTrans = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable Enhanced Weapon Transitions", "Custom implementation of weapon transitions (sling-shoulder-holster)");
            IsRunPoseCorrection = ConstructBoolConfig(defaultValue: true, BASE_FEATURES_SECTION, "Enable run pose correction", "Smooths first-person weapon position when entering and exiting the body sprint animation state.");
            IsLogging = ConstructBoolConfig(defaultValue: false, DEBUG_OPTIONS, "Enable debug logging", "");
            DebugSpam = ConstructBoolConfig(defaultValue: false, DEBUG_OPTIONS, "Enable debug spam", "");
            DebugEfficiency = ConstructBoolConfig(defaultValue: false, DEBUG_OPTIONS, "Show efficiency value (inverse version)", "");
            DebugRunPoseOverride = ConstructEnumConfig(ERunPoseDebugOverride.Off, DEV_SECTION, "Run pose debug override", "Forces one run-pose transition offset while a firearm is equipped so the position and rotation values can be tuned without repeatedly sprinting.");
            WeaponDeadzoneMulti = ConstructFloatConfig(0.3f, GENERAL_SLIDERS, "Deadzone multiplier", "Change overall deadzone effect strength.", 0f, 5f);
            WeaponSwayMulti = ConstructFloatConfig(0.5f, GENERAL_SLIDERS, "Sway multiplier", "Change overall sway effect strength.", 0f, 2f);
            ParallaxMulti = ConstructFloatConfig(16f, GENERAL_SLIDERS, "Aiming misalignment multiplier", "Change overall parallax effect strength.", 1f, 100f);
            DirectionalSwayMulti = ConstructFloatConfig(0.12f, GENERAL_SLIDERS, "Directional Sway Final Modifier", "", 0f, 5f);
            TransitionSpeedMulti = ConstructFloatConfig(1.3f, GENERAL_SLIDERS, "Weapon transition speed multiplier", "Global multiplier for how fast all weapon transitions occur", 0.1f, 5f);
            HandFinalSmooth = ConstructFloatConfig(1f, GENERAL_SLIDERS, "Main hand smoothing layer", "Value to control how much all of the hand movements are smoothed before being pushed to visual layer", 1f, 20f);

            NewSwayFinalLerpSpeed = ConstructFloatConfig(15f, NEW_SWAY_SLIDERS, "Sway return to centre speed", "The speed at which the sway functions corrects back to zero", -5f, 50f);
            NewSwayPositionMulti = ConstructFloatConfig(0.75f, NEW_SWAY_SLIDERS, "Sway position multiplier", "", 0f, 10f);
            NewSwayRotationMulti = ConstructFloatConfig(2f, NEW_SWAY_SLIDERS, "Sway rotation multiplier", "", 0f, 10f);
            NewSwayPositionDTMulti = ConstructFloatConfig(55f, NEW_SWAY_SLIDERS, "Sway position change speed", "", 0f, 150f);
            NewSwayRotationDTMulti = ConstructFloatConfig(13f, NEW_SWAY_SLIDERS, "Sway rotation change speed", "", 0f, 15f);
            NewSwayWpnUnstockedDropValue = ConstructFloatConfig(0.3f, NEW_SWAY_SLIDERS, "Weapon drop for unstocked value", "Unstocked weapons will drop a little when rotating.", 0f, 10f);
            NewSwayWpnUnstockedDropSpeed = ConstructFloatConfig(3f, NEW_SWAY_SLIDERS, "Weapon drop for unstocked speed", "", 0f, 10f);
            WeaponCantValue = ConstructFloatConfig(0f, NEW_SWAY_SLIDERS, "Weapon cant value", "", -1f, 1f);
            LeanExtraVerticalMulti = ConstructFloatConfig(0.01f, NEW_SWAY_SLIDERS, "Lean vertical change value", "When you peek, your weapon points up/down a little to complicate aiming.", 0f, 0.05f);
            NewSwayWpnDropFromRotMulti = ConstructFloatConfig(0.3f, NEW_SWAY_SLIDERS, "Weapon aimpoint drop on rotation", "When rotating, your weapon will point down a little consonent with the rotation speed. A slight emulation of manipulating a physical object, stacks with weapon weight etc.", -10f, 10f);
            HyperVerticalClamp = ConstructFloatConfig(0.1f, NEW_SWAY_SLIDERS, "Hyper-vertical effect input clamp", "The 'hyper-vertical' effect emulates gravity, so to speak. Figure it out.", -1f, 1f);
            HyperVerticalMulti = ConstructFloatConfig(2f, NEW_SWAY_SLIDERS, "Hyper-vertical effect value", "The 'hyper-vertical' effect emulates gravity, so to speak. Figure it out.", 0f, 30f);
            HyperVerticalDT = ConstructFloatConfig(2.5f, NEW_SWAY_SLIDERS, "Hyper-vertical change speed", "The 'hyper-vertical' effect emulates gravity, so to speak. Figure it out.", -10f, 10f);
            NewSwayADSRotClamp = ConstructFloatConfig(0.9f, NEW_SWAY_SLIDERS, "Clamp sway effect specifically in ADS", "", 0f, 1f);
            NewSwayRotDeltaClamp = ConstructFloatConfig(0.01f, NEW_SWAY_SLIDERS, "Sway rotation input clamp", "Touch at your own risk kek", 0f, 0.1f);
            NewSwayRotFinalClamp = ConstructFloatConfig(0.06f, NEW_SWAY_SLIDERS, "Sway input smoothing clamp", "Touch at your own risk kek", 0f, 0.1f);
            LaggingSwayNorm = ConstructFloatConfig(0.5f, NEW_SWAY_SLIDERS, "Lagging sway norm", "", 0f, 5f);
            LaggingSwayMulti = ConstructFloatConfig(2f, NEW_SWAY_SLIDERS, "Lagging sway multiplier", "Defines strength of laggin sway effect, a component of the overall sway effect package.", 0f, 5f);
            LaggingSwayClamp = ConstructFloatConfig(12f, NEW_SWAY_SLIDERS, "Lagging sway clamp", "Defines max strength of laggin sway effect, a component of the overall sway effect package.", 1f, 30f);

            ParallaxSetSizeMulti = ConstructFloatConfig(0.5f, PARALLAX_SLIDERS, "Parallax set size", "Amount of parallax that is possible per player rotation.", 0f, 20f);
            ParallaxInAds = ConstructFloatConfig(ParallaxInAdsDefault, PARALLAX_SLIDERS, "Parallax effect in ADS", "The % of parallax effect that you see in ADS (with a stocked weapon)", 0f, 1f);
            PistolSpecificParallax = ConstructFloatConfig(4f, PARALLAX_SLIDERS, "Parallax effect value specifically for pistols", "", 0f, 10f);
            ShotParallaxResetTimeMulti = ConstructFloatConfig(ShotParallaxResetTimeMultiDefault, PARALLAX_SLIDERS, "Shot-parallax cooldown multiplier", "Rate of return to reduced parallax in the ADS, after a shot.", 0f, 20f);
            AdsParallaxTimeMulti = ConstructFloatConfig(2f, PARALLAX_SLIDERS, "Ads-parallax cooldown multiplier", "", 0f, 160f);
            ShotParallaxWeaponWeightMulti = ConstructFloatConfig(ShotParallaxWeaponWeightMultiDefault, PARALLAX_SLIDERS, "Shot parallax weapon weight factor multiplier", "After a shot in ADS, the parallax effect is momentarilly returned to full strength.", 0f, 10f);
            PistolShotParallaxWeightClamp = ConstructFloatConfig(1.2f, PARALLAX_SLIDERS, "Pistol shot parallax weight clamp", "Maximum shot-parallax weight applied to pistols after firing.", 0f, 20f);
            ParallaxDTMulti = ConstructFloatConfig(10f, PARALLAX_SLIDERS, "Parallax main lerp multiplier", "How fast the main parallax routine processes (so, responsiveness we can say).", 0.05f, 1000f);
            ParallaxRotationSmoothingMulti = ConstructFloatConfig(10f, PARALLAX_SLIDERS, "ParallaxRotationSmoothingMulti", "The strength with which the parallax effect is bled-off when not rotating.", 0.05f, 1000f);
            ParallaxHardClamp = ConstructFloatConfig(0.08f, PARALLAX_SLIDERS, "Parallax Hard Stop", "Absolute maximum value of the parallax effect -- if you want to prevent noodle-arms, this is a good place to start.", 0f, 1f);
            ParallaxHardClampPistols = ConstructFloatConfig(0.03f, PARALLAX_SLIDERS, "Parallax Hard Stop Pistols", "Absolute maximum value of the parallax effect -- if you want to prevent noodle-arms, this is a good place to start.", 0f, 1f);

            EfficiencyLerpMulti = ConstructFloatConfig(8f, EFFICIENCY_SLIDERS, "Efficiency change rate multiplier", "", 0f, 10f);
            EfficiencyOverweightIpmact = ConstructFloatConfig(250f, EFFICIENCY_SLIDERS, "Efficiency overweight coef", "Controls how much being overweight affects your Efficiency.", 0, 200f);
            EfficiencyInjuryImpact = ConstructFloatConfig(0.5f, EFFICIENCY_SLIDERS, "Efficiency injury effect coef", "Controls how much injuries affect your Efficiency stat (before you heal them).", 0f, 2f);

            LeanCounterRotateMod = ConstructFloatConfig(-8f, MISC_SLIDERS, "Peek counter-rotate value", "Just a small visual effect so that your head does not rotate perfectly with the weapon when peeking; I find this more immersive.", -10f, 10f);
            BreathingEffectMulti = ConstructFloatConfig(BreathingEffectMultiDefault, MISC_SLIDERS, "Breathing effect multiplier", "Adjust the strength of the breathing effect which comes with non-full stamina.", 0f, 5f);
            ArmShakeMulti = ConstructFloatConfig(2f, MISC_SLIDERS, "Arm shake multiplier", "", 0f, 5f);
            ArmShakeRateMulti = ConstructFloatConfig(ArmShakeRateMultiDefault, MISC_SLIDERS, "Arm shake effect speed multiplier", "The arm shake effect is an animation, this changes the speed of the animation playback.", 0f, 5f);
            FootstepLerpMulti = ConstructFloatConfig(FootstepLerpMultiDefault, MISC_SLIDERS, "Footstep lerp speed multiplier", "The footstep effect is an animation, this changes the speed of the animation playback", 0f, 1f);
            FootstepIntesnityMulti = ConstructFloatConfig(6f, MISC_SLIDERS, "Footstep intensity multiplier", "", 0f, 10f);
            ThrowStrengthMulti = ConstructFloatConfig(ThrowStrengthMultiDefault, MISC_SLIDERS, "Throw visual effect multi", "Change the strength of the throw animation", 0f, 100f);
            ThrowSpeedMulti = ConstructFloatConfig(ThrowSpeedMultiDefault, MISC_SLIDERS, "Throw effect speed", "The throw effect is an animation, this changes the speed of the animation playback.", 0f, 10f);
            SideToSideSwayMulti = ConstructFloatConfig(0.01f, MISC_SLIDERS, "Footstep side-to-side value", "", 0f, 0.03f);
            SideToSideRotationDTMulti = ConstructFloatConfig(0.9f, MISC_SLIDERS, "Footstep side-to-side rotation speed", "", 0f, 10f);
            SideToSidePositionDTMulti = ConstructFloatConfig(0.35f, MISC_SLIDERS, "Footstep side-to-side position speed", "", 0f, 10f);
            FootstepCutoffRatio = ConstructFloatConfig(0.65f, MISC_SLIDERS, "Footstep animation cutoff", "Just don't touch this.", 0f, 1f);
            MotionTrackingThreshold = ConstructFloatConfig(0.005f, MISC_SLIDERS, "Motion tracking threshold", "Just don't touch this.", 0f, 0.01f);
            RunFadeDTMulti = ConstructFloatConfig(10f, MISC_SLIDERS, "Run Transition Animation Fade Speed Multi", "", 1f, 50f);
            RunPoseEnterSpeed = ConstructFloatConfig(2f, RUN_POSE, "Run pose enter smoothing", "How quickly the additive sprint pose moves through transition into the final sprint pose.", 1f, 60f);
            RunPoseExitSpeed = ConstructFloatConfig(3f, RUN_POSE, "Run pose release smoothing", "How quickly the additive sprint pose moves through transition back to the normal weapon pose.", 1f, 60f);
            RunPoseSprintTransitionPosition = ConstructVector3Config(new Vector3(0f, 0f, -0.06f), RUN_POSE, "Normal sprint transition position", "Additive weapon-root position used as connective tissue when entering or leaving normal sprint.");
            RunPoseSprintTransitionRotation = ConstructVector3Config(new Vector3(0.05f, -0.50f, -0.40f), RUN_POSE, "Normal sprint transition rotation", "Additive weapon-root rotation used as connective tissue when entering or leaving normal sprint.");
            RunPoseSprintPosition = ConstructVector3Config(new Vector3(0.04f, -0.10f, 0.02f), RUN_POSE, "Normal sprint final position", "Additive weapon-root held position while using the normal body sprint state.");
            RunPoseSprintRotation = ConstructVector3Config(new Vector3(-0.10f, 0.25f, 0.15f), RUN_POSE, "Normal sprint final rotation", "Additive weapon-root held rotation while using the normal body sprint state.");
            RunPoseHighReadyTransitionPosition = ConstructVector3Config(new Vector3(0.02f, -0.01f, -0.03f), RUN_POSE, "High ready transition position", "Additive weapon-root position used as connective tissue when entering or leaving Realism high-ready or tac-sprint.");
            RunPoseHighReadyTransitionRotation = ConstructVector3Config(new Vector3(0.02f, -0.02f, -0.02f), RUN_POSE, "High ready transition rotation", "Additive weapon-root rotation used as connective tissue when entering or leaving Realism high-ready or tac-sprint.");
            RunPoseHighReadyPosition = ConstructVector3Config(new Vector3(0.02f, -0.03f, -0.03f), RUN_POSE, "High ready final position", "Additive weapon-root held position while sprinting from Realism high-ready or tac-sprint.");
            RunPoseHighReadyRotation = ConstructVector3Config(new Vector3(0.02f, -0.04f, -0.02f), RUN_POSE, "High ready final rotation", "Additive weapon-root held rotation while sprinting from Realism high-ready or tac-sprint.");
            RotationAverageDTMulti = ConstructFloatConfig(80f, ROTATION_ENGINE_SLIDERS, "RotationAverageDTMulti", "Just don't touch this.", 0.1f, 100f);
            RotationHistoryClamp = ConstructFloatConfig(0.1f, ROTATION_ENGINE_SLIDERS, "RotationHistoryClamp", "Just don't touch this.", 0f, 1f);
            DirectionalSwayLateralPosValue = ConstructFloatConfig(-0.01f, DIRECTIONAL_SWAY, "Directional Sway Lateral Pos Value", "", -0.1f, 0.1f);
            DirectionalSwayProjectedPosValue = ConstructFloatConfig(-0.04f, DIRECTIONAL_SWAY, "Directional Sway Projected Pos Value", "", -0.1f, 0.1f);
            DirectionalSwayLateralRotValue = ConstructFloatConfig(0.2f, DIRECTIONAL_SWAY, "Directional Sway Lateral Rot Value", "", -0.2f, 0.2f);
            DirectionalSwayVerticalRotValue = ConstructFloatConfig(0.04f, DIRECTIONAL_SWAY, "Directional Sway Vertical Rot Value", "", -0.1f, 0.1f);
            DirectionalSwayLerpSpeed = ConstructFloatConfig(3f, DIRECTIONAL_SWAY, "Directional Sway Lerp Speed", "", 1f, 20f);
            DirectionalSwayLerpOnAds = ConstructFloatConfig(0.25f, DIRECTIONAL_SWAY, "Directional Sway % During ADS", "", 0f, 1f);
            AugmentedReloadSpeed = ConstructFloatConfig(1.3f, AUGMENTED_RELOAD, "Augmented reload speed modifier", "", 1f, 5f);
            AugmentedReloadSprintingDebuff = ConstructFloatConfig(0.5f, AUGMENTED_RELOAD, "Reloading while sprinting debuff %", "", 0.1f, 1f);

            DeadzoneInShortStock = ConstructFloatConfig(0.35f, DEADZONE, "Deadzone % in Short Stock Pose", "", -5f, 5f);
            DeadzoneInActiveAim = ConstructFloatConfig(0.5f, DEADZONE, "Deadzone % in Active Aim Pose", "", -5f, 5f);
            DeadzoneInVanilla = ConstructFloatConfig(0.65f, DEADZONE, "Deadzone % in BSG Pose", "", -5f, 5f);
            DeadzoneInADS = ConstructFloatConfig(0.2f, DEADZONE, "Deadzone % in ADS", "", -5f, 5f);
            DeadzoneInHighReady = ConstructFloatConfig(0.35f, DEADZONE, "Deadzone % in High Ready", "", -5f, 5f);
            DeadzoneInLowReady = ConstructFloatConfig(0.75f, DEADZONE, "Deadzone % in Low Ready", "", -5f, 5f);
            DeadzoneHeadFollowSpeedMulti = ConstructFloatConfig(10f, DEADZONE, "Headfollow Speed Multi", "", 0f, 20f);
            DeadzoneWeightForEfficiency = ConstructBoolConfig(defaultValue: true, DEADZONE, "Deadzone weighted for Efficiency", "Ties Efficiency stat (see documentation) to your camera deadzone; less efficiency will cause more lag in the camera to recenter");


            DebugFloat1 = ConstructFloatConfig(550f, GENERAL_SLIDERS, "Efficiency Indicator Position", "Adjust vertical position of the Efficiency Indicator", 400f, 600f);

            LoggingUpdateFrequency = ConstructFloatConfig(0.5f, "z - Debug", "Logging update frequency", "", 0.1f, 5f);
        }

        void Update()
        {
            DeltaTime = Mathf.Min(UnityEngine.Time.unscaledDeltaTime, FTDThresh);
            Time += UnityEngine.Time.unscaledDeltaTime;

            bool isInRaid = RaidState.IsInRaid;
            if (isInRaid != _wasInRaid)
            {
                ResetProceduralState();
                _wasInRaid = isInRaid;
            }

            DriveLerps();
        }

        void ResetProceduralState()
        {
            PlayerMotionController.Reset();
            EfficiencyController.Reset();
            ParallaxController.Reset();
            ParallaxAdsController.Reset();
            RunningFadeController.Reset();
            DirectionalSwayController.Reset();
            NewSwayController.Reset();
            FootstepController.Reset();
        }

        //
        // drive lerps
        //
        void DriveLerps()
        {
            if (!RaidState.IsInRaid)
            {
                return;
            }
            TIRLUtils.Update(DeltaTime);
            EfficiencyController.UpdateEfficiencyLerp(DeltaTime);
            //ThrowController.UpdateLerp(DeltaTime);
            AugmentedReloadController.Update();
            //RunningFadeController.UpdateRunningFadeOffsets(DeltaTime);
        }

        void FixedUpdate()
        {
            if (!RaidState.IsInRaid)
            {
                return;
            }

            float rawFixedDeltaTime = UnityEngine.Time.unscaledDeltaTime;
            FixedDeltaTime = Mathf.Min(rawFixedDeltaTime, FTDThresh);
            if (rawFixedDeltaTime > FTDThresh && IsLogging.Value)
            {
                TIRLUtils.LogError($"fixed deltaTime exceeds limits : {rawFixedDeltaTime} / {FTDThresh} -- movement updates clamped this frame");
            }
            PlayerMotionController.UpdateMovementMeasurementsInFDT(FixedDeltaTime);
            WeaponSelectionController.UpdateAnimationPump(FixedDeltaTime);
            NewDeadzoneController.Update(FixedDeltaTime);
            NewSwayController.UpdateLerp(FixedDeltaTime);
            HeadRotController.UpdateLerp(FixedDeltaTime);
            DirectionalSwayController.UpdateDirectionalSwayLerp(FixedDeltaTime);
            FootstepController.UpdateStep(FixedDeltaTime);
            SwayController.UpdateLerp(FixedDeltaTime);
            ParallaxAdsController.UpdateLerps(FixedDeltaTime);
            ParallaxController.Update(FixedDeltaTime);
        }

        void LateUpdate()
        {
            SwayController.IsSwayUpdatedThisFrame = false;
            DeadzoneController.DeadzoneUpdatedThisFrame = false;
        }

        static bool IsAdvancedConfigSection(string category)
        {
            return category != BASE_FEATURES_SECTION && category != GENERAL_SLIDERS;
        }

        static object[] BuildConfigTags(string category)
        {
            if (!IsAdvancedConfigSection(category))
            {
                return Array.Empty<object>();
            }

            return new object[]
            {
                new ConfigurationManagerAttributes
                {
                    IsAdvanced = true
                }
            };
        }

        ConfigEntry<float> ConstructFloatConfig(float defaultValue, string category, string descriptionShort, string descriptionFull, float min, float max)
        {
            ConfigEntry<float> result = ((BaseUnityPlugin)this).Config.Bind<float>(category, descriptionShort, defaultValue, new ConfigDescription(descriptionFull, (AcceptableValueBase)(object)new AcceptableValueRange<float>(min, max), BuildConfigTags(category)));
            return result;
        }

        ConfigEntry<Vector3> ConstructVector3Config(Vector3 defaultValue, string category, string descriptionShort, string descriptionFull)
        {
            ConfigEntry<Vector3> result = ((BaseUnityPlugin)this).Config.Bind<Vector3>(category, descriptionShort, defaultValue, new ConfigDescription(descriptionFull, (AcceptableValueBase)null, BuildConfigTags(category)));
            return result;
        }

        ConfigEntry<bool> ConstructBoolConfig(bool defaultValue, string category, string descriptionShort, string descriptionFull)
        {
            ConfigEntry<bool> result = ((BaseUnityPlugin)this).Config.Bind<bool>(category, descriptionShort, defaultValue, new ConfigDescription(descriptionFull, (AcceptableValueBase)null, BuildConfigTags(category)));
            return result;
        }

        ConfigEntry<T> ConstructEnumConfig<T>(T defaultValue, string category, string descriptionShort, string descriptionFull) where T : Enum
        {
            ConfigEntry<T> result = ((BaseUnityPlugin)this).Config.Bind<T>(category, descriptionShort, defaultValue, new ConfigDescription(descriptionFull, (AcceptableValueBase)null, BuildConfigTags(category)));
            return result;
        }

        void OnGUI()
        {
            if (RaidState.IsInRaid && PrimeMover.IsEfficiencyIndicator.Value)
            {
                float horizontalCoef = 40f;
                efficiencyIndicatorStyle.normal.textColor = Color.grey;
                efficiencyIndicatorStyle.fontSize = 30;
                float indicatorHorizontalPosR = (float)Screen.width / 2f + horizontalCoef * EfficiencyController.EfficiencyModifier;
                float indicatorHorizontalPosL = (float)Screen.width / 2f + horizontalCoef * EfficiencyController.EfficiencyModifier * -1f;
                float indicatorVerticalPos = (float)Screen.height / 2f + PrimeMover.DebugFloat1.Value;
                GUI.Label(new Rect(indicatorHorizontalPosR, indicatorVerticalPos, 40f, 40f), ".", efficiencyIndicatorStyle);
                GUI.Label(new Rect(indicatorHorizontalPosL, indicatorVerticalPos, 40f, 40f), ".", efficiencyIndicatorStyle);
            }
        }
    }
}

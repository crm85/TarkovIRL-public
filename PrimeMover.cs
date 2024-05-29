using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using RootMotion.FinalIK;
using UnityEngine.Assertions;

namespace TarkovIRL
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class PrimeMover : BaseUnityPlugin
    {
        private const string modGUID = "TarkovIRL";
        private const string modName = "TarkovIRL";
        private const string modVersion = "0.1";

        public float DeltaTime = 0;

        private readonly Harmony harmony = new Harmony(modGUID);

        public static PrimeMover Instance;


        public AnimationCurve weapSwayCurve;
        public AnimationCurve deadZoneCurve;

        bool pauseTimer = false;

        public float deltaTime = 0;
        public float timeTime = 0;


        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            Initialize();
        }

        void Initialize()
        {
            // enable patches
            //TryLoadPatch(new Player_get_RotationPatch());
            TryLoadPatch(new IsAimingPatch());
            TryLoadPatch(new LerpCameraPatch_ApplyUpdateSwayFactors());
            TryLoadPatch(new UpdateSwayFactorsPatch());
            TryLoadPatch(new SetHeadRotationPatch_ApplyDeadzone());
            TryLoadPatch(new LateUpdatePatch_RetrieveFAC());
            TryLoadPatch(new ProcessUpperbodyRotationPatch());

            Utils.Logger = this.Logger;

            weapSwayCurve = new AnimationCurve
            (
                new Keyframe(0f, -1f), new Keyframe(.1f, -1f), new Keyframe(.2f, 1f), new Keyframe(1f, 1f)
            );
        }

        void Update()
        {
            WeaponHandlingController.DeltaTime = deltaTime;
            WeaponHandlingController.Time = timeTime;
            Utils.PumpUpdate(Time.deltaTime);

            deltaTime += (Time.unscaledDeltaTime - deltaTime);
            timeTime += Time.unscaledDeltaTime;
        }

        void LateUpdate()
        {
            WeaponHandlingController.SwayUpdatedThisFrame = false;
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
                Utils.Log(true, "could not load " + patchName + " -- " + e);
                throw;
            }
        }
    }
}

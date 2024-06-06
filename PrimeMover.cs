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

            deadZoneCurve = new AnimationCurve
            (
                 new Keyframe(0f, 1f), new Keyframe(0.01f, 1.000172f), new Keyframe(0.02f, 1.000375f), new Keyframe(0.03f, 1.000588f), new Keyframe(0.04f, 1.00081f), new Keyframe(0.05f, 1.001041f), new Keyframe(0.05999999f, 1.001279f), new Keyframe(0.06999999f, 1.001524f), new Keyframe(0.07999999f, 1.001775f), new Keyframe(0.08999999f, 1.002031f), new Keyframe(0.09999999f, 1.002292f), new Keyframe(0.11f, 1.002556f), new Keyframe(0.12f, 1.002823f), new Keyframe(0.13f, 1.003093f), new Keyframe(0.14f, 1.003364f), new Keyframe(0.15f, 1.003636f), new Keyframe(0.16f, 1.003907f), new Keyframe(0.17f, 1.004177f), new Keyframe(0.18f, 1.004446f), new Keyframe(0.19f, 1.004712f), new Keyframe(0.2f, 1.004975f), new Keyframe(0.21f, 1.005234f), new Keyframe(0.22f, 1.005488f), new Keyframe(0.23f, 1.005737f), new Keyframe(0.24f, 1.005979f), new Keyframe(0.25f, 1.006214f), new Keyframe(0.26f, 1.00644f), new Keyframe(0.27f, 1.006659f), new Keyframe(0.28f, 1.006867f), new Keyframe(0.29f, 1.007066f), new Keyframe(0.3f, 1.007253f), new Keyframe(0.31f, 1.007428f), new Keyframe(0.32f, 1.007591f), new Keyframe(0.33f, 1.007741f), new Keyframe(0.3399999f, 1.007876f), new Keyframe(0.3499999f, 1.007996f), new Keyframe(0.3599999f, 1.008101f), new Keyframe(0.3699999f, 1.008189f), new Keyframe(0.3799999f, 1.00826f), new Keyframe(0.3899999f, 1.008312f), new Keyframe(0.3999999f, 1.008346f), new Keyframe(0.4099999f, 1.008361f), new Keyframe(0.4199999f, 1.008354f), new Keyframe(0.4299999f, 1.008327f), new Keyframe(0.4399998f, 1.008278f), new Keyframe(0.4499998f, 1.008206f), new Keyframe(0.4599998f, 1.00811f), new Keyframe(0.4699998f, 1.00799f), new Keyframe(0.4799998f, 1.007846f), new Keyframe(0.4899998f, 1.007675f), new Keyframe(0.4999998f, 1.007478f), new Keyframe(0.5099998f, 1.007253f), new Keyframe(0.5199998f, 1.007f), new Keyframe(0.5299998f, 1.006718f), new Keyframe(0.5399998f, 1.006406f), new Keyframe(0.5499998f, 1.006064f), new Keyframe(0.5599998f, 1.005691f), new Keyframe(0.5699998f, 1.005285f), new Keyframe(0.5799997f, 1.004847f), new Keyframe(0.5899997f, 1.004375f), new Keyframe(0.5999997f, 1.003868f), new Keyframe(0.6099997f, 1.003327f), new Keyframe(0.6199997f, 1.002749f), new Keyframe(0.6299997f, 1.002135f), new Keyframe(0.6399997f, 1.001483f), new Keyframe(0.6499997f, 1.000794f), new Keyframe(0.6599997f, 1.000426f), new Keyframe(0.6699997f, 1.00035f), new Keyframe(0.6799996f, 1.000105f), new Keyframe(0.6899996f, 0.9992316f), new Keyframe(0.6999996f, 0.9972684f), new Keyframe(0.7099996f, 0.9937554f), new Keyframe(0.7199996f, 0.988232f), new Keyframe(0.7299996f, 0.9802377f), new Keyframe(0.7399996f, 0.9693123f), new Keyframe(0.7499996f, 0.9549951f), new Keyframe(0.7599996f, 0.9368258f), new Keyframe(0.7699996f, 0.914344f), new Keyframe(0.7799996f, 0.8870892f), new Keyframe(0.7899995f, 0.8546009f), new Keyframe(0.7999995f, 0.8164188f), new Keyframe(0.8099995f, 0.7720823f), new Keyframe(0.8199995f, 0.7200972f), new Keyframe(0.8299995f, 0.6591832f), new Keyframe(0.8399995f, 0.5893648f), new Keyframe(0.8499995f, 0.510667f), new Keyframe(0.8599995f, 0.423115f), new Keyframe(0.8699995f, 0.3267338f), new Keyframe(0.8799995f, 0.2215486f), new Keyframe(0.8899994f, 0.1075844f), new Keyframe(0.8999994f, 0f), new Keyframe(0.9099994f, 0f), new Keyframe(0.9199994f, 0f), new Keyframe(0.9299994f, 0f), new Keyframe(0.9399994f, 0f), new Keyframe(0.9499994f, 0f), new Keyframe(0.9599994f, 0f), new Keyframe(0.9699994f, 0f), new Keyframe(0.9799994f, 0f), new Keyframe(0.9899994f, 0f), new Keyframe(0.9999993f, 0f)
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

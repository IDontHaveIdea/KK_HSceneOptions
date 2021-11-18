﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using Manager;

using static ChaFileDefine;
using static KK_HSceneOptions.SpeechControl;
using static KK_HSceneOptions.Utilities;


namespace KK_HSceneOptions
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class HSceneOptions : BaseUnityPlugin
    {
        public const string PluginName = "HSceneOptions";
        public const string Version = "3.2.1";

        internal static bool isVR;
        internal static bool isDarkness;

        internal static HFlag flags;
        internal static List<HActionBase> lstProc;
        internal static List<ChaControl> lstFemale;
        internal static List<ChaControl> lstmMale;
        internal static List<HSprite> sprites = new List<HSprite>();
        internal static HVoiceCtrl voice;
        internal static object[] hands = new object[2];

        internal static HCategory hCategory;

        internal static AnimationToggle animationToggle;
        internal static SpeechControl speechControl;


        public static ConfigEntry<bool> AutoSubAccessories { get; private set; }
        public static ConfigEntry<bool> DisableAutoFinish { get; private set; }
        public static ConfigEntry<bool> HideMaleShadow { get; private set; }
        public static ConfigEntry<bool> HideFemaleShadow { get; private set; }
        public static ConfigEntry<bool> ForceInsertInterrupt { get; private set; }
        public static ConfigEntry<bool> ControlPadInterrupt { get; private set; }
        public static ConfigEntry<PositionSkipMode> QuickPositionChange { get; private set; }

        public static ConfigEntry<bool> LockFemaleGauge { get; private set; }
        public static ConfigEntry<bool> LockMaleGauge { get; private set; }
        public static ConfigEntry<int> FemaleGaugeMin { get; private set; }
        public static ConfigEntry<int> FemaleGaugeMax { get; private set; }
        public static ConfigEntry<int> MaleGaugeMin { get; private set; }
        public static ConfigEntry<int> MaleGaugeMax { get; private set; }

        public static ConfigEntry<SpeechMode> SpeechControlMode { get; private set; }
        public static ConfigEntry<float> SpeechTimer { get; private set; }

        public static ConfigEntry<bool> VRResetCamera { get; private set; }

        public static ConfigEntry<bool> PrecumToggle { get; private set; }
        public static ConfigEntry<float> PrecumTimer { get; private set; }

        public static ConfigEntry<KeyboardShortcut> InsertWaitKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> InsertNowKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> OrgasmInsideKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> OrgasmOutsideKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> OLoopKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SpitKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SwallowKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> BottomClothesToggleKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> PantsuStripKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SubAccToggleKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> TopClothesToggleKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> TriggerVoiceKey { get; private set; }


        private void Start()
        {
            //Harmony patching
            var harmony = new Harmony(GUID);
            try
            {
                harmony.PatchAll(typeof(Hooks));

                if (isVR = Type.GetType("VRHScene, Assembly-CSharp") != null)
                    harmony.PatchAll(typeof(Hooks_VR));

                if (isDarkness = Type.GetType("H3PDarkSonyu, Assembly-CSharp") != null)
                    harmony.PatchAll(typeof(Hooks_Darkness));

            }
            catch (Exception)
            {
                harmony.UnpatchAll(harmony.Id);
                Destroy(this);

                Logger.LogError("Harmony patch failed, plugin now exiting");
                throw;
            }

            /// 
            /////////////////// Miscellaneous Configs //////////////////////////
            /// 

            AutoSubAccessories = Config.Bind(
                section: "",
                key: "Auto Equip Sub-Accessories",
                defaultValue: false,
                "Auto equip sub-accessories at H start");

            HideFemaleShadow = Config.Bind(
                section: "",
                key: "Hide Shadow Casted by Female Limbs and Accessories",
                defaultValue: false,
                "Hide shadow casted by female limbs and accessories. This does not affect shadows casted " +
                "by the head or hair");
            HideFemaleShadow.SettingChanged += (sender, args) => { FemaleShadow(); };

            HideMaleShadow = Config.Bind(
                section: "",
                key: "Hide Shadow Casted by Male Body",
                defaultValue: false);
            HideMaleShadow.SettingChanged += (sender, args) => { MaleShadow(); };

            ForceInsertInterrupt = Config.Bind(
                section: "",
                key: "Interrupt Speech When Force Insert",
                defaultValue: true,
                "Interrupt female speech when inserting without asking (blue button), allowing immediate " +
                "insertion.");

            ControlPadInterrupt = Config.Bind(
                section: "",
                key: "Interrupt Speech With Control Pad",
                defaultValue: true,
                "Allow skipping voice lines by left clicking the control pad");

            QuickPositionChange = Config.Bind(
                section: "",
                key: "Quick Position Change",
                defaultValue: PositionSkipMode.Auto,
                "Quickly switch between positions, skipping speech and maintaining motion if not changing " +
                "sex mode.\n\nAuto:  Activates quick switching only when switching between positions " +
                "within the same mode, and only during active sex when the characters are moving \n\n" +
                "Always:  Always activates quick switching (motion is only maintained within the same " +
                "sex mode)");

            /// 
            /////////////////// Excitement Gauge //////////////////////////
            /// 
            LockFemaleGauge = Config.Bind(
                section: "Excitement Gauge",
                key: "Auto Lock Female Gauge",
                defaultValue: false,
                "Auto lock female gauge at H start");

            LockMaleGauge = Config.Bind(
                section: "Excitement Gauge",
                key: "Auto Lock Male Gauge",
                defaultValue: false,
                "Auto lock male gauge at H start");

            FemaleGaugeMin = Config.Bind(
                section: "Excitement Gauge",
                key: "Female Excitement Gauge Minimum Value",
                defaultValue: 0,
                new ConfigDescription("Female excitement gauge will not fall below this value",
                    new AcceptableValueRange<int>(0, 100)));

            FemaleGaugeMax = Config.Bind(
                section: "Excitement Gauge",
                key: "Female Excitement Gauge Maximum Value",
                defaultValue: 100,
                new ConfigDescription("Female excitement gauge will not go above this value",
                    new AcceptableValueRange<int>(0, 100)));

            MaleGaugeMin = Config.Bind(
                section: "Excitement Gauge",
                key: "Male Excitement Gauge Minimum Value",
                defaultValue: 0,
                new ConfigDescription("Male excitement gauge will not fall below this value",
                    new AcceptableValueRange<int>(0, 100)));

            MaleGaugeMax = Config.Bind(
                section: "Excitement Gauge",
                key: "Male Excitement Gauge Maximum Value",
                defaultValue: 100,
                new ConfigDescription("Male excitement gauge will not go above this value",
                    new AcceptableValueRange<int>(0, 100)));

            /// 
            /////////////////// Female Speech //////////////////////////
            /// 
            SpeechControlMode = Config.Bind(
                section: "Female Speech",
                key: "Speech Control",
                defaultValue: SpeechMode.Disabled,
                "Default Behavior: Disable this feature and return to vanilla behavior" +
                    "\n\nBased on Timer: Automatically trigger speech at set interval" +
                    "\n\nMute Idle Speech: Prevent the girl from speaking at all during " +
                    "idle (she would still speak during events such as insertion)" +
                    "\n\nMute All Spoken Lines: Mute all speech other than moans" +
                    "\n(To prevent issues with the duration of precum or orgasm, set the " +
                    "Precum Timer to a few seconds when muting all speech)");

            SpeechTimer = Config.Bind(
                section: "Female Speech",
                key: "Speech Timer  (Effective only if Speech Control is set to Based on Timer)",
                defaultValue: 20f,
                new ConfigDescription("Sets the time interval at which the girl will randomly speak. " +
                            "In seconds.",
                        new AcceptableValueRange<float>(voiceMinInterval, voiceMaxInterval)));
            SpeechTimer.SettingChanged += (sender, args) => { SetVoiceTimer(); };

            /// 
            /////////////////// VR //////////////////////////
            /// 
            VRResetCamera = Config.Bind(
                section: "Official VR",
                key: "Reset Camera At Position Change",
                defaultValue: true,
                "Resets the camera back to the male's head when switching to a different position in official VR.");

            /// 
            /////////////////// Precum Related //////////////////////////
            /// 	
            DisableAutoFinish = Config.Bind(
                section: "Finish & Orgasm",
                key: "Disable Auto Finish in Service Modes",
                defaultValue: true,
                "If enabled, animation in service modes will not be stuck in the fast precum animation " +
                "when male's excitement gauge is past the 70% threshold");

            PrecumTimer = Config.Bind(
                section: "Finish & Orgasm",
                key: "Precum Timer",
                defaultValue: 0f,
                new ConfigDescription("When orgasm is initiated via the keyboard shortcuts or in-game menu," +
                        " animation will forcibly exit precum and enter orgasm after this many seconds." +
                        " \n\nSet to 0 to disable this.\n(It is recommended to set this timer to a few " +
                        "seconds when muting all speech to prevent issues with the duration of precum " +
                        "or orgasm)",
                    new AcceptableValueRange<float>(0, 13f)));

            PrecumToggle = Config.Bind(
                section: "Finish & Orgasm",
                key: "Precum Toggle",
                defaultValue: true,
                "Allow toggling through precum animation when right clicking the speed control pad." +
                "\n\nToggle order: weak motion > strong motion > precum > back to weak motion");

            /// 
            /////////////////// Keyboard Shortcuts //////////////////////////
            /// 
            const string sectionKeys = "Ξ  Keyboard Shortcuts";

            TopClothesToggleKey = Config.Bind(
                section: sectionKeys,
                key: "Toggle Top Clothes",
                defaultValue: KeyboardShortcut.Empty,
                new ConfigDescription("Toggle through states of the top clothes of the main female, " +
                        "including top cloth and bra at the same time.",
                    acceptableValues: null,
                    new ConfigurationManagerAttributes { Order = 3 }));

            BottomClothesToggleKey = Config.Bind(
                section: sectionKeys,
                key: "Toggle Bottom Clothes",
                defaultValue: KeyboardShortcut.Empty,
                new ConfigDescription("Toggle through states of the bottom cloth (skirt, pants...etc) of the main female.",
                    acceptableValues: null,
                    new ConfigurationManagerAttributes { Order = 2 }));

            PantsuStripKey = Config.Bind(
                section: sectionKeys,
                key: "Toggle Pantsu Stripped/Half Stripped",
                defaultValue: KeyboardShortcut.Empty,
                new ConfigDescription("Toggle between a fully stripped and a partially stripped pantsu." +
                        "\n(You would not be able to fully dress the pantsu with this shortcut)",
                    acceptableValues: null,
                    new ConfigurationManagerAttributes { Order = 1 }));

            InsertWaitKey = Config.Bind(
                section: sectionKeys,
                key: "Insert After Asking Female",
                defaultValue: KeyboardShortcut.Empty,
                "Insert male genital after female speech");

            InsertNowKey = Config.Bind(
                section: sectionKeys,
                key: "Insert Without Asking",
                defaultValue: KeyboardShortcut.Empty,
                "Insert male genital without asking for permission");

            OrgasmInsideKey = Config.Bind(
                section: sectionKeys,
                key: "Orgasm Inside",
                defaultValue: KeyboardShortcut.Empty,
                "Press this key to manually cum inside mouth or vagina");

            OrgasmOutsideKey = Config.Bind(
                section: sectionKeys,
                key: "Orgasm Outside",
                defaultValue: KeyboardShortcut.Empty,
                "Press this key to manually cum outside of mouth or vagina");

            OLoopKey = Config.Bind(
                section: sectionKeys,
                key: "Precum Loop Toggle",
                defaultValue: KeyboardShortcut.Empty,
                "Press this key to enter/exit precum animation");

            SpitKey = Config.Bind(
                section: sectionKeys,
                key: "Spit Out",
                defaultValue: KeyboardShortcut.Empty,
                "Press this key to make female spit out after blowjob");

            SwallowKey = Config.Bind(
                section: sectionKeys,
                key: "Swallow",
                defaultValue: KeyboardShortcut.Empty,
                "Press this key to make female swallow after blowjob");

            SubAccToggleKey = Config.Bind(
                section: sectionKeys,
                key: "Toggle Sub-Accessories",
                defaultValue: KeyboardShortcut.Empty,
                "Toggle the display of sub-accessories");

            TriggerVoiceKey = Config.Bind(
                section: sectionKeys,
                key: "Trigger Speech",
                defaultValue: KeyboardShortcut.Empty,
                "Trigger a voice line based on the current context");
        }

        /// <summary>
        /// Function to equip all accessories
        /// </summary>
        internal static void EquipAllAccessories(List<ChaControl> females)
        {
            if (AutoSubAccessories.Value)
            {
                foreach (ChaControl chaCtrl in females)
                {
                    chaCtrl.SetAccessoryStateAll(true);
                }
            }
        }

        /// <summary>
        ///Function to lock female/male gauge depending on config
        /// </summary>
        internal static void LockGaugesAction(HSprite hSprite)
        {
            if (LockFemaleGauge.Value)
            {
                hSprite.OnFemaleGaugeLockOnGauge();
                hSprite.flags.lockGugeFemale = true;
            }

            if (LockMaleGauge.Value)
            {
                hSprite.OnMaleGaugeLockOnGauge();
                hSprite.flags.lockGugeMale = true;
            }
        }

        /// <summary>
        ///Function to enable/disable shadow from male body
        /// </summary>
        internal static void MaleShadow()
        {
            if (!flags)
            {
                return;
            }

            ShadowCastingMode maleShadowMode = HideMaleShadow.Value ? ShadowCastingMode.Off : ShadowCastingMode.On;

            foreach (ChaControl male in lstmMale)
            {
                foreach (Renderer mesh in male.objRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if (mesh.name != "o_shadowcaster_cm")
                    {
                        mesh.shadowCastingMode = maleShadowMode;
                    }
                }
            }
        }

        /// <summary>
        /// Function to enable/disable shadow from female limbs and accessories
        /// </summary>
        internal static void FemaleShadow()
        {
            if (!flags)
            {
                return;
            }

            ShadowCastingMode femaleShadowMode = HideFemaleShadow.Value ? ShadowCastingMode.Off : ShadowCastingMode.On;

            foreach (ChaControl female in lstFemale)
            {
                foreach (Transform child in female.objTop.transform)
                {
                    if (child.name == "p_cf_body_bone")
                    {
                        foreach (MeshRenderer mesh in child.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            mesh.shadowCastingMode = femaleShadowMode;
                        }
                    }
                    else
                    {
                        foreach (SkinnedMeshRenderer mesh in child.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        {
                            if (mesh.name != "o_shadowcaster")
                            {
                                mesh.shadowCastingMode = femaleShadowMode;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Wait for current animation transition to finish, then run the given delegate
        /// </summary>
        internal static IEnumerator RunAfterTransition(Action action)
        {
            yield return new WaitUntil(
                () => lstFemale[0]?.animBody.GetCurrentAnimatorStateInfo(0).IsName(flags.nowAnimStateName) ?? true);

            action();
        }

        /// <summary>
        /// Function to limit excitement gauges based on configured values
        /// </summary>
        internal static void GaugeLimiter()
        {
            if (FemaleGaugeMax.Value >= FemaleGaugeMin.Value)
            {
                flags.gaugeFemale = Mathf.Clamp(flags.gaugeFemale, FemaleGaugeMin.Value, FemaleGaugeMax.Value);
            }
            if (MaleGaugeMax.Value >= MaleGaugeMin.Value)
            {
                flags.gaugeMale = Mathf.Clamp(flags.gaugeMale, MaleGaugeMin.Value, MaleGaugeMax.Value);
            }
        }

        internal void OnInsertNoVoiceClick()
        {
            int num = (flags.mode == HFlag.EMode.houshi3P || flags.mode == HFlag.EMode.sonyu3P) ? (flags.nowAnimationInfo.id % 2) : 0;
            if (flags.mode != HFlag.EMode.sonyu3PMMF)
            {
                if (flags.isInsertOK[num] || flags.isDebug)
                {
                    flags.click = HFlag.ClickKind.insert_voice;
                    return;
                }
                if (flags.isCondom)
                {
                    flags.click = HFlag.ClickKind.insert_voice;
                    return;
                }
                flags.AddNotCondomPlay();
                int num2 = ((flags.mode == HFlag.EMode.sonyu3P) ? ((!flags.nowAnimationInfo.isFemaleInitiative) ? 500 : 538) : ((Game.isAddH && flags.nowAnimationInfo.isFemaleInitiative) ? 38 : 0));
                flags.voice.playVoices[num] = 302 + num2;
                flags.voice.SetSonyuIdleTime();
                flags.isDenialvoiceWait = true;
                if (flags.mode == HFlag.EMode.houshi3P || flags.mode == HFlag.EMode.sonyu3P)
                {
                    int num3 = num ^ 1;
                    if (voice.nowVoices[num3].state == HVoiceCtrl.VoiceKind.voice && Voice.IsPlay(flags.transVoiceMouth[num3]))
                    {
                        Voice.Stop(flags.transVoiceMouth[num3]);
                    }
                }
            }
            else
            {
                flags.click = HFlag.ClickKind.insert_voice;
            }
        }

        private void OnInsertClick()
        {
            int num2 = (flags.mode == HFlag.EMode.houshi3P || flags.mode == HFlag.EMode.sonyu3P) ? (flags.nowAnimationInfo.id % 2) : 0;
            int num = ((flags.mode == HFlag.EMode.sonyu3P) ? ((!flags.nowAnimationInfo.isFemaleInitiative) ? 500 : 538) : ((Game.isAddH && flags.nowAnimationInfo.isFemaleInitiative) ? 38 : 0));
            if (flags.mode != HFlag.EMode.sonyu3PMMF)
            {
                if (flags.isInsertOK[num2] || flags.isDebug)
                {
                    flags.click = HFlag.ClickKind.insert;
                    flags.voice.playVoices[num2] = 301 + num;
                }
                else if (flags.isCondom)
                {
                    flags.click = HFlag.ClickKind.insert;
                    flags.voice.playVoices[num2] = 301 + num;
                }
                else
                {
                    flags.AddNotCondomPlay();
                    flags.voice.playVoices[num2] = 302 + num;
                    flags.voice.SetSonyuIdleTime();
                    flags.isDenialvoiceWait = true;
                }
            }
            else
            {
                flags.click = HFlag.ClickKind.insert;
                flags.voice.playVoices[num2] = 1001;
            }
            if (flags.mode == HFlag.EMode.houshi3P || flags.mode == HFlag.EMode.sonyu3P)
            {
                int num3 = num2 ^ 1;
                if (voice.nowVoices[num3].state == HVoiceCtrl.VoiceKind.voice && Voice.IsPlay(flags.transVoiceMouth[num3]))
                {
                    Voice.Stop(flags.transVoiceMouth[num3]);
                }
            }
        }

        /// <summary>
        /// Display or hide all accessories within the specified category of the dominant female. Display 
        /// them if more than half of the accessories in that category are currently hidden, otherwise hide them.
        /// </summary>
        /// <param name="category">The category of accessories to affect. 0 = main, 1 = sub</param>
        private void ToggleMainGirlAccessories(int category)
        {
            //In modes with two females, use flags.nowAnimationInfo.id to determine which girl's accessories should be affected.
            ChaControl mainFemale = lstFemale[(flags.mode == HFlag.EMode.houshi3P || flags.mode == HFlag.EMode.sonyu3P) ? flags.nowAnimationInfo.id % 2 : 0];

            float categoryCount = 0;
            float categoryShown = 0;
            //Iterate through the vanilla accessories list and update the total count of the accessories in
            //the specified category, as well as the count of accessories in that category that are currently hidden
            for (int i = 0; i < mainFemale.nowCoordinate.accessory.parts.Length; i++)
            {
                if (mainFemale.nowCoordinate.accessory.parts[i].hideCategory == category)
                {
                    categoryCount++;
                    if (mainFemale.fileStatus.showAccessory[i])
                    {
                        categoryShown++;
                    }
                }
            }

            //Determine if MoreAccessories is present. If yes, iterate through its list of accessories.
            var moreAccsType = Type.GetType("MoreAccessoriesKOI.MoreAccessories, MoreAccessories");
            if (moreAccsType != null)
            {
                try
                {
                    var moreAccsObj = Traverse.Create(moreAccsType).Field("_self").GetValue();
                    var dic = Traverse.Create(moreAccsObj).Field("_accessoriesByChar").GetValue();
                    var accessoriesByChar = GetValueWeakDict(dic, mainFemale.chaFile);

                    if (accessoriesByChar == null)
                    {
                        throw new NullReferenceException("Failed to acquire reference to MoreAccessories._accessoriesByChar");
                    }

                    var charAddDataTraverse = Traverse.Create(accessoriesByChar);
                    var nowAccessories = charAddDataTraverse.Field("nowAccessories").GetValue<List<ChaFileAccessory.PartsInfo>>();
                    var showAccessories = charAddDataTraverse.Field("showAccessories").GetValue<List<bool>>();

                    for (int i = 0; i < nowAccessories.Count; i++)
                    {
                        if (nowAccessories[i].hideCategory == category)
                        {
                            if (showAccessories[i])
                            {
                                categoryShown++;
                            }

                            categoryCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }

            mainFemale.SetAccessoryStateCategory(category, categoryShown < (categoryCount / 2));
        }


        /// <summary>
        /// Toggle through the states of the kinds of clothes provided in the parameter while keeping their 
        /// states synchronized, using the state of the first cloth in the parameter as basis.
        /// </summary>
        /// <param name="clotheSelection">The list of clothes that should be affected.</param>
        /// <param name="partialOnly">Whether to toggle through fully dressed and fully stripped states</param>
        private void SetClothesStateRange(ClothesKind[] clotheSelection, bool partialOnly = false)
        {
            //In modes with two females, use flags.nowAnimationInfo.id to determine which girl's clothes should be affected.
            int femaleIndex = (flags.mode == HFlag.EMode.houshi3P || flags.mode == HFlag.EMode.sonyu3P) ? flags.nowAnimationInfo.id % 2 : 0;

            //Trigger the next state of the first cloth provided by the parameters, then use its state to
            //synchronize other clothes in the parameter.
            //  If partialOnly is true, only toggle between the two partially stripped states
            byte state = lstFemale[femaleIndex].fileStatus.clothesState[(int)clotheSelection[0]];
            if (partialOnly)
            {
                lstFemale[femaleIndex].SetClothesState((int)clotheSelection[0], (byte)((state % 2) + 1), false);
            }
            else
            {
                lstFemale[femaleIndex].SetClothesStateNext((int)clotheSelection[0]);
            }

            state = lstFemale[femaleIndex].fileStatus.clothesState[(int)clotheSelection[0]];

            for (int i = 1; i < clotheSelection.Length; i++)
            {
                lstFemale[femaleIndex].SetClothesState((int)clotheSelection[i], state, next: false);
            }
        }

        private enum ClothesState
        {
            Full,
            Open1,
            Open2,
            Nude
        }

        public enum SpeechMode
        {
            [Description("Default Behavior")]
            Disabled,
            [Description("Based on Timer")]
            Timer,
            [Description("Mute Idle Speech")]
            MuteIdle,
            [Description("Mute All Spoken Lines")]
            MuteAll
        }

        public enum PositionSkipMode
        {
            Disabled,
            Auto,
            Always
        }

        internal enum HCategory
        {
            service,
            intercourse,
            maleNotVisible
        }
    }
}

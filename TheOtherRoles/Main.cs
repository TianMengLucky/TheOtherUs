﻿using System;
using System.Reflection;
using System.Text;
using AmongUs.Data;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using InnerNet;
using Reactor.Networking;
using Reactor.Networking.Attributes;
using TheOtherRoles.CustomCosmetics;
using TheOtherRoles.Modules.Languages;
using TheOtherRoles.Options;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles;

[BepInAutoPlugin("me.spex.theotherus")]
[BepInDependency(SubmergedCompatibility.SUBMERGED_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInProcess("Among Us.exe")]
[ReactorModFlags(ModFlags.RequireOnAllClients)]
public partial class TheOtherRolesPlugin : BasePlugin
{
    public static uint betaDays = 0; // amount of days for the build to be usable (0 for infinite!)

    public static readonly Version version = System.Version.Parse(Version);
    public static Main Instance;

    public static int optionsPage = 2;

    public static Sprite ModStamp;

    public static IRegionInfo[] defaultRegions;
    public Harmony Harmony { get; } = new(Id);

    public static ConfigEntry<string> DebugMode { get; private set; }
    public static ConfigEntry<bool> GhostsSeeInformation { get; set; }
    public static ConfigEntry<bool> GhostsSeeRoles { get; set; }
    public static ConfigEntry<bool> GhostsSeeModifier { get; set; }
    public static ConfigEntry<bool> GhostsSeeVotes { get; set; }
    public static ConfigEntry<bool> ShowRoleSummary { get; set; }
    public static ConfigEntry<bool> ShowLighterDarker { get; set; }
    public static ConfigEntry<bool> EnableSoundEffects { get; set; }
    public static ConfigEntry<bool> EnableHorseMode { get; set; }
    public static ConfigEntry<bool> ToggleCursor { get; set; }
    public static ConfigEntry<bool> ShowVentsOnMap { get; set; }
    public static ConfigEntry<string> Ip { get; set; }
    public static ConfigEntry<ushort> Port { get; set; }
    public static ConfigEntry<string> ShowPopUpVersion { get; set; }


    // This is part of the Mini.RegionInstaller, Licensed under GPLv3
    // file="RegionInstallPlugin.cs" company="miniduikboot">
    public static void UpdateRegions()
    {
        var serverManager = FastDestroyableSingleton<ServerManager>.Instance;
        var regions = new[]
        {
            new StaticHttpRegionInfo("Custom", StringNames.NoTranslation, Ip.Value,
                new Il2CppReferenceArray<ServerInfo>(
                    [new ServerInfo("Custom", Ip.Value, Port.Value, false)])).CastFast<IRegionInfo>()
        };

        var currentRegion = serverManager.CurrentRegion;
        Info($"Adding {regions.Length} regions");
        foreach (var region in regions)
            if (region == null)
            {
                Error("Could not add region");
            }
            else
            {
                if (currentRegion != null && region.Name.Equals(currentRegion.Name, StringComparison.OrdinalIgnoreCase))
                    currentRegion = region;
                serverManager.AddOrUpdateRegion(region);
            }

        // AU remembers the previous region that was set, so we need to restore it
        if (currentRegion == null) return;
        Debug("Resetting previous region");
        serverManager.SetRegion(currentRegion);
    }

    public override void Load()
    {
        if (ConsoleManager.ConsoleEnabled) System.Console.OutputEncoding = Encoding.UTF8;
        SetLogSource(Log);
        InitConsole();
        Instance = this;

        _ = Helpers.checkBeta(); // Exit if running an expired beta
        _ = CredentialsPatch.MOTD.loadMOTDs();

        DebugMode = Config.Bind("Custom", "Enable Debug Mode", "false");
        GhostsSeeInformation = Config.Bind("Custom", "Ghosts See Remaining Tasks", true);
        GhostsSeeRoles = Config.Bind("Custom", "Ghosts See Roles", true);
        GhostsSeeModifier = Config.Bind("Custom", "Ghosts See Modifier", true);
        GhostsSeeVotes = Config.Bind("Custom", "Ghosts See Votes", true);
        ShowRoleSummary = Config.Bind("Custom", "Show Role Summary", true);
        ShowLighterDarker = Config.Bind("Custom", "Show Lighter / Darker", false);
        ToggleCursor = Config.Bind("Custom", "Better Cursor", true);
        EnableSoundEffects = Config.Bind("Custom", "Enable Sound Effects", true);
        EnableHorseMode = Config.Bind("Custom", "Enable Horse Mode", false);
        ShowPopUpVersion = Config.Bind("Custom", "Show PopUp", "0");
        ShowVentsOnMap = Config.Bind("Custom", "Show vent positions on minimap", false);

        Ip = Config.Bind("Custom", "Custom Server IP", "127.0.0.1");
        Port = Config.Bind("Custom", "Custom Server Port", (ushort)22023);
        defaultRegions = ServerManager.DefaultRegions;

        UpdateRegions();

        DebugMode = Config.Bind("Custom", "Enable Debug Mode", "false");
        Harmony.PatchAll();

        if (ToggleCursor.Value) Helpers.enableCursor(true);
        if (BepInExUpdater.UpdateRequired)
        {
            AddComponent<BepInExUpdater>();
            return;
        }

        AddComponent<ModUpdater>();

        SubmergedCompatibility.Initialize();
        MainMenuPatch.addSceneChangeCallbacks();
        _ = RoleInfo.loadReadme();
        AddToKillDistanceSetting.addKillDistance();

        TaskQueue.Instance
            .StartTask(() => 
            { 
                AttributeManager.Instance
                    .SetInit()
                    .Add<MonoRegisterAndDontDestroy>()
                    .Add<RegisterRole>(_RoleManager)
                    .Add<OnEvent>()
                    .Add<RPCMethod>()
                    .Add<RPCListener>()
                    .Start();  
            }, "RegisterAttributes")
            .StartTask(CosmeticsManager.Instance.DefConfigCreateAndInit, "DefConfigCreate");

        Info("Loading TOR completed!");
    }

    internal static void OnTranslationController_Initialized_Load()
    {
        DependentDownload.Instance.CheckLoad();
        DependentDownload.Instance.DownLoadDependentMap("https://raw.githubusercontent.com/SpexGH/TheOtherUs/the-other-us/LoadDependent/");

        TaskQueue.Instance
            .StartTask(() => DependentDownload.Instance.DownLoadDependentFormMap("Csv"), "LoadDependentFormMap Csv")
            .StartTask(() => DependentDownload.Instance.DownLoadDependentFormMap("Excel"), "LoadDependentFormMap Excel")
            .StartTask(LanguageManager.Instance.Load, "LoadLanguage")
            .StartTask(CustomOptionHolder.Load, "LoadOption")
            .StartTask(CustomColors.Load, "LoadColor");
        
        Info("OnTranslationController_Initialized_Load End");
    }
}

// Deactivate bans, since I always leave my local testing game and ban myself
[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.AmBanned), MethodType.Getter)]
public static class AmBannedPatch
{
    public static void Postfix(out bool __result)
    {
        __result = false;
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Awake))]
public static class ChatControllerAwakePatch
{
    private static void Prefix()
    {
        if (!EOSManager.Instance.isKWSMinor)
            DataManager.Settings.Multiplayer.ChatMode = QuickChatModes.FreeChatOrQuickChat;
    }
}

/*// Debugging tools
[HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
public static class DebugManager
{
    //private static readonly string passwordHash = "d1f51dfdfd8d38027fd2ca9dfeb299399b5bdee58e6c0b3b5e9a45cd4e502848";
    private static readonly Random random = new((int)DateTime.Now.Ticks);
    private static readonly List<PlayerControl> bots = [];

    public static void Postfix(KeyboardJoystick __instance)
    {
        // Spawn dummys
        if (AmongUsClient.Instance.AmHost && Input.GetKeyDown(KeyCode.F) && Input.GetKey(KeyCode.RightShift))
        {
            var playerControl = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
            var i = playerControl.PlayerId = (byte)GameData.Instance.GetAvailableId();

            bots.Add(playerControl);
            GameData.Instance.AddPlayer(playerControl);
            AmongUsClient.Instance.Spawn(playerControl);

            playerControl.transform.position = CachedPlayer.LocalPlayer.transform.position;
            playerControl.GetComponent<DummyBehaviour>().enabled = true;
            playerControl.NetTransform.enabled = false;
            playerControl.SetName(RandomString(10));
            playerControl.SetColor((byte)random.Next(Palette.PlayerColors.Length));
            GameData.Instance.RpcSetTasks(playerControl.PlayerId, new byte[0]);
        }

        // Terminate round
        if (AmongUsClient.Instance.AmHost && Helpers.gameStarted && Input.GetKeyDown(KeyCode.Return) &&
            Input.GetKey(KeyCode.L) && Input.GetKey(KeyCode.LeftShift))
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.Control.NetId,
                (byte)CustomRPC.ForceEnd, SendOption.Reliable);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.forceEnd();
        }

        if (Input.GetKeyDown(KeyCode.Return) && Input.GetKey(KeyCode.M) && Input.GetKey(KeyCode.LeftShift) &&
            MeetingHud.Instance)
        {
            MeetingHud.Instance.RpcClose();
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc == null || pc.Data.IsDead || pc.Data.Disconnected)
                    continue;
        }
    }

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}*/
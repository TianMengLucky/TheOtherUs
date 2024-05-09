using System;
using System.Collections.Generic;
using TheOtherRoles.Objects;
using System.Linq;
using UnityEngine;
using Hazel;
using TheOtherRoles.Modules.Options;

namespace TheOtherRoles.Roles.Neutral;

[RegisterRole]
public class Arsonist : RoleBase
{
    public PlayerControl arsonist;
    public Color color = new Color32(238, 112, 46, byte.MaxValue);

    public float cooldown = 30f;
    public float duration = 3f;
    public bool triggerArsonistWin;

    public PlayerControl currentTarget;
    public PlayerControl douseTarget;
    public List<PlayerControl> dousedPlayers = new();

    private ResourceSprite douseSprite = new("DouseButton.png");
    private ResourceSprite igniteSprite = new ("IgniteButton.png");

    public CustomOption arsonistSpawnRate;
    public CustomOption arsonistCooldown;
    public CustomOption arsonistDuration;

    public CustomButton arsonistButton;

    public bool dousedEveryoneAlive()
    {
        return CachedPlayer.AllPlayers.All(x =>
        {
            return x.PlayerControl == arsonist || x.Data.IsDead || x.Data.Disconnected ||
                   dousedPlayers.Any(y => y.PlayerId == x.PlayerId);
        });
    }

    public override void ClearAndReload()
    {
        arsonist = null;
        currentTarget = null;
        douseTarget = null;
        triggerArsonistWin = false;
        dousedPlayers = new List<PlayerControl>();
        foreach (var p in TORMapOptions.playerIcons.Values.Where(p => p != null && p.gameObject != null))
            p.gameObject.SetActive(false);
        cooldown = arsonistCooldown.getFloat();
        duration = arsonistDuration.getFloat();
    }

    public override void OptionCreate()
    {
        arsonistSpawnRate = new CustomOption(290, "Arsonist".ColorString(color), CustomOptionHolder.rates, null, true);
        arsonistCooldown = new CustomOption(291, "Arsonist Cooldown", 12.5f, 2.5f, 60f, 2.5f, arsonistSpawnRate);
        arsonistDuration = new CustomOption(292, "Arsonist Douse Duration", 3f, 1f, 10f, 1f, arsonistSpawnRate);
    }
    public override void ButtonCreate(HudManager _hudManager)
    {
        // Arsonist button
        arsonistButton = new CustomButton(
            () =>
            {
                //var dousedEveryoneAlive = dousedEveryoneAlive();
                if (dousedEveryoneAlive())
                {
                    var winWriter = AmongUsClient.Instance.StartRpcImmediately(
                        CachedPlayer.LocalPlayer.Control.NetId, (byte)CustomRPC.ArsonistWin, SendOption.Reliable);
                    AmongUsClient.Instance.FinishRpcImmediately(winWriter);
                    RPCProcedure.arsonistWin();
                    arsonistButton.HasEffect = false;
                }
                else if (currentTarget != null)
                {
                    if (Helpers.checkAndDoVetKill(currentTarget)) return;
                    Helpers.checkWatchFlash(currentTarget);
                    douseTarget = currentTarget;
                    arsonistButton.HasEffect = true;
                    SoundEffectsManager.play("arsonistDouse");
                }
            },
            () =>
            {
                return arsonist != null && arsonist == CachedPlayer.LocalPlayer.Control &&
                       !CachedPlayer.LocalPlayer.Data.IsDead;
            },
            () =>
            {
                //var dousedEveryoneAlive = dousedEveryoneAlive();
                if (!dousedEveryoneAlive())
                    ButtonHelper.showTargetNameOnButton(currentTarget, arsonistButton, "");
                if (dousedEveryoneAlive()) arsonistButton.actionButton.graphic.sprite = igniteSprite;

                if (!arsonistButton.isEffectActive || douseTarget == currentTarget)
                    return CachedPlayer.LocalPlayer.Control.CanMove &&
                           (dousedEveryoneAlive() || currentTarget != null);
                douseTarget = null;
                arsonistButton.Timer = 0f;
                arsonistButton.isEffectActive = false;

                return CachedPlayer.LocalPlayer.Control.CanMove &&
                       (dousedEveryoneAlive() || currentTarget != null);
            },
            () =>
            {
                arsonistButton.Timer = arsonistButton.MaxTimer;
                arsonistButton.isEffectActive = false;
                douseTarget = null;
            },
            douseSprite,
            CustomButton.ButtonPositions.lowerRowRight,
            _hudManager,
            KeyCode.F,
            true,
            duration,
            () =>
            {
                if (douseTarget != null) dousedPlayers.Add(douseTarget);

                arsonistButton.Timer = dousedEveryoneAlive() ? 0 : arsonistButton.MaxTimer;

                foreach (var p in dousedPlayers)
                    if (TORMapOptions.playerIcons.ContainsKey(p.PlayerId))
                        TORMapOptions.playerIcons[p.PlayerId].setSemiTransparent(false);

                // Ghost Info
                var writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.Control.NetId,
                    (byte)CustomRPC.ShareGhostInfo, SendOption.Reliable);
                writer.Write(CachedPlayer.LocalPlayer.PlayerId);
                writer.Write((byte)RPCProcedure.GhostInfoTypes.ArsonistDouse);
                writer.Write(douseTarget.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

                douseTarget = null;
            }
        );
    }
    public override void ResetCustomButton()
    {
        arsonistButton.MaxTimer = cooldown;
        arsonistButton.EffectDuration = duration;
    }
    public override RoleInfo RoleInfo { get; protected set; }
    public override Type RoleType { get; protected set; }
}
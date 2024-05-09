using System;
using System.Collections.Generic;
using TheOtherRoles.Modules.Options;
using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

[RegisterRole]
public class Seer : RoleBase
{
    public PlayerControl seer;
    public List<Vector3> deadBodyPositions = new();

    public float soulDuration = 15f;
    public bool limitSoulDuration;
    public int mode;

    private ResourceSprite soulSprite = new ("Soul.png", 500f);
    
    public override void ClearAndReload()
    {
        seer = null;
        deadBodyPositions = [];
        limitSoulDuration = seerLimitSoulDuration;
        soulDuration = seerSoulDuration;
        mode = seerMode.getSelection();
    }
    
    public CustomOption seerSpawnRate;
    public CustomOption seerMode;
    public CustomOption seerSoulDuration;
    public CustomOption seerLimitSoulDuration;
    public override void OptionCreate()
    {
        seerSpawnRate = new CustomOption(160, "Seer".ColorString(roleInfo.Color), CustomOptionHolder.rates, null, true);
        seerMode = new CustomOption(161, "Seer Mode",
            ["Show Death Flash + Souls", "Show Death Flash", "Show Souls"], seerSpawnRate);
        seerLimitSoulDuration =
            new CustomOption(163, "Seer Limit Soul Duration", false, seerSpawnRate);
        seerSoulDuration = new CustomOption(162, "Seer Soul Duration", 15f, 0f, 120f, 5f,
            seerLimitSoulDuration);
    }


    public static readonly RoleInfo roleInfo = new()
    {
        RoleType = CustomRoleType.Main,
        Color = new Color32(97, 178, 108, byte.MaxValue),
        Name = nameof(Seer),
        RoleId = RoleId.Seer,
        RoleTeam = RoleTeam.Crewmate,
        IntroInfo = "You will see players die",
        Description = "You will see players die",
        GetRole = Get<Seer>
    };

    public override RoleInfo RoleInfo { get; protected set; } = roleInfo;
    
    public override Type RoleType { get; protected set; } = typeof(Seer);
}
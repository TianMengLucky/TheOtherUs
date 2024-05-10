using System;
using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

[RegisterRole]
public class Crew : RoleBase
{
    public Color color = Palette.White;
    public PlayerControl crew;

    public override RoleInfo RoleInfo { get; protected set; }
    public override Type RoleType { get; protected set; }

    public override void ClearAndReload()
    {
        crew = null;
    }
}
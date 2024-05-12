using System;
using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

[RegisterRole]
public class Godfather : RoleBase
{
    public Color color = Palette.ImpostorRed;
    public PlayerControl godfather;

    public override RoleInfo RoleInfo { get; protected set; }
    public override Type RoleType { get; protected set; }

    public override void ClearAndReload()
    {
        godfather = null;
    }
}
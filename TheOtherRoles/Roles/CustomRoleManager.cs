using System.Collections.Generic;
using System.Linq;

namespace TheOtherRoles.Roles;

public class CustomRoleManager : ManagerBase<CustomRoleManager>
{
    public readonly List<RoleBase> _AllActiveRole = [];

    public readonly List<RoleControllerBase> _AllControllerBases = [];
    public readonly List<RoleBase> _RoleBases = [];

    public readonly List<RoleControllerBase> LocalControllerBases = [];

    public readonly List<RoleBase> LocalRoleBases = [];

    public readonly Dictionary<RoleBase, List<PlayerControl>> PlayerAndRoles = new();

    public void Register(RoleBase role)
    {
        _RoleBases.Add(role);
    }

    public void UnSetRole(RoleBase @base, PlayerControl player)
    {
        PlayerAndRoles[@base].Remove(player);
        UpdateActiveRole();
        if (player != CachedPlayer.LocalPlayer) return;
        foreach (var ControllerBase in LocalControllerBases)
        {
            LocalControllerBases.Remove(ControllerBase);
            ControllerBase.Dispose();
        }

        foreach (var Base in LocalRoleBases) LocalRoleBases.Remove(Base);
    }

    public void SetRole(RoleBase @base, PlayerControl player)
    {
        PlayerAndRoles[@base].Add(player);
        var controller = @base.RoleInfo.CreateRoleController(player);
        UpdateActiveRole();

        if (player != CachedPlayer.LocalPlayer) return;
        LocalRoleBases.Add(@base);
        LocalControllerBases.Add(controller);
    }

    public void UpdateActiveRole()
    {
        foreach (var dic in PlayerAndRoles)
        {
            if (dic.Value.Any() && !_AllActiveRole.Contains(dic.Key))
                _AllActiveRole.Add(dic.Key);

            if (!dic.Value.Any() && _AllActiveRole.Contains(dic.Key))
                _AllActiveRole.Remove(dic.Key);
        }
    }

    public void ShifterRole(PlayerControl player, RoleBase target)
    {
        UnSetRole(player.GetMainRole(), player);
        SetRole(target, player);
    }

    public void Update(HudManager __instance)
    {
        if (!_AllControllerBases.Any() || !_AllActiveRole.Any()) return;

        foreach (var Base in _AllControllerBases) Base.Update(__instance);
    }

    public void ClearAndReload()
    {
        foreach (var role in _RoleBases)
            role.ClearAndReload();
    }
}
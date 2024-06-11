using System;
using Hazel;
using TheOtherUs.Modules.Compatibility;
using TheOtherUs.Roles.Impostors;
using UnityEngine;

namespace TheOtherUs.Objects;

public class Bomb
{
    private static Sprite bombSprite;
    private static Sprite backgroundSprite;
    private static Sprite defuseSprite;
    public static bool canDefuse;
    public GameObject background;
    public GameObject bomb;

    public Bomb(Vector2 p)
    {
        bomb = new GameObject("Bomb") { layer = 11 };
        bomb.AddSubmergedComponent(SubmergedCompatibility.Classes.ElevatorMover);
        var position = new Vector3(p.x, p.y, (p.y / 1000) + 0.001f); // just behind player
        bomb.transform.position = position;

        background = new GameObject("Background") { layer = 11 };
        background.transform.SetParent(bomb.transform);
        background.transform.localPosition = new Vector3(0, 0, -1f); // before player
        background.transform.position = position;

        var bombRenderer = bomb.AddComponent<SpriteRenderer>();
        bombRenderer.sprite = getBombSprite();
        var backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = getBackgroundSprite();

        bomb.SetActive(false);
        background.SetActive(false);
        if (CachedPlayer.LocalPlayer.Control == Bomber.bomber) bomb.SetActive(true);
        Bomber.bomb = this;
        var c = Color.white;
        var g = Color.red;
        backgroundRenderer.color = Color.white;
        Bomber.isActive = false;

        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(Bomber.bombActiveAfter,
            new Action<float>(x =>
            {
                if (x == 1f && this != null)
                {
                    bomb.SetActive(true);
                    background.SetActive(true);
                    SoundEffectsManager.playAtPosition("bombFuseBurning", p, Bomber.destructionTime, Bomber.hearRange,
                        true);
                    Bomber.isActive = true;

                    FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(Bomber.destructionTime,
                        new Action<float>(x =>
                        {
                            // can you feel the pain?
                            var combinedColor = (Mathf.Clamp01(x) * g) + (Mathf.Clamp01(1 - x) * c);
                            if (backgroundRenderer) backgroundRenderer.color = combinedColor;
                            if (x == 1f && this != null) explode(this);
                        })));
                }
            })));
    }

    public static Sprite getBombSprite()
    {
        if (bombSprite) return bombSprite;
        bombSprite = UnityHelper.loadSpriteFromResources("TheOtherUs.Resources.Bomb.png", 300f);
        return bombSprite;
    }

    public static Sprite getBackgroundSprite()
    {
        if (backgroundSprite) return backgroundSprite;
        backgroundSprite =
            UnityHelper.loadSpriteFromResources("TheOtherUs.Resources.BombBackground.png", 110f / Bomber.hearRange);
        return backgroundSprite;
    }

    public static Sprite getDefuseSprite()
    {
        if (defuseSprite) return defuseSprite;
        defuseSprite = UnityHelper.loadSpriteFromResources("TheOtherUs.Resources.Bomb_Button_Defuse.png", 115f);
        return defuseSprite;
    }

    public static void explode(Bomb b)
    {
        if (b == null) return;
        if (Bomber.bomber != null)
        {
            var position = b.bomb.transform.position;
            var distance =
                Vector2.Distance(position,
                    CachedPlayer.LocalPlayer.transform
                        .position); // every player only checks that for their own client (desynct with positions sucks)
            if (distance < Bomber.destructionRange && !CachedPlayer.LocalPlayer.Data.IsDead)
            {
                Helpers.checkMurderAttemptAndKill(Bomber.bomber, CachedPlayer.LocalPlayer.Control, false, false, true,
                    true);

                var writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.Control.NetId,
                    (byte)CustomRPC.ShareGhostInfo, SendOption.Reliable);
                writer.Write(CachedPlayer.LocalPlayer.PlayerId);
                writer.Write((byte)RPCProcedure.GhostInfoTypes.DeathReasonAndKiller);
                writer.Write(CachedPlayer.LocalPlayer.PlayerId);
                writer.Write((byte)DeadPlayer.CustomDeathReason.Bomb);
                writer.Write(Bomber.bomber.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                GameHistory.overrideDeathReasonAndKiller(CachedPlayer.LocalPlayer, DeadPlayer.CustomDeathReason.Bomb,
                    Bomber.bomber);
            }

            SoundEffectsManager.playAtPosition("bombExplosion", position, range: Bomber.hearRange);
        }

        Bomber.clearBomb();
        canDefuse = false;
        Bomber.isActive = false;
    }

    public static void update()
    {
        if (Bomber.bomb == null || !Bomber.isActive)
        {
            canDefuse = false;
            return;
        }

        Bomber.bomb.background.transform.Rotate(Vector3.forward * 50 * Time.fixedDeltaTime);

        if (MeetingHud.Instance && Bomber.bomb != null) Bomber.clearBomb();

        if (Vector2.Distance(CachedPlayer.LocalPlayer.Control.GetTruePosition(), Bomber.bomb.bomb.transform.position) >
            1f) canDefuse = false;
        else canDefuse = true;
    }

    public static void clearBackgroundSprite()
    {
        backgroundSprite = null;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Supremo do Guerreiro (E com a Ressonância cheia):
// LÂMINA RUBRA: CINCO LUAS DE FOGO. Roda dentro do SpecialController.Run(), depois do cut-in.
// 5 meias-luas de fogo que atravessam inimigos + 1 corte gigante final.
public static class CrimsonCrescents
{
    public const int WaveCount = 5;
    public const float WaveDamageScale = 0.7f;   // Força 90 → 63 por onda
    public const float FinalDamageScale = 1.6f;  // Força 90 → 144 no corte final
    public const float WaveInterval = 0.16f;
    public const float WaveKnockX = 1.5f, WaveKnockY = 0.5f, WaveStun = 0.2f;
    public const float FinalKnockX = 5f, FinalKnockY = 2.5f, FinalStun = 0.6f;
    // altura do centro de cada onda acima dos pés (unidades)
    static readonly float[] WaveHeights = { 1.0f, 1.3f, 0.8f, 1.2f, 1.0f };
    static readonly Color Gold = new Color(1f, 0.839f, 0.251f);

    static Sprite[] _poses;
    static GuerreiroVisual _visual;
    static SpriteRenderer _visualRenderer;
    static Sprite _savedSprite;

    public static IEnumerator Run(Transform owner, CharacterData hero, PlayerController player, PlayerCombat combat)
    {
        if (owner == null)
            yield break;

        float dir = player != null && player.Facing.x < 0f ? -1f : 1f;
        float strength = hero != null ? hero.Strength : 90f;
        var body = owner.GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);

        BeginPose(owner);
        try
        {
            Pose(0);
            yield return new WaitForSeconds(0.18f);

            for (int i = 0; i < WaveCount; i++)
            {
                Pose(0);
                yield return new WaitForSeconds(0.05f);
                Pose(i % 2 == 0 ? 1 : 2);
                GameAudio.Emit(GameAudio.Cue.WarriorCrescent, owner.position, .75f);
                FireCrescent.Spawn(owner, Feet(owner) + new Vector3(dir * 0.9f, WaveHeights[i], 0f), dir,
                    strength * WaveDamageScale, false);
                CameraFollow.Shake(0.06f, 0.05f);
                yield return new WaitForSeconds(WaveInterval - 0.05f);
            }

            Pose(3);
            yield return new WaitForSeconds(0.30f);

            Pose(4);
            GameAudio.Emit(GameAudio.Cue.WarriorFinal, owner.position);
            FireCrescent.Spawn(owner, Feet(owner) + new Vector3(dir * 1.4f, 1.6f, 0f), dir,
                strength * FinalDamageScale, true);
            RajadaFx.Flash(Feet(owner) + new Vector3(dir * 1.3f, 1.1f, 0f));
            SupremeFx.PlayFlash();
            CameraFollow.Shake(0.3f, 0.2f);
            SupremeFx.Popup(Feet(owner) + Vector3.up * 2.6f, SpecialJapanese.WarriorImpact, Gold);
            yield return new WaitForSeconds(0.25f);

            Pose(5);
            yield return new WaitForSeconds(0.25f);

            float wait = 0f;
            while (FireCrescent.Alive > 0 && wait < 1.2f)
            {
                wait += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            EndPose();
        }
    }

    public static Vector3 Feet(Transform owner)
    {
        var col = owner.GetComponent<Collider2D>();
        return col != null ? new Vector3(owner.position.x, col.bounds.min.y, 0f)
                           : owner.position + Vector3.down * 0.5f;
    }

    // Desliga o GuerreiroVisual e mostra as 6 poses de supremo_rajada. O flip vem do localScale do pai.
    static void BeginPose(Transform owner)
    {
        if (_poses == null)
            _poses = TrioAnimationFrames.Load("guerreiro", "supremo_rajada", 6);
        _visual = owner.GetComponentInChildren<GuerreiroVisual>(true);
        _visualRenderer = _visual != null ? _visual.GetComponent<SpriteRenderer>() : null;
        if (_visual == null || _visualRenderer == null || _poses == null)
        {
            _visual = null;
            _visualRenderer = null;
            return;
        }
        _savedSprite = _visualRenderer.sprite;
        _visual.enabled = false;
    }

    static void Pose(int i)
    {
        if (_visualRenderer != null && _poses != null && i >= 0 && i < _poses.Length)
            _visualRenderer.sprite = _poses[i];
    }

    // Idempotente: chamado no fim da rajada e também no SpecialController.Finish().
    public static void EndPose()
    {
        if (_visualRenderer != null && _savedSprite != null)
            _visualRenderer.sprite = _savedSprite;
        if (_visual != null)
            _visual.enabled = true;
        _visual = null;
        _visualRenderer = null;
        _savedSprite = null;
    }
}

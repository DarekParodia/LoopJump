using UnityEngine;

/// <summary>
/// Odtwarza animacje sprite'ów w stylu DOOM.
/// Pobiera aktualny kierunek z DoomBillboard i stan z DoomEnemyController.
/// </summary>
[RequireComponent(typeof(DoomBillboard))]
public class DoomSpriteAnimator : MonoBehaviour
{
    public enum AnimState
    {
        Idle,
        Walk,
        Attack,
        Pain,
        Death
    }

    [Header("Dane sprite'ów")]
    public DoomEnemySprites sprites;

    // Aktualny stan animacji
    public AnimState CurrentState { get; private set; } = AnimState.Idle;

    // Czy animacja śmierci/bólu dobiegła końca?
    public bool IsAnimationFinished { get; private set; }

    private DoomBillboard billboard;
    private float timer;
    private int currentFrame;
    private AnimState pendingState;
    private bool hasPendingState;

    void Awake()
    {
        billboard = GetComponent<DoomBillboard>();
    }

    void Update()
    {
        if (sprites == null) return;

        DoomEnemySprites.DirectionalAnimation anim = GetCurrentAnim();
        if (anim == null || anim.frames == null || anim.frames.Length == 0) return;

        // Tick timera animacji
        float frameDuration = 1f / Mathf.Max(anim.fps, 0.1f);
        timer += Time.deltaTime;

        if (timer >= frameDuration)
        {
            timer -= frameDuration;
            AdvanceFrame(anim);
        }

        // Aktualizuj sprite na podstawie kierunku i klatki
        Sprite frame = anim.GetFrame(billboard.CurrentDirection, currentFrame);
        if (frame != null)
            billboard.SpriteRenderer.sprite = frame;
    }

    void AdvanceFrame(DoomEnemySprites.DirectionalAnimation anim)
    {
        currentFrame++;

        if (currentFrame >= anim.FrameCount)
        {
            if (anim.loop)
            {
                currentFrame = 0;
            }
            else
            {
                // Animacja jednorazowa dobiegła końca
                currentFrame = anim.FrameCount - 1;
                IsAnimationFinished = true;

                // Jeśli czekał inny stan – przełącz
                if (hasPendingState)
                {
                    hasPendingState = false;
                    SetState(pendingState);
                }
            }
        }
    }

    // ─── Publiczne API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Natychmiast ustawia stan animacji.
    /// </summary>
    
    public void SetState(AnimState newState)
    {
        if (CurrentState == newState && newState != AnimState.Pain)
            return;
	
        CurrentState = newState;
        currentFrame = 0;
        timer = 0f;
        IsAnimationFinished = false;
        hasPendingState = false;
	
        // FIX: Immediately apply frame 0 so there's no blank frame
        ApplyCurrentFrame();
    }
	
    private void ApplyCurrentFrame()
    {
        if (sprites == null || billboard == null) return;
        DoomEnemySprites.DirectionalAnimation anim = GetCurrentAnim();
        if (anim == null || anim.frames == null || anim.frames.Length == 0)
            return;
	
        Sprite frame = anim.GetFrame(
            billboard.CurrentDirection,
            currentFrame
        );
        if (frame != null)
            billboard.SpriteRenderer.sprite = frame;
    }

    /// <summary>
    /// Ustawia stan, który zostanie odtworzony PO zakończeniu aktualnej animacji jednorazowej.
    /// Przydatne np. po Pain → wróć do Walk.
    /// </summary>
    public void SetStateQueued(AnimState immediate, AnimState afterFinish)
    {
        SetState(immediate);
        pendingState = afterFinish;
        hasPendingState = true;
    }

    /// <summary>
    /// Zwraca aktualną konfigurację animacji.
    /// </summary>
    DoomEnemySprites.DirectionalAnimation GetCurrentAnim()
    {
        if (sprites == null) return null;

        return CurrentState switch
        {
            AnimState.Idle   => sprites.idle,
            AnimState.Walk   => sprites.walk,
            AnimState.Attack => sprites.attack,
            AnimState.Pain   => sprites.pain,
            AnimState.Death  => sprites.death,
            _                => sprites.idle
        };
    }
}

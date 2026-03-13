using UnityEngine;

/// <summary>
/// ScriptableObject przechowujący wszystkie sprite'y przeciwnika w stylu DOOM.
/// Każdy stan animacji ma 8 kierunków × N klatek.
/// 
/// Jak zorganizować sprite sheet:
///   Kierunki (0–7): Front, Front-Right, Right, Back-Right, Back, Back-Left, Left, Front-Left
///   Stany: Idle, Walk, Attack, Pain, Death
/// </summary>
[CreateAssetMenu(fileName = "NewDoomEnemy", menuName = "Doom/Enemy Sprites")]
public class DoomEnemySprites : ScriptableObject
{
    [System.Serializable]
    public class DirectionalAnimation
    {
        [Tooltip("8 kierunków × N klatek. Indeks: kierunek * framesPerDirection + klatka")]
        public Sprite[] frames;

        [Tooltip("Ile klatek na jeden kierunek")]
        public int framesPerDirection = 1;

        [Tooltip("Prędkość odtwarzania (klatki na sekundę)")]
        public float fps = 8f;

        [Tooltip("Czy animacja ma się zapętlać?")]
        public bool loop = true;

        /// <summary>
        /// Zwraca sprite dla danego kierunku (0–7) i klatki animacji.
        /// </summary>
        public Sprite GetFrame(int direction, int frameIndex)
        {
            if (frames == null || frames.Length == 0) return null;

            direction = Mathf.Clamp(direction, 0, 7);
            int totalFrames = framesPerDirection > 0 ? framesPerDirection : 1;
            frameIndex = frameIndex % totalFrames;

            int index = direction * totalFrames + frameIndex;
            if (index >= frames.Length) return null;

            return frames[index];
        }

        public int FrameCount => framesPerDirection;
    }

    [Header("Stany animacji")]
    public DirectionalAnimation idle;
    public DirectionalAnimation walk;
    public DirectionalAnimation attack;
    public DirectionalAnimation pain;   // animacja trafienia
    public DirectionalAnimation death;  // odtworzona raz, bez pętli

    private void OnValidate()
    {
        // Death nigdy nie zapętla
        if (death != null) death.loop = false;
        if (pain != null) pain.loop = false;
    }
}

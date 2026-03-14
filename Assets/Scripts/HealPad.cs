using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class HealPad : MonoBehaviour
{
    [Header("Settings")]
    public float healAmount = 2f;
    public float cooldown = 1f;

    [Header("UI")]
    [SerializeField] private TMP_Text cooldownText;

    // Tracks when each player can be healed again.
    private readonly Dictionary<PlayerHealth, float> _nextHealTimeByPlayer = new();
    private PlayerHealth _currentPlayerInTrigger;

    void OnTriggerEnter(Collider other)
    {
        TryHeal(other);
    }

    void OnTriggerStay(Collider other)
    {
        _currentPlayerInTrigger = other != null ? other.GetComponent<PlayerHealth>() : null;
        TryHeal(other);
        UpdateCooldownText(_currentPlayerInTrigger);
    }

    void OnTriggerExit(Collider other)
    {
        if (other == null) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        _nextHealTimeByPlayer.Remove(playerHealth);

        if (_currentPlayerInTrigger == playerHealth)
        {
            _currentPlayerInTrigger = null;
            UpdateCooldownText(null);
        }
    }

    void Update()
    {
        if (_currentPlayerInTrigger != null)
            UpdateCooldownText(_currentPlayerInTrigger);
    }

    private void TryHeal(Collider other)
    {
        if (other == null) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        if (!_nextHealTimeByPlayer.TryGetValue(playerHealth, out float nextAllowedTime))
            nextAllowedTime = 0f;

        if (Time.time < nextAllowedTime) return;

        playerHealth.Heal(healAmount);
        _nextHealTimeByPlayer[playerHealth] = Time.time + Mathf.Max(0f, cooldown);
    }

    public float GetCooldownLeftSeconds(Collider other)
    {
        if (other == null) return 0f;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        return GetCooldownLeftSeconds(playerHealth);
    }

    public float GetCooldownLeftSeconds(PlayerHealth playerHealth)
    {
        if (playerHealth == null) return 0f;
        if (!_nextHealTimeByPlayer.TryGetValue(playerHealth, out float nextAllowedTime)) return 0f;

        return Mathf.Max(0f, nextAllowedTime - Time.time);
    }

    private void UpdateCooldownText(PlayerHealth playerHealth)
    {
        if (cooldownText == null) return;

        float cooldownLeft = GetCooldownLeftSeconds(playerHealth);
        cooldownText.text = cooldownLeft > 0f ? cooldownLeft.ToString("0.0") : string.Empty;
    }
}
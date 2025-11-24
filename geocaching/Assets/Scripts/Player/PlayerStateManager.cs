using System;
using UnityEngine;

/// <summary>
/// Управление состояниями игрока (нормальное/спотыкание/падение).
/// </summary>
[AddComponentMenu("Gameplay/Player State Manager")]
public class PlayerStateManager : MonoBehaviour
{
    public enum PlayerState
    {
        Normal,      // Нормальное состояние
        Stumbling,   // Спотыкание
        Falling      // Падение
    }
    
    [Header("State Durations")]
    [Tooltip("Длительность состояния спотыкания (сек)")]
    public float stumblingDuration = 2f;
    
    [Tooltip("Длительность состояния падения (сек)")]
    public float fallingDuration = 3f;
    
    // Текущее состояние
    private PlayerState currentState = PlayerState.Normal;
    
    // События состояний
    public event Action OnStumblingStarted;
    public event Action OnFallingStarted;
    public event Action OnRecovered;
    
    // Свойство для доступа к текущему состоянию
    public PlayerState CurrentState => currentState;
    public bool IsNormal => currentState == PlayerState.Normal;
    public bool IsStumbling => currentState == PlayerState.Stumbling;
    public bool IsFalling => currentState == PlayerState.Falling;
    
    private Coroutine currentStateCoroutine;
    
    /// <summary>
    /// Запускает состояние спотыкания.
    /// </summary>
    public void StartStumbling()
    {
        if (currentState == PlayerState.Normal)
        {
            SetState(PlayerState.Stumbling);
            Debug.Log($"[PlayerStateManager] Событие: СПОТЫКНУЛСЯ. Длительность: {stumblingDuration} сек.");
            OnStumblingStarted?.Invoke();
            
            // Запускаем корутину для автоматического восстановления
            if (currentStateCoroutine != null)
            {
                StopCoroutine(currentStateCoroutine);
            }
            currentStateCoroutine = StartCoroutine(StumblingCoroutine());
        }
    }
    
    /// <summary>
    /// Запускает состояние падения.
    /// </summary>
    public void StartFalling()
    {
        if (currentState == PlayerState.Normal)
        {
            SetState(PlayerState.Falling);
            Debug.Log($"[PlayerStateManager] Событие: УПАЛ. Длительность: {fallingDuration} сек.");
            OnFallingStarted?.Invoke();
            
            // Запускаем корутину для автоматического восстановления
            if (currentStateCoroutine != null)
            {
                StopCoroutine(currentStateCoroutine);
            }
            currentStateCoroutine = StartCoroutine(FallingCoroutine());
        }
    }
    
    /// <summary>
    /// Принудительно восстанавливает нормальное состояние.
    /// </summary>
    public void Recover()
    {
        if (currentState != PlayerState.Normal)
        {
            PlayerState previousState = currentState;
            
            if (currentStateCoroutine != null)
            {
                StopCoroutine(currentStateCoroutine);
                currentStateCoroutine = null;
            }
            
            SetState(PlayerState.Normal);
            Debug.Log($"[PlayerStateManager] Событие: ПРИШЁЛ В СЕБЯ (было состояние: {previousState}).");
            OnRecovered?.Invoke();
        }
    }
    
    private void SetState(PlayerState newState)
    {
        currentState = newState;
    }
    
    private System.Collections.IEnumerator StumblingCoroutine()
    {
        yield return new WaitForSeconds(stumblingDuration);
        Recover();
    }
    
    private System.Collections.IEnumerator FallingCoroutine()
    {
        yield return new WaitForSeconds(fallingDuration);
        Recover();
    }
}


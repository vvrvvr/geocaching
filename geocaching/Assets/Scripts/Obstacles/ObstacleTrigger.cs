using UnityEngine;

/// <summary>
/// Компонент для обработки триггера препятствия.
/// Передаёт информацию о границах зон при коллизии с игроком.
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("Gameplay/Obstacle Trigger")]
public class ObstacleTrigger : MonoBehaviour
{
    [Header("Obstacle Reference")]
    [Tooltip("Ссылка на скрипт препятствия с настройками зон")]
    public ObstacleSpeedColorDiscrete obstacleSpeedColor;
    
    [Header("Collider")]
    [Tooltip("Коллайдер-триггер для обнаружения игрока. Если не указан, будет найден автоматически.")]
    public Collider triggerCollider;
    
    [Header("Settings")]
    [Tooltip("Тег игрока для проверки коллизий")]
    public string playerTag = "Player";
    
    void Awake()
    {
        // Проверяем наличие коллайдера и выводим ошибку, если его нет
        if (triggerCollider == null)
        {
            Debug.LogError($"[ObstacleTrigger] Нет коллайдера на объекте '{gameObject.name}'. Добавьте Collider компонент и перетащите его в поле 'Trigger Collider' или установите на этом же GameObject.", this);
            return;
        }
        
        // Убеждаемся, что коллайдер является триггером
        triggerCollider.isTrigger = true;
        
        // Если препятствие не указано, пытаемся найти его на этом же объекте
        if (obstacleSpeedColor == null)
        {
            obstacleSpeedColor = GetComponent<ObstacleSpeedColorDiscrete>();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Проверяем, что это игрок
        if (!other.CompareTag(playerTag))
            return;
        
        // Передаём информацию о границах зон
        if (obstacleSpeedColor != null)
        {
            obstacleSpeedColor.GetZoneBoundaries(
                out float greenEnd, 
                out float yellowEnd, 
                out float redStart
            );
            
            // Уведомляем систему о входе в препятствие через синглтон
            if (ObstacleInteractionHandler.Instance != null)
            {
                ObstacleInteractionHandler.Instance.OnEnterObstacle(greenEnd, yellowEnd, redStart, obstacleSpeedColor);
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        // Проверяем, что это игрок
        if (!other.CompareTag(playerTag))
            return;
        
            // Уведомляем систему о выходе из препятствия через синглтон
            if (ObstacleInteractionHandler.Instance != null)
            {
                ObstacleInteractionHandler.Instance.OnExitObstacle();
            }
    }
}


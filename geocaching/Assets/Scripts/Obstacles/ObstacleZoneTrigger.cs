using UnityEngine;

/// <summary>
/// Компонент для обработки триггера препятствия.
/// Передаёт информацию о границах зон при коллизии с игроком.
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("Gameplay/Obstacle Zone Trigger")]
public class ObstacleZoneTrigger : MonoBehaviour
{
    [Header("Obstacle Reference")]
    [Tooltip("Ссылка на скрипт препятствия с настройками зон")]
    public ObstacleZoneVisualizer obstacleSpeedColor;
    
    [Header("Collider")]
    [Tooltip("Коллайдер-триггер для обнаружения игрока. Если не указан, будет найден автоматически.")]
    public Collider triggerCollider;
    
    [Header("Settings")]
    [Tooltip("Тег игрока для проверки коллизий")]
    public string playerTag = "Player";
    
    [Header("Priority")]
    [Tooltip("Приоритет препятствия. Чем выше значение, тем выше приоритет. Если игрок одновременно касается нескольких препятствий, используется то, у которого приоритет выше.")]
    [Range(0, 100)]
    public int priority = 0;
    
    void Awake()
    {
        // Проверяем наличие коллайдера и выводим ошибку, если его нет
        if (triggerCollider == null)
        {
            Debug.LogError($"[ObstacleZoneTrigger] Нет коллайдера на объекте '{gameObject.name}'. Добавьте Collider компонент и перетащите его в поле 'Trigger Collider' или установите на этом же GameObject.", this);
            return;
        }
        
        // Убеждаемся, что коллайдер является триггером
        triggerCollider.isTrigger = true;
        
        // Если препятствие не указано, пытаемся найти его на этом же объекте
        if (obstacleSpeedColor == null)
        {
            obstacleSpeedColor = GetComponent<ObstacleZoneVisualizer>();
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
            if (ObstacleZoneHandler.Instance != null)
            {
                ObstacleZoneHandler.Instance.OnEnterObstacle(greenEnd, yellowEnd, redStart, obstacleSpeedColor, priority, this);
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        // Проверяем, что это игрок
        if (!other.CompareTag(playerTag))
            return;
        
            // Уведомляем систему о выходе из препятствия через синглтон
            if (ObstacleZoneHandler.Instance != null)
            {
                ObstacleZoneHandler.Instance.OnExitObstacle(this);
            }
    }
}


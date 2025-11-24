using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Обработчик взаимодействия игрока с препятствиями.
/// Проверяет зоны при нажатии пробела и вызывает соответствующие события.
/// </summary>
[AddComponentMenu("Gameplay/Obstacle Zone Handler")]
public class ObstacleZoneHandler : MonoBehaviour
{
    // Статический доступ к экземпляру синглтона
    private static ObstacleZoneHandler instance;
    public static ObstacleZoneHandler Instance => instance;
    
    [Header("References")]
    [Tooltip("Контроллер состояния игрока")]
    public PlayerStateController playerStateManager;
    
    
    // Структура для хранения данных о препятствии
    private struct ObstacleData
    {
        public ObstacleZoneVisualizer obstacle;
        public float greenEnd;
        public float yellowEnd;
        public float redStart;
        public int priority;
        public ObstacleZoneTrigger trigger;
    }
    
    // Словарь активных препятствий (ключ - ObstacleZoneTrigger, значение - данные препятствия)
    private Dictionary<ObstacleZoneTrigger, ObstacleData> activeObstacles = new Dictionary<ObstacleZoneTrigger, ObstacleData>();
    
    // Текущее активное препятствие (с максимальным приоритетом)
    private ObstacleData? currentActiveObstacle = null;
    
    // Флаг, что игрок находится в зоне препятствия
    private bool isInObstacle => activeObstacles.Count > 0;
    
    void Awake()
    {
        // Регистрация экземпляра для статического доступа
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.LogWarning("Multiple ObstacleZoneHandler instances detected. Only one should exist.");
            return;
        }
        
        // Если контроллер состояния не указан, пытаемся найти его на этом же объекте
        if (playerStateManager == null)
        {
            playerStateManager = GetComponent<PlayerStateController>();
        }
    }
    
    void OnDestroy()
    {
        // Очистка статической ссылки при уничтожении
        if (instance == this)
        {
            instance = null;
        }
    }
    
    /// <summary>
    /// Вызывается при нажатии клавиши спринта для проверки зон препятствий.
    /// </summary>
    public void OnSprintInput()
    {
        // Проверяем зону только если игрок в нормальном состоянии и в зоне препятствия
        if (isInObstacle && 
            playerStateManager != null && 
            playerStateManager.IsNormal)
        {
            CheckZoneAndTriggerEvent();
        }
    }
    
    /// <summary>
    /// Вызывается при входе игрока в зону препятствия.
    /// </summary>
    public void OnEnterObstacle(float greenEnd, float yellowEnd, float redStart, ObstacleZoneVisualizer obstacle, int priority, ObstacleZoneTrigger trigger)
    {
        // Добавляем препятствие в словарь активных
        ObstacleData data = new ObstacleData
        {
            obstacle = obstacle,
            greenEnd = greenEnd,
            yellowEnd = yellowEnd,
            redStart = redStart,
            priority = priority,
            trigger = trigger
        };
        
        activeObstacles[trigger] = data;
        
        // Обновляем текущее активное препятствие (с максимальным приоритетом)
        UpdateActiveObstacle();
    }
    
    /// <summary>
    /// Вызывается при выходе игрока из зоны препятствия.
    /// </summary>
    public void OnExitObstacle(ObstacleZoneTrigger trigger)
    {
        // Удаляем препятствие из словаря активных
        if (activeObstacles.ContainsKey(trigger))
        {
            activeObstacles.Remove(trigger);
        }
        
        // Обновляем текущее активное препятствие (с максимальным приоритетом)
        UpdateActiveObstacle();
    }
    
    /// <summary>
    /// Обновляет текущее активное препятствие на основе приоритетов.
    /// </summary>
    private void UpdateActiveObstacle()
    {
        if (activeObstacles.Count == 0)
        {
            // Нет активных препятствий - сбрасываем
            currentActiveObstacle = null;
            
            if (SprintController.Instance != null)
            {
                SprintController.Instance.ClearActiveObstacle();
            }
            return;
        }
        
        // Находим препятствие с максимальным приоритетом
        var maxPriorityObstacle = activeObstacles.Values
            .OrderByDescending(o => o.priority)
            .FirstOrDefault();
        
        // Если текущее активное препятствие изменилось, обновляем
        if (!currentActiveObstacle.HasValue || 
            currentActiveObstacle.Value.obstacle != maxPriorityObstacle.obstacle ||
            currentActiveObstacle.Value.priority != maxPriorityObstacle.priority)
        {
            currentActiveObstacle = maxPriorityObstacle;
            
            // Обновляем границы зон в SprintController для отображения на слайдерах
            if (SprintController.Instance != null)
            {
                SprintController.Instance.SetActiveObstacle(maxPriorityObstacle.obstacle);
            }
        }
    }
    
    /// <summary>
    /// Проверяет текущую зону скорости и вызывает соответствующее событие.
    /// </summary>
    private void CheckZoneAndTriggerEvent()
    {
        if (playerStateManager == null || !isInObstacle || !currentActiveObstacle.HasValue)
            return;
        
        ObstacleData activeData = currentActiveObstacle.Value;
        
        // Получаем текущую нормализованную скорость
        float currentSpeed = SprintController.NormalizedSpeed;
        
        // Определяем зону на основе текущего активного препятствия
        ObstacleZoneVisualizer.Zone currentZone = GetZoneBySpeed(currentSpeed, activeData);
        
        // Вызываем соответствующее событие
        switch (currentZone)
        {
            case ObstacleZoneVisualizer.Zone.Yellow:
                // Игрок в жёлтой зоне - спотыкается
                playerStateManager.StartStumbling();
                Debug.Log($"Player stumbled! Speed: {currentSpeed:F2}, Zone: Yellow, Obstacle: {activeData.trigger.gameObject.name}, Priority: {activeData.priority}");
                break;
                
            case ObstacleZoneVisualizer.Zone.Red:
                // Игрок в красной зоне - падает
                playerStateManager.StartFalling();
                Debug.Log($"Player fell! Speed: {currentSpeed:F2}, Zone: Red, Obstacle: {activeData.trigger.gameObject.name}, Priority: {activeData.priority}");
                break;
                
            case ObstacleZoneVisualizer.Zone.Green:
                // Игрок в зелёной зоне - ничего не происходит
               // Debug.Log($"Player in safe zone. Speed: {currentSpeed:F2}, Zone: Green");
                break;
        }
    }
    
    /// <summary>
    /// Определяет зону по текущей скорости на основе данных препятствия.
    /// </summary>
    private ObstacleZoneVisualizer.Zone GetZoneBySpeed(float speed, ObstacleData data)
    {
        if (speed <= data.greenEnd)
            return ObstacleZoneVisualizer.Zone.Green;
        if (speed <= data.yellowEnd)
            return ObstacleZoneVisualizer.Zone.Yellow;
        return ObstacleZoneVisualizer.Zone.Red;
    }
}


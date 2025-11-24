using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Обработчик взаимодействия игрока с препятствиями.
/// Проверяет зоны при нажатии пробела и вызывает соответствующие события.
/// </summary>
[AddComponentMenu("Gameplay/Obstacle Interaction Handler")]
public class ObstacleInteractionHandler : MonoBehaviour
{
    // Статический доступ к экземпляру синглтона
    private static ObstacleInteractionHandler instance;
    public static ObstacleInteractionHandler Instance => instance;
    
    [Header("References")]
    [Tooltip("Менеджер состояния игрока")]
    public PlayerStateManager playerStateManager;
    
    [Header("Settings")]
    [Tooltip("Клавиша для взаимодействия с препятствием")]
    public KeyCode interactionKey = KeyCode.Space;
    
    // Структура для хранения данных о препятствии
    private struct ObstacleData
    {
        public ObstacleSpeedColorDiscrete obstacle;
        public float greenEnd;
        public float yellowEnd;
        public float redStart;
        public int priority;
        public ObstacleTrigger trigger;
    }
    
    // Словарь активных препятствий (ключ - ObstacleTrigger, значение - данные препятствия)
    private Dictionary<ObstacleTrigger, ObstacleData> activeObstacles = new Dictionary<ObstacleTrigger, ObstacleData>();
    
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
            Debug.LogWarning("Multiple ObstacleInteractionHandler instances detected. Only one should exist.");
            return;
        }
        
        // Если менеджер состояния не указан, пытаемся найти его на этом же объекте
        if (playerStateManager == null)
        {
            playerStateManager = GetComponent<PlayerStateManager>();
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
    
    void Update()
    {
        // Проверяем нажатие пробела только если игрок в нормальном состоянии и в зоне препятствия
        if (Input.GetKeyDown(interactionKey) && 
            isInObstacle && 
            playerStateManager != null && 
            playerStateManager.IsNormal)
        {
            CheckZoneAndTriggerEvent();
        }
    }
    
    /// <summary>
    /// Вызывается при входе игрока в зону препятствия.
    /// </summary>
    public void OnEnterObstacle(float greenEnd, float yellowEnd, float redStart, ObstacleSpeedColorDiscrete obstacle, int priority, ObstacleTrigger trigger)
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
    public void OnExitObstacle(ObstacleTrigger trigger)
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
            
            if (SprintRhythmController.Instance != null)
            {
                SprintRhythmController.Instance.ClearActiveObstacle();
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
            
            // Обновляем границы зон в SprintRhythmController для отображения на слайдерах
            if (SprintRhythmController.Instance != null)
            {
                SprintRhythmController.Instance.SetActiveObstacle(maxPriorityObstacle.obstacle);
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
        float currentSpeed = SprintRhythmController.NormalizedSpeed;
        
        // Определяем зону на основе текущего активного препятствия
        ObstacleSpeedColorDiscrete.Zone currentZone = GetZoneBySpeed(currentSpeed, activeData);
        
        // Вызываем соответствующее событие
        switch (currentZone)
        {
            case ObstacleSpeedColorDiscrete.Zone.Yellow:
                // Игрок в жёлтой зоне - спотыкается
                playerStateManager.StartStumbling();
                Debug.Log($"Player stumbled! Speed: {currentSpeed:F2}, Zone: Yellow, Obstacle: {activeData.trigger.gameObject.name}, Priority: {activeData.priority}");
                break;
                
            case ObstacleSpeedColorDiscrete.Zone.Red:
                // Игрок в красной зоне - падает
                playerStateManager.StartFalling();
                Debug.Log($"Player fell! Speed: {currentSpeed:F2}, Zone: Red, Obstacle: {activeData.trigger.gameObject.name}, Priority: {activeData.priority}");
                break;
                
            case ObstacleSpeedColorDiscrete.Zone.Green:
                // Игрок в зелёной зоне - ничего не происходит
               // Debug.Log($"Player in safe zone. Speed: {currentSpeed:F2}, Zone: Green");
                break;
        }
    }
    
    /// <summary>
    /// Определяет зону по текущей скорости на основе данных препятствия.
    /// </summary>
    private ObstacleSpeedColorDiscrete.Zone GetZoneBySpeed(float speed, ObstacleData data)
    {
        if (speed <= data.greenEnd)
            return ObstacleSpeedColorDiscrete.Zone.Green;
        if (speed <= data.yellowEnd)
            return ObstacleSpeedColorDiscrete.Zone.Yellow;
        return ObstacleSpeedColorDiscrete.Zone.Red;
    }
}


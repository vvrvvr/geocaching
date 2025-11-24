using UnityEngine;

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
    
    // Текущее активное препятствие
    private ObstacleSpeedColorDiscrete activeObstacle;
    
    // Границы зон текущего препятствия
    private float currentGreenEnd = 0f;
    private float currentYellowEnd = 0f;
    private float currentRedStart = 0f;
    
    // Флаг, что игрок находится в зоне препятствия
    private bool isInObstacle = false;
    
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
    public void OnEnterObstacle(float greenEnd, float yellowEnd, float redStart, ObstacleSpeedColorDiscrete obstacle)
    {
        activeObstacle = obstacle;
        currentGreenEnd = greenEnd;
        currentYellowEnd = yellowEnd;
        currentRedStart = redStart;
        isInObstacle = true;
        
        // Обновляем границы зон в SprintRhythmController для отображения на слайдерах
        if (SprintRhythmController.Instance != null)
        {
            SprintRhythmController.Instance.SetActiveObstacle(obstacle);
        }
    }
    
    /// <summary>
    /// Вызывается при выходе игрока из зоны препятствия.
    /// </summary>
    public void OnExitObstacle()
    {
        isInObstacle = false;
        activeObstacle = null;
        
        // Сбрасываем активное препятствие в SprintRhythmController
        if (SprintRhythmController.Instance != null)
        {
            SprintRhythmController.Instance.ClearActiveObstacle();
        }
    }
    
    /// <summary>
    /// Проверяет текущую зону скорости и вызывает соответствующее событие.
    /// </summary>
    private void CheckZoneAndTriggerEvent()
    {
        if (playerStateManager == null || !isInObstacle)
            return;
        
        // Получаем текущую нормализованную скорость
        float currentSpeed = SprintRhythmController.NormalizedSpeed;
        
        // Определяем зону
        ObstacleSpeedColorDiscrete.Zone currentZone = GetZoneBySpeed(currentSpeed);
        
        // Вызываем соответствующее событие
        switch (currentZone)
        {
            case ObstacleSpeedColorDiscrete.Zone.Yellow:
                // Игрок в жёлтой зоне - спотыкается
                playerStateManager.StartStumbling();
                Debug.Log($"Player stumbled! Speed: {currentSpeed:F2}, Zone: Yellow");
                break;
                
            case ObstacleSpeedColorDiscrete.Zone.Red:
                // Игрок в красной зоне - падает
                playerStateManager.StartFalling();
                Debug.Log($"Player fell! Speed: {currentSpeed:F2}, Zone: Red");
                break;
                
            case ObstacleSpeedColorDiscrete.Zone.Green:
                // Игрок в зелёной зоне - ничего не происходит
                Debug.Log($"Player in safe zone. Speed: {currentSpeed:F2}, Zone: Green");
                break;
        }
    }
    
    /// <summary>
    /// Определяет зону по текущей скорости.
    /// </summary>
    private ObstacleSpeedColorDiscrete.Zone GetZoneBySpeed(float speed)
    {
        if (speed <= currentGreenEnd)
            return ObstacleSpeedColorDiscrete.Zone.Green;
        if (speed <= currentYellowEnd)
            return ObstacleSpeedColorDiscrete.Zone.Yellow;
        return ObstacleSpeedColorDiscrete.Zone.Red;
    }
}


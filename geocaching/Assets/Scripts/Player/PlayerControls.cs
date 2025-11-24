using UnityEngine;

/// <summary>
/// Централизованная обработка управления игроком.
/// </summary>
[AddComponentMenu("Gameplay/Player Controls")]
public class PlayerControls : MonoBehaviour
{
    [Header("Sprint Input")]
    [Tooltip("Клавиша для увеличения скорости спринта и взаимодействия с препятствиями")]
    public KeyCode sprintKey = KeyCode.Space;
    
    void Update()
    {
        // Обработка нажатия клавиши спринта
        if (Input.GetKeyDown(sprintKey))
        {
            OnSprintKeyPressed();
        }
    }
    
    /// <summary>
    /// Вызывается при нажатии клавиши спринта.
    /// </summary>
    private void OnSprintKeyPressed()
    {
        // Регистрируем нажатие в SprintController для увеличения скорости
        if (SprintController.Instance != null)
        {
            SprintController.Instance.RegisterSprintTap();
        }
        
        // Проверяем взаимодействие с препятствиями
        if (ObstacleZoneHandler.Instance != null)
        {
            ObstacleZoneHandler.Instance.OnSprintInput();
        }
    }
}

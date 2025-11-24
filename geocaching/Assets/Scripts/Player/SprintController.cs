using UnityEngine;
using UnityEngine.UI;

public class SprintController : MonoBehaviour
{
    // Статический доступ к экземпляру контроллера
    private static SprintController instance;
    public static SprintController Instance => instance;
    
    // Статическое свойство для быстрого доступа к нормализованной скорости
    public static float NormalizedSpeed => instance != null ? instance.normalizedValue : 0f;
    
    [Header("Movement")]
    public float baseSpeed = 2f;
    public float maxExtraSpeed = 8f;
    public float speedSmooth = 8f;

    [Header("Frequency Settings")]
    [Tooltip("Минимальная частота, соответствующая значению 0.")]
    public float minTapsPerSecond = 0.5f;

    [Tooltip("Максимальная частота, соответствующая значению 1.")]
    public float maxTapsPerSecond = 5f;

    [Tooltip("Коэффициент EMA (0.1–0.4 обычно отлично).")]
    [Range(0.01f, 1f)]
    public float emaAlpha = 0.25f;

    [Header("Decay")]
    [Tooltip("Через сколько секунд после последнего нажатия начинается снижение.")]
    public float decayDelay = 0.4f;

    [Tooltip("Скорость снижения нормализованного значения.")]
    public float decayRate = 1f;

    [Header("UI")]
    public Slider speedSlider;
    [Tooltip("Слайдер для отображения границы зелёной зоны")]
    public Slider greenZoneSlider;
    [Tooltip("Слайдер для отображения границы жёлтой зоны")]
    public Slider yellowZoneSlider;
    [Tooltip("Слайдер для отображения границы красной зоны")]
    public Slider redZoneSlider;
    
    [Header("Zone Sliders Smoothing")]
    [Tooltip("Время плавного перехода значений слайдеров зон (сек)")]
    [Range(0.1f, 5f)]
    public float zoneSmoothTime = 1f;
    
    [Header("Obstacle Reference")]
    [Tooltip("Ссылка на скрипт препятствия для получения границ зон (для тестового вызова по клавише A)")]
    public ObstacleZoneVisualizer obstacleSpeedColor;

    private float emaInterval;
    private float lastTapTime = -1f;
    private float normalizedValue = 0f;
    private float smoothSpeed = 0f;
    private bool initialized = false;
    
    // Активное препятствие (устанавливается через ObstacleZoneHandler)
    private ObstacleZoneVisualizer activeObstacle = null;
    
    // Целевые значения для плавной интерполяции слайдеров зон
    private float targetGreenEnd = 0f;
    private float targetYellowEnd = 0f;
    private float targetRedStart = 0f;

    void Awake()
    {
        // Регистрация экземпляра для статического доступа
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.LogWarning("Multiple SprintController instances detected. Only one should exist.");
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
    
    void Start()
    {
        if (speedSlider != null)
        {
            speedSlider.minValue = 0;
            speedSlider.maxValue = 1;
            speedSlider.value = 0;
        }

        // Инициализация слайдеров зон
        InitializeZoneSliders();

        // Инициализация EMA интервала так, чтобы частота = minTapsPerSecond
        emaInterval = 1f / minTapsPerSecond;
        
        // Тестовый вызов для получения границ зон
       
    }
    
    void InitializeZoneSliders()
    {
        if (greenZoneSlider != null)
        {
            greenZoneSlider.minValue = 0;
            greenZoneSlider.maxValue = 1;
            greenZoneSlider.value = 0;
        }
        
        if (yellowZoneSlider != null)
        {
            yellowZoneSlider.minValue = 0;
            yellowZoneSlider.maxValue = 1;
            yellowZoneSlider.value = 0;
        }
        
        if (redZoneSlider != null)
        {
            redZoneSlider.minValue = 0;
            redZoneSlider.maxValue = 1;
            redZoneSlider.value = 0;
        }
    }
    
    /// <summary>
    /// Обновляет границы зон из указанного препятствия (для тестового вызова по клавише A).
    /// </summary>
    public void UpdateZoneBoundaries()
    {
        ObstacleZoneVisualizer obstacle = activeObstacle ?? obstacleSpeedColor;
        if (obstacle == null)
            return;
            
        obstacle.GetZoneBoundaries(out targetGreenEnd, out targetYellowEnd, out targetRedStart);
    }
    
    /// <summary>
    /// Устанавливает активное препятствие и обновляет границы зон.
    /// </summary>
    public void SetActiveObstacle(ObstacleZoneVisualizer obstacle)
    {
        activeObstacle = obstacle;
        if (obstacle != null)
        {
            obstacle.GetZoneBoundaries(out targetGreenEnd, out targetYellowEnd, out targetRedStart);
        }
    }
    
    /// <summary>
    /// Очищает активное препятствие и сбрасывает границы зон.
    /// </summary>
    public void ClearActiveObstacle()
    {
        activeObstacle = null;
        // Сбрасываем границы зон в ноль
        targetGreenEnd = 0f;
        targetYellowEnd = 0f;
        targetRedStart = 0f;
    }

    void Update()
    {
        
        // упадение значения при отсутствии нажатий
        if (initialized && Time.time - lastTapTime > decayDelay)
        {
            normalizedValue = Mathf.MoveTowards(
                normalizedValue,
                0f,
                decayRate * Time.deltaTime
            );
        }

        // сглаженная итоговая скорость
        float targetSpeed = baseSpeed + normalizedValue * maxExtraSpeed;
        smoothSpeed = Mathf.Lerp(
            smoothSpeed,
            targetSpeed,
            1f - Mathf.Exp(-speedSmooth * Time.deltaTime)
        );

        // UI
        if (speedSlider)
            speedSlider.value = normalizedValue;
        
        // Плавное обновление слайдеров зон
        UpdateZoneSlidersSmooth();

        // Пример ввода
        if (Input.GetKeyDown(KeyCode.Space))
            Tap();
    }

    public void Tap()
    {
        float t = Time.time;

        if (!initialized)
        {
            initialized = true;
            lastTapTime = t; // просто запоминаем, без расчёта
            return;
        }

        float delta = Mathf.Max(0.0001f, t - lastTapTime);
        lastTapTime = t;

        // EMA интервала
        emaInterval = emaInterval * (1f - emaAlpha) + delta * emaAlpha;

        float freq = 1f / emaInterval;

        // нормализация в диапазон [0..1]
        normalizedValue = Mathf.InverseLerp(minTapsPerSecond, maxTapsPerSecond, freq);
    }

    void UpdateZoneSlidersSmooth()
    {
        if (zoneSmoothTime <= 0f)
        {
            // Если время плавности равно нулю, устанавливаем значения сразу
            if (greenZoneSlider != null)
                greenZoneSlider.value = targetGreenEnd;
            if (yellowZoneSlider != null)
                yellowZoneSlider.value = targetYellowEnd;
            if (redZoneSlider != null)
                redZoneSlider.value = targetRedStart;
            return;
        }
        
        // Плавная интерполяция с использованием экспоненциального сглаживания
        float smoothFactor = 1f - Mathf.Exp(-Time.deltaTime / zoneSmoothTime);
        
        if (greenZoneSlider != null)
        {
            greenZoneSlider.value = Mathf.Lerp(greenZoneSlider.value, targetGreenEnd, smoothFactor);
        }
        
        if (yellowZoneSlider != null)
        {
            yellowZoneSlider.value = Mathf.Lerp(yellowZoneSlider.value, targetYellowEnd, smoothFactor);
        }
        
        if (redZoneSlider != null)
        {
            redZoneSlider.value = Mathf.Lerp(redZoneSlider.value, targetRedStart, smoothFactor);
        }
    }

    public float GetCurrentSpeed() => smoothSpeed;

    public float GetNormalized() => normalizedValue;
}

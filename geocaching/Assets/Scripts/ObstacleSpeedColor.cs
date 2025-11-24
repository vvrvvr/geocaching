using System.Collections;
using UnityEngine;

[AddComponentMenu("Gameplay/Obstacle Speed Color (Discrete + Smooth Zone Enter)")]
public class ObstacleSpeedColorDiscrete : MonoBehaviour
{
    public enum Zone { Green = 0, Yellow = 1, Red = 2 }

    [Header("References")]
    [Tooltip("Рендерер, материал которого будет менять цвет.")]
    public Renderer targetRenderer;
    
    [Header("Speed Source")]
    [Tooltip("Если включено, будет использовать статический доступ к SprintRhythmController (рекомендуется). Если выключено, можно указать конкретный источник вручную.")]
    public bool useStaticAccess = true;
    [Tooltip("Источник нормализованной скорости 0..1 (используется только если useStaticAccess = false)")]
    public SprintRhythmController playerSpeedSource;

    [Header("Zones (0..1)")]
    [Range(0f, 1f)] public float greenEnd  = 0.33f;
    [Range(0f, 1f)] public float yellowEnd = 0.66f;

    [Header("Zone Colors")]
    public Color greenColor  = Color.green;
    public Color yellowColor = Color.yellow;
    public Color redColor    = Color.red;

    [Header("Transition")]
    [Tooltip("Время перехода цвета в новую зону (сек).")]
    public float transitionDuration = 0.3f;
    [Tooltip("Прерывать ли переход, если игрок попал в другую зону.")]
    public bool interruptTransitions = true;

    // Internal state
    private Zone currentZone = Zone.Green;
    private Coroutine runningTransition = null;
    private Material runtimeMaterial;

    void Start()
    {
        if (targetRenderer != null)
            runtimeMaterial = targetRenderer.material;

        ClampZoneBounds();

        currentZone = Zone.Green;
        ApplyImmediateColor(greenColor);
    }

    void OnValidate()
    {
        if (yellowEnd < greenEnd) yellowEnd = greenEnd;
        ClampZoneBounds();

        if (targetRenderer != null && Application.isPlaying)
            runtimeMaterial = targetRenderer.material;
    }

    void Update()
    {
        if (runtimeMaterial == null)
            return;

        // Получаем скорость через статический доступ или через ссылку
        float speed = 0f;
        if (useStaticAccess)
        {
            speed = Mathf.Clamp01(SprintRhythmController.NormalizedSpeed);
        }
        else
        {
            if (playerSpeedSource == null)
                return;
            speed = Mathf.Clamp01(playerSpeedSource.GetNormalized());
        }

        Zone newZone = ZoneByNormalized(speed);

        if (newZone == currentZone)
            return;

        // Зона изменилась → запускаем переход
        if (transitionDuration <= 0f)
        {
            ApplyColorForZone(newZone);
            currentZone = newZone;
            return;
        }

        if (runningTransition != null)
        {
            if (interruptTransitions)
            {
                StopCoroutine(runningTransition);
                runningTransition = StartCoroutine(TransitionToZone(newZone));
            }
            // Если прерывать нельзя — игнорируем
        }
        else
        {
            runningTransition = StartCoroutine(TransitionToZone(newZone));
        }

        currentZone = newZone;
    }

    Zone ZoneByNormalized(float n)
    {
        if (n <= greenEnd) return Zone.Green;
        if (n <= yellowEnd) return Zone.Yellow;
        return Zone.Red;
    }

    void ApplyImmediateColor(Color c)
    {
        if (runtimeMaterial != null)
            runtimeMaterial.color = c;
    }

    void ApplyColorForZone(Zone z)
    {
        switch (z)
        {
            case Zone.Green:  ApplyImmediateColor(greenColor);  break;
            case Zone.Yellow: ApplyImmediateColor(yellowColor); break;
            case Zone.Red:    ApplyImmediateColor(redColor);    break;
        }
    }

    IEnumerator TransitionToZone(Zone targetZone)
    {
        Color from = runtimeMaterial.color;
        Color to = targetZone switch
        {
            Zone.Green  => greenColor,
            Zone.Yellow => yellowColor,
            Zone.Red    => redColor,
            _           => greenColor
        };

        float t = 0f;

        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / transitionDuration);
            float eased = k * k * (3f - 2f * k); // плавность
            runtimeMaterial.color = Color.Lerp(from, to, eased);
            yield return null;
        }

        runtimeMaterial.color = to;
        runningTransition = null;
    }

    void ClampZoneBounds()
    {
        greenEnd = Mathf.Clamp01(greenEnd);
        yellowEnd = Mathf.Clamp01(yellowEnd);
        if (yellowEnd < greenEnd) yellowEnd = greenEnd;
    }
    
    /// <summary>
    /// Возвращает границы зон для визуализации на слайдерах.
    /// </summary>
    /// <param name="greenEnd">Конец зелёной зоны (начало жёлтой)</param>
    /// <param name="yellowEnd">Конец жёлтой зоны (начало красной)</param>
    /// <param name="redStart">Начало красной зоны (равно yellowEnd)</param>
    public void GetZoneBoundaries(out float greenEnd, out float yellowEnd, out float redStart)
    {
        greenEnd = this.greenEnd;
        yellowEnd = this.yellowEnd;
        redStart = 1; // Красная зона начинается там, где заканчивается жёлтая
    }
}

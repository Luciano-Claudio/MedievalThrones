using UnityEngine;

/// <summary>
/// Atualiza cor/intensidade da luz direcional com base em TimeOfDay.
/// Ligue este script na sua Directional Light.
/// </summary>
[RequireComponent(typeof(Light))]
public class DayNightLightController : MonoBehaviour
{
    public Gradient colorOverDay = new Gradient
    {
        colorKeys = new[] {
            new GradientColorKey(new Color(0.85f, 0.75f, 0.55f), 0.00f), // amanhecer
            new GradientColorKey(new Color(1.00f, 0.95f, 0.85f), 0.25f), // dia
            new GradientColorKey(new Color(1.00f, 0.85f, 0.60f), 0.50f), // pôr-do-sol
            new GradientColorKey(new Color(0.20f, 0.25f, 0.40f), 0.75f), // crepúsculo
            new GradientColorKey(new Color(0.10f, 0.12f, 0.20f), 1.00f), // noite
        }
    };

    public AnimationCurve intensityOverDay = AnimationCurve.EaseInOut(0, 0.15f, 0.25f, 1f);
    private Light _light;

    void OnEnable()
    {
        _light = GetComponent<Light>();
        GameEvents.OnTimeOfDay01 += Apply;
    }
    void OnDisable() => GameEvents.OnTimeOfDay01 -= Apply;

    private void Apply(float t01)
    {
        _light.color = colorOverDay.Evaluate(t01);
        _light.intensity = Mathf.Clamp01(intensityOverDay.Evaluate(t01));
        // Rotação simples do sol (opcional)
        transform.rotation = Quaternion.Euler(new Vector3((t01 * 360f) - 90f, 170f, 0f));
    }
}

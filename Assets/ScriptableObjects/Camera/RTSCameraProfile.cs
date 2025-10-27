using UnityEngine;

/// <summary>
/// ScriptableObject que centraliza todas as configurações de câmera RTS.
/// Permite criar diferentes perfis (Default, Cinematic, Spectator) reutilizáveis.
/// Sugestão de Refatoração #1 da documentação.
/// </summary>
[CreateAssetMenu(fileName = "CameraProfile", menuName = "Game/Camera Profile")]
public class RTSCameraProfile : ScriptableObject
{
    [Header("Pan (WASD / Borda / Drag)")]
    [Tooltip("Velocidade de pan quando a câmera está perto (zoom mínimo)")]
    public float panSpeedNear = 20f;

    [Tooltip("Velocidade de pan quando a câmera está longe (zoom máximo)")]
    public float panSpeedFar = 35f;

    [Tooltip("Espessura da borda em pixels para edge-pan")]
    public int edgeThickness = 12;

    [Tooltip("Sensibilidade do arrasto com botão do meio")]
    public float middleDragSensitivity = 1f;

    [Header("Zoom (Altura + Tilt)")]
    [Tooltip("Velocidade de zoom (scroll)")]
    public float zoomSpeed = 0.15f;

    [Tooltip("Altura mínima da câmera (zoom próximo)")]
    public float minHeight = 10f;

    [Tooltip("Altura máxima da câmera (zoom distante)")]
    public float maxHeight = 60f;

    [Tooltip("Ângulo de tilt mínimo em graus (visão mais plana)")]
    public float minTilt = 35f;

    [Tooltip("Ângulo de tilt máximo em graus (visão mais inclinada)")]
    public float maxTilt = 75f;

    [Header("Rotation")]
    [Tooltip("Velocidade de rotação em graus por segundo")]
    public float rotateSpeed = 90f;

    [Header("Opções de Curvas (Avançado)")]
    [Tooltip("Curva de suavização para movimento de pan por zoom. X=zoom(0-1), Y=multiplicador")]
    public AnimationCurve panSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);

    [Tooltip("Curva de suavização para zoom. X=entrada, Y=saída suavizada")]
    public AnimationCurve zoomCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Flags de Interação")]
    public bool enableWASD = true;
    public bool enableEdgePan = true;
    public bool enableMiddleDrag = true;
    public bool enableRotate = true;
    public bool pauseWhenPointerOverUI = true;

    /// <summary>
    /// Calcula a velocidade de pan interpolada baseada no zoom atual (0..1)
    /// Aplica a curva customizada se configurada
    /// </summary>
    public float GetPanSpeed(float zoom01)
    {
        float baseSpeed = Mathf.Lerp(panSpeedNear, panSpeedFar, zoom01);
        float curveMultiplier = panSpeedCurve.Evaluate(zoom01);
        return baseSpeed * curveMultiplier;
    }

    /// <summary>
    /// Aplica suavização no valor de zoom usando a curva configurada
    /// </summary>
    public float ApplyZoomCurve(float rawZoom01)
    {
        return zoomCurve.Evaluate(Mathf.Clamp01(rawZoom01));
    }
}
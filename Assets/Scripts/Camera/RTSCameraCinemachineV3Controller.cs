using System.Collections;
using UnityEngine;
using Unity.Cinemachine; // CM3
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Controlador de câmera RTS usando Cinemachine v3.
/// Refatorado para usar RTSCameraProfile, GameEvents e CameraMath.
/// Implementa sugestões de refatoração #1, #2, #5, #8 da documentação.
/// </summary>
[DisallowMultipleComponent]
public class RTSCameraCinemachineV3Controller : MonoBehaviour
{
    [Header("Profile & References")]
    [Tooltip("Perfil de configuração da câmera (ScriptableObject)")]
    public RTSCameraProfile profile;

    [Tooltip("Câmera virtual do Cinemachine (criada automaticamente se null)")]
    public CinemachineCamera vcam;

    [Tooltip("Pivot usado para tilt (criado automaticamente se null)")]
    public Transform pivot;

    [Header("Bounds (Mundo)")]
    public Vector2 boundsCenter = Vector2.zero; // (x,z)
    public Vector2 boundsSize = new Vector2(200, 200);

    [Header("Initial Setup")]
    public Vector3 initialPosition = Vector3.zero;
    public float initialHeading = 0f;
    [Range(0, 1)] public float initialZoom = 0.5f;

    [Header("Runtime State (Read-Only)")]
    [SerializeField, Range(0, 1)]
    private float _zoom = 0.5f;  // 0=perto 1=longe

    public float Zoom => _zoom;

    // Componentes CM3
    private CinemachineFollow _follow;
    private CinemachineBasicMultiChannelPerlin _noise;
    private CinemachineCamera _cutscene;

    // Cache zoom/tilt
    private float _cachedHeight;
    private float _cachedTilt;
    private Coroutine _goToCo;

    void Reset()
    {
        EnsureSetup();
    }

    void Awake()
    {
        EnsureSetup();

        // Validar profile
        if (profile == null)
        {
            Debug.LogError($"[RTSCamera] Profile não atribuído em {gameObject.name}! " +
                          "Crie um RTSCameraProfile via Assets > Create > Game > Camera Profile", this);
        }

        // Aplicar setup inicial
        transform.position = initialPosition;
        transform.rotation = Quaternion.Euler(0f, initialHeading, 0f);
        _zoom = Mathf.Clamp01(initialZoom);
        ApplyZoomAndTilt();
        UpdateFollowOffset();
    }

    void OnEnable()
    {
        // Sugestão #2 e #5: escutar eventos de jogo
        GameEvents.OnCameraShake += HandleCameraShake;
        GameEvents.OnCameraFocus += HandleCameraFocus;
        GameEvents.OnCameraFocusXZ += HandleCameraFocusXZ;
        GameEvents.OnCutsceneStart += HandleCutsceneStart;
        GameEvents.OnCutsceneEnd += HandleCutsceneEnd;
        GameEvents.OnMinimapPing += HandleMinimapPing;
        GameEvents.OnSelectionFocus += HandleSelectionFocus;
    }

    void OnDisable()
    {
        GameEvents.OnCameraShake -= HandleCameraShake;
        GameEvents.OnCameraFocus -= HandleCameraFocus;
        GameEvents.OnCameraFocusXZ -= HandleCameraFocusXZ;
        GameEvents.OnCutsceneStart -= HandleCutsceneStart;
        GameEvents.OnCutsceneEnd -= HandleCutsceneEnd;
        GameEvents.OnMinimapPing -= HandleMinimapPing;
        GameEvents.OnSelectionFocus -= HandleSelectionFocus;
    }

    void EnsureSetup()
    {
        if (!pivot)
        {
            pivot = new GameObject("Pivot").transform;
            pivot.SetParent(transform, false);
        }

        if (!vcam)
        {
            var go = new GameObject("RTS_Camera (CM3)");
            vcam = go.AddComponent<CinemachineCamera>();
        }

        vcam.Follow = pivot;
        vcam.LookAt = pivot;

        // Body
        _follow = vcam.GetComponent<CinemachineFollow>();
        if (_follow == null)
            _follow = vcam.gameObject.AddComponent<CinemachineFollow>();

        // Aim
        var aim = vcam.GetComponent<CinemachineRotationComposer>();
        if (aim == null)
            aim = vcam.gameObject.AddComponent<CinemachineRotationComposer>();

        // Noise (shake)
        _noise = vcam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        if (_noise == null)
            _noise = vcam.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();

        _noise.AmplitudeGain = 0;
        _noise.FrequencyGain = 0;
    }

    // ==================== API PÚBLICA (Input) ====================

    /// <summary>
    /// Processa entrada por frame. Chamado pelo RTSCameraInputSystem.
    /// </summary>
    public void TickInput(
        Vector2 wasdMove,
        Vector2 pointerPosition,
        bool isMiddleDragging,
        Vector2 pointerDelta,
        float rotateAxis,
        float zoomAxis,
        bool pointerOverUI)
    {
        if (profile == null) return;

        bool pauseInput = profile.pauseWhenPointerOverUI && pointerOverUI;

        HandleZoom(zoomAxis, pauseInput);
        HandleRotate(rotateAxis, pauseInput);
        HandlePan(wasdMove, pointerPosition, isMiddleDragging, pointerDelta, pauseInput);
        ClampToBounds();
        UpdateFollowOffset();
    }

    // ==================== HANDLERS DE INPUT ====================

    void HandlePan(Vector2 moveInput, Vector2 mousePos, bool dragging, Vector2 dragDelta, bool pauseInput)
    {
        if (profile == null) return;

        Vector3 move = Vector3.zero;

        // WASD
        if (profile.enableWASD)
        {
            move += transform.forward * moveInput.y + transform.right * moveInput.x;
            move.y = 0;
        }

        // Edge Pan
        if (profile.enableEdgePan && !pauseInput)
        {
            Vector2 edgeDir = CameraMath.GetEdgePanDirection(
                mousePos,
                profile.edgeThickness,
                Screen.width,
                Screen.height
            );

            move += transform.right * edgeDir.x + transform.forward * edgeDir.y;
            move.y = 0;
        }

        // Middle Drag
        if (profile.enableMiddleDrag && dragging)
        {
            move += (-transform.right * dragDelta.x - transform.forward * dragDelta.y)
                    * (0.01f * profile.middleDragSensitivity);
        }

        // Aplicar movimento com velocidade do profile
        if (move.sqrMagnitude > 0.0001f)
        {
            float speed = profile.GetPanSpeed(_zoom);
            transform.position += move.normalized * speed * Time.deltaTime;
        }
    }

    void HandleRotate(float axis, bool pauseInput)
    {
        if (profile == null || !profile.enableRotate) return;
        if (Mathf.Abs(axis) < 0.0001f) return;
        if (pauseInput) return;

        transform.Rotate(Vector3.up, axis * profile.rotateSpeed * Time.deltaTime, Space.World);
    }

    void HandleZoom(float axis, bool pauseInput)
    {
        if (profile == null) return;
        if (Mathf.Abs(axis) < 0.0001f) return;
        if (pauseInput) return;

        _zoom = Mathf.Clamp01(_zoom - axis * profile.zoomSpeed);
        _zoom = profile.ApplyZoomCurve(_zoom); // Sugestão #7: aplicar curva
        ApplyZoomAndTilt();
    }

    void ApplyZoomAndTilt()
    {
        if (profile == null) return;

        _cachedHeight = Mathf.Lerp(profile.minHeight, profile.maxHeight, _zoom);
        _cachedTilt = Mathf.Lerp(profile.minTilt, profile.maxTilt, _zoom);
    }

    void UpdateFollowOffset()
    {
        if (profile == null || _follow == null) return;

        // Sugestão #8: usar CameraMath para lógica pura testável
        _follow.FollowOffset = CameraMath.TiltToOffset(
            _cachedHeight,
            _cachedTilt,
            transform.forward
        );
    }

    void ClampToBounds()
    {
        // Sugestão #8: usar CameraMath
        transform.position = CameraMath.ClampToBounds(
            transform.position,
            boundsCenter,
            boundsSize
        );
    }

    // ==================== EVENT HANDLERS ====================

    void HandleCameraShake(float amplitude, float frequency, float duration)
    {
        StartCoroutine(CoShake(amplitude, frequency, duration));
    }

    void HandleCameraFocus(Vector3 worldPos, bool snap, float duration)
    {
        GoTo(worldPos, snap, duration);
    }

    void HandleCameraFocusXZ(Vector2 worldXZ, bool snap, float duration)
    {
        GoToXZ(worldXZ, snap, duration);
    }

    void HandleCutsceneStart(Transform target, float fov, int priority)
    {
        StartCutscene(target, fov, priority);
    }

    void HandleCutsceneEnd()
    {
        EndCutscene();
    }

    void HandleMinimapPing(Vector2 worldXZ)
    {
        // Foco suave no ping
        GoToXZ(worldXZ, snap: false, duration: 0.5f);
    }

    void HandleSelectionFocus(Transform target)
    {
        if (target != null)
        {
            GoTo(target.position, snap: false, duration: 0.3f);
        }
    }

    // ==================== API PÚBLICA (Shake, Cutscene, GoTo) ====================

    public void PlayShake(float amplitude = 1.2f, float frequency = 2.0f, float duration = 0.25f)
    {
        StartCoroutine(CoShake(amplitude, frequency, duration));
    }

    IEnumerator CoShake(float amp, float freq, float dur)
    {
        if (_noise == null) yield break;

        _noise.AmplitudeGain = amp;
        _noise.FrequencyGain = freq;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            yield return null;
        }

        _noise.AmplitudeGain = 0;
        _noise.FrequencyGain = 0;
    }

    public void StartCutscene(Transform target, float fov = 50f, int priority = 100)
    {
        if (_cutscene == null)
        {
            var go = new GameObject("Cutscene (CM3)");
            _cutscene = go.AddComponent<CinemachineCamera>();
            _cutscene.gameObject.AddComponent<CinemachineFollow>();
            _cutscene.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
        }

        _cutscene.Follow = target;
        _cutscene.LookAt = target;
        _cutscene.Lens.FieldOfView = fov;
        _cutscene.Priority = priority;
    }

    public void EndCutscene()
    {
        if (_cutscene)
            _cutscene.Priority = 0;
    }

    public void GoToXZ(Vector2 worldXZ, bool snap = false, float duration = 0.4f)
    {
        Vector3 target = CameraMath.XZToVector3(worldXZ, transform.position.y);
        GoTo(target, snap, duration);
    }

    public void GoTo(Vector3 worldPos, bool snap = false, float duration = 0.4f)
    {
        // Sugestão #8: usar CameraMath para clamp
        Vector3 clampedPos = CameraMath.ClampToBounds(
            new Vector3(worldPos.x, transform.position.y, worldPos.z),
            boundsCenter,
            boundsSize
        );

        if (snap || duration <= 0f)
        {
            transform.position = clampedPos;
            UpdateFollowOffset();
            return;
        }

        if (_goToCo != null)
            StopCoroutine(_goToCo);

        _goToCo = StartCoroutine(CoGoTo(clampedPos, duration));
    }

    IEnumerator CoGoTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            // Sugestão #8: usar CameraMath para easing
            transform.position = CameraMath.SmoothLerp(start, target, t);
            UpdateFollowOffset();

            yield return null;
        }

        transform.position = target;
        UpdateFollowOffset();
        _goToCo = null;
    }

    // ==================== UTILITÁRIOS ====================

    [ContextMenu("Set Bounds From Terrain(s)")]
    public void SetBoundsFromTerrains()
    {
        // Sugestão #6 e #8: usar CameraMath
        if (CameraMath.CalculateTerrainBounds(
            Terrain.activeTerrains,
            out Vector2 center,
            out Vector2 size))
        {
            boundsCenter = center;
            boundsSize = size;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif

            Debug.Log($"[RTSCamera] Bounds atualizados: Center={center}, Size={size}");
        }
        else
        {
            Debug.LogWarning("[RTSCamera] Nenhum terreno ativo encontrado!");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.25f);
        Vector3 center3D = new Vector3(boundsCenter.x, 0f, boundsCenter.y);
        Vector3 size3D = new Vector3(boundsSize.x, 0f, boundsSize.y);

        Gizmos.DrawCube(center3D, size3D);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center3D, size3D);
    }
}
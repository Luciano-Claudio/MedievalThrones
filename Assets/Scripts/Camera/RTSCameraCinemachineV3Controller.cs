using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Cinemachine; // CM3
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class RTSCameraCinemachineV3Controller : MonoBehaviour
{
    [Header("References")]
    public CinemachineCamera vcam;   // se null, cria uma
    public Transform pivot;          // criado/ usado para tilt

    [Header("Pan (WASD / Borda / Drag)")]
    public bool enableWASD = true;
    public bool enableEdgePan = true;
    public bool enableMiddleDrag = true;
    public float panSpeedNear = 20f;
    public float panSpeedFar = 35f;
    public int edgeThickness = 12;         // px
    public float middleDragSensitivity = 1; // escala do drag

    [Header("Zoom (altura + tilt)")]
    [Range(0, 1)] public float zoom = 0.5f;  // 0=perto 1=longe
    public float zoomSpeed = 0.15f;
    public float minHeight = 10f;
    public float maxHeight = 60f;
    public float minTilt = 35f;
    public float maxTilt = 75f;

    [Header("Rotation")]
    public bool enableRotate = true;
    public float rotateSpeed = 90f; // graus/seg

    [Header("Bounds (mundo)")]
    public Vector2 boundsCenter = Vector2.zero; // (x,z)
    public Vector2 boundsSize = new Vector2(200, 200);

    [Header("UI / Misc")]
    public bool pauseWhenPointerOverUI = true;

    [Header("Initial Setup")]
    public Vector3 initialPosition = Vector3.zero;
    public float initialHeading = 0f;
    [Range(0, 1)] public float initialZoom = 0.5f;

    // CM3 components
    CinemachineFollow _follow;
    CinemachineBasicMultiChannelPerlin _noise;
    CinemachineCamera _cutscene;

    // cache zoom/tilt
    float _cachedHeight;
    float _cachedTilt;

    void Reset() => EnsureSetup();

    void Awake()
    {
        EnsureSetup();    
        // aplicar inicial
        transform.position = initialPosition;
        transform.rotation = Quaternion.Euler(0f, initialHeading, 0f);
        zoom = Mathf.Clamp01(initialZoom);
        ApplyZoomAndTilt();
        UpdateFollowOffset();
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
        vcam.LookAt = pivot; // << mirar no Pivot
        
        // Body
        _follow = vcam.GetComponent<CinemachineFollow>();
        if (_follow == null) _follow = vcam.gameObject.AddComponent<CinemachineFollow>();

        // Aim (ADICIONE ISSO)
        var _aim = vcam.GetComponent<CinemachineRotationComposer>();
        if (_aim == null) _aim = vcam.gameObject.AddComponent<CinemachineRotationComposer>();

        // Noise (shake)
        _noise = vcam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        if (_noise == null) _noise = vcam.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
        _noise.AmplitudeGain = 0;
        _noise.FrequencyGain = 0;
    }

    // ---------- API pública (chamada pelo script de Input) ----------
    public void TickInput(
        Vector2 wasdMove,           // -1..1 em XZ relativo ao rig (WASD)
        Vector2 pointerPosition,    // em pixels (para edge-pan)
        bool isMiddleDragging,      // botão do meio segurado?
        Vector2 pointerDelta,       // delta do mouse (para drag)
        float rotateAxis,           // -1..1 (Q/E)
        float zoomAxis,             // delta do scroll (positivo/negativo)
        bool pointerOverUI          // veio do módulo de UI do InputSystem ou EventSystem
    )
    {
        bool overUI = pauseWhenPointerOverUI && pointerOverUI;

        HandleZoom(zoomAxis, overUI);
        HandleRotate(rotateAxis, overUI);
        HandlePan(wasdMove, pointerPosition, isMiddleDragging, pointerDelta, overUI);
        ClampToBounds();
        UpdateFollowOffset();
    }

    // ---------- Implementação ----------
    void HandlePan(Vector2 moveInput, Vector2 mousePos, bool dragging, Vector2 dragDelta, bool overUI)
    {
        Vector3 move = Vector3.zero;

        if (enableWASD)
        {
            move += transform.forward * moveInput.y + transform.right * moveInput.x;
            move.y = 0;
        }

        if (enableEdgePan && !overUI)
        {
            if (mousePos.x <= edgeThickness) move += -transform.right;
            else if (mousePos.x >= Screen.width - edgeThickness) move += transform.right;
            if (mousePos.y <= edgeThickness) move += -transform.forward;
            else if (mousePos.y >= Screen.height - edgeThickness) move += transform.forward;
            move.y = 0;
        }

        if (enableMiddleDrag && dragging)
        {
            // arrasto no espaço de tela vira movimento no plano
            move += (-transform.right * dragDelta.x - transform.forward * dragDelta.y) * (0.01f * middleDragSensitivity);
        }

        float spd = Mathf.Lerp(panSpeedNear, panSpeedFar, zoom);
        if (move.sqrMagnitude > 0.0001f)
            transform.position += move.normalized * spd * Time.deltaTime;
    }
    void HandleRotate(float axis, bool overUI)
    {
        if (!enableRotate || Mathf.Abs(axis) < 0.0001f) return;
        if (pauseWhenPointerOverUI && overUI) return;   // << bloqueia rotação sobre UI (opcional)
        transform.Rotate(Vector3.up, axis * rotateSpeed * Time.deltaTime, Space.World);
    }

    void HandleZoom(float axis, bool overUI)
    {
        if (Mathf.Abs(axis) < 0.0001f) return;
        if (pauseWhenPointerOverUI && overUI) return;   // << bloqueia ZOOM sobre UI
        zoom = Mathf.Clamp01(zoom - axis * zoomSpeed); // eixo positivo aproxima/afasta conforme preferência
        ApplyZoomAndTilt();
    }

    void ApplyZoomAndTilt()
    {
        _cachedHeight = Mathf.Lerp(minHeight, maxHeight, zoom);
        _cachedTilt = Mathf.Lerp(minTilt, maxTilt, zoom);
        //pivot.localRotation = Quaternion.Euler(_cachedTilt, 0f, 0f);
    }

    void UpdateFollowOffset()
    {
        float h = _cachedHeight;
        float tiltRad = _cachedTilt * Mathf.Deg2Rad;
        float d = (Mathf.Tan(tiltRad) > 0.0001f) ? h / Mathf.Tan(tiltRad) : h * 2f;

        Vector3 back = -transform.forward;
        Vector3 worldOffset = back * d + Vector3.up * h;

        _follow.FollowOffset = worldOffset; // CM3: offset em world
    }

    void ClampToBounds()
    {
        var half = boundsSize * 0.5f;
        float minX = boundsCenter.x - half.x;
        float maxX = boundsCenter.x + half.x;
        float minZ = boundsCenter.y - half.y;
        float maxZ = boundsCenter.y + half.y;

        var p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.z = Mathf.Clamp(p.z, minZ, maxZ);
        transform.position = p;
    }

    // ---------- Ganchos prontos ----------
    public void PlayShake(float amplitude = 1.2f, float frequency = 2.0f, float duration = 0.25f)
        => StartCoroutine(CoShake(amplitude, frequency, duration));

    IEnumerator CoShake(float amp, float freq, float dur)
    {
        if (_noise == null) yield break;
        _noise.AmplitudeGain = amp;
        _noise.FrequencyGain = freq;
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; yield return null; }
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

    // --- API pública para mini-mapa / jump-to ---

    public void GoToXZ(Vector2 worldXZ, bool snap = false, float duration = 0.4f)
    {
        var target = new Vector3(worldXZ.x, transform.position.y, worldXZ.y);
        GoTo(target, snap, duration);
    }

    public void GoTo(Vector3 worldPos, bool snap = false, float duration = 0.4f)
    {
        // clamp no retângulo XZ
        var half = boundsSize * 0.5f;
        float minX = boundsCenter.x - half.x;
        float maxX = boundsCenter.x + half.x;
        float minZ = boundsCenter.y - half.y;
        float maxZ = boundsCenter.y + half.y;

        var p = new Vector3(
            Mathf.Clamp(worldPos.x, minX, maxX),
            transform.position.y, // não mexe na altura do rig
            Mathf.Clamp(worldPos.z, minZ, maxZ)
        );

        if (snap || duration <= 0f)
        {
            transform.position = p;
            UpdateFollowOffset(); // mantém a câmera correta para o tilt/zoom atuais
            return;
        }

        // movimento suave
        if (_goToCo != null) StopCoroutine(_goToCo);
        _goToCo = StartCoroutine(CoGoTo(p, duration));
    }

    Coroutine _goToCo;

    System.Collections.IEnumerator CoGoTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            // ease in-out (cúbico suave)
            float s = t * t * (3f - 2f * t);
            transform.position = Vector3.LerpUnclamped(start, target, s);
            UpdateFollowOffset();
            yield return null;
        }
        transform.position = target;
        UpdateFollowOffset();
        _goToCo = null;
    }


    public void EndCutscene() { if (_cutscene) _cutscene.Priority = 0; }

    [ContextMenu("Set Bounds From Terrain(s)")]
    public void SetBoundsFromTerrains()
    {
        var terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length == 0) return;

        Bounds b;
        if (terrains.Length == 1)
        {
            var t = terrains[0];
            var pos = t.transform.position;
            var size = t.terrainData.size;
            b = new Bounds(pos + new Vector3(size.x, 0, size.z) * 0.5f,
                           new Vector3(size.x, 0, size.z));
        }
        else
        {
            b = new Bounds();
            for (int i = 0; i < terrains.Length; i++)
            {
                var t = terrains[i];
                var sz = t.terrainData.size;
                var p = t.transform.position;
                var bb = new Bounds(p + new Vector3(sz.x, 0, sz.z) * 0.5f,
                                    new Vector3(sz.x, 0, sz.z));
                if (i == 0) b = bb; else b.Encapsulate(bb);
            }
        }

        boundsCenter = new Vector2(b.center.x, b.center.z);
        boundsSize = new Vector2(b.size.x, b.size.z);

    #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
    #endif
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.25f);
        var c = new Vector3(boundsCenter.x, 0f, boundsCenter.y);
        var s = new Vector3(boundsSize.x, 0f, boundsSize.y);
        Gizmos.DrawCube(c, s);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(c, s);
    }
}

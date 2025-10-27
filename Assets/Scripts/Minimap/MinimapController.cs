using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class MinimapController : MonoBehaviour, IPointerClickHandler, IScrollHandler
{
    [Header("Refs")]
    public RawImage minimapImage;                 // o RawImage que mostra a RT
    public RectTransform iconsRoot;               // overlay para ícones
    public RectTransform iconPrefab;              // prefab quadradinho
    public Camera minimapCamera;                  // a MinimapCamera ortográfica
    public RTSCameraCinemachineV3Controller rtsCamera; // nossa câmera de jogo
    public FactionDatabase factionDb;             // para cores de facção

    [Header("Mapa (mesma referência dos bounds da câmera)")]
    public Vector2 boundsCenter = Vector2.zero;   // (x,z)
    public Vector2 boundsSize = new Vector2(200, 200);

    [Header("Aparência")]
    public Vector2 iconSize = new Vector2(8, 8);
    public bool clampIconsInside = true;

    [Header("Follow Main View")]
    public bool followMainView = true;
    public Camera mainCamera;                 // a câmera que renderiza o jogo (Main Camera com Brain)

    [Tooltip("Camadas consideradas como chão para raycast. Se não acertar, usa plano Y=groundY.")]
    public LayerMask groundMask = ~0;
    public float groundY = 0f;                // altura do plano de fallback

    [Tooltip("Quanto maior que a visão atual o minimapa deve enquadrar (ex.: 1.2 = 20% a mais).")]
    public float viewPadding = 1.2f;

    [Header("Culling de ícones")]
    public bool hideIconsOutside = true;   // se true: esconde fora; se false: ainda permite clamp
    [Range(0f, 0.1f)] public float uvBorderTolerance = 0.0f; // margem extra antes de considerar "fora"
    public bool fadeNearBorder = false;    // opcional: esmaecer ao se aproximar da borda
    public float fadeWidthUV = 0.03f;      // largura de transição do fade (em UV)

    [Header("Minimap Zoom (independente da câmera principal)")]
    [Tooltip("Tamanho base em world-units para a ALTURA do minimapa (eixo Z).")]
    public float baseWorldHeight = 150f;
    public float minZoom = 0.5f;     // metade do base
    public float maxZoom = 3.0f;     // 3x do base
    public float zoom = 1.0f;
    public float zoomStepButtons = 0.15f;
    public float zoomScrollSensitivity = 0.2f;



    // pooling
    readonly Dictionary<Unit, RectTransform> _icons = new();
    readonly Stack<RectTransform> _pool = new();

    void OnEnable()
    {
        // assina eventos (se quiser atualizar incrementalmente)
        UnitRegistry.OnUnitSpawned += HandleSpawn;
        UnitRegistry.OnUnitDespawned += HandleDespawn;

        RebuildAll();
        SyncMinimapCamera();
    }

    void OnDisable()
    {
        UnitRegistry.OnUnitSpawned -= HandleSpawn;
        UnitRegistry.OnUnitDespawned -= HandleDespawn;
        ClearAll();
    }

    void Update()
    {
        if (followMainView && mainCamera != null && minimapCamera != null)
        {
            if (TryGetMainCenterOnGround(out var c))
            {
                boundsCenter = c; // só o centro
                                  // o tamanho agora vem do nosso zoom, não da câmera principal
                float worldHeight = Mathf.Max(5f, baseWorldHeight * zoom); // eixo Z “vertical” do minimapa
                float aspect = (float)minimapCamera.pixelWidth / Mathf.Max(1, minimapCamera.pixelHeight);
                float worldWidth = worldHeight * aspect; // eixo X visto pela câmera

                boundsSize = new Vector2(worldWidth, worldHeight);
                SyncMinimapCamera(); // posiciona/ajusta ortho
            }
        }

        UpdateIcons();
    }



    // --- Eventos de unidade ---
    void HandleSpawn(Unit u) { CreateIcon(u); }
    void HandleDespawn(Unit u) { RemoveIcon(u); }

    // --- Build inicial ---
    void RebuildAll()
    {
        ClearAll();
        foreach (var u in UnitRegistry.All)
            CreateIcon(u);
    }

    void ClearAll()
    {
        foreach (var kv in _icons)
            ReturnIcon(kv.Value);
        _icons.Clear();
    }

    // --- Ícones ---
    void CreateIcon(Unit u)
    {
        if (u == null || _icons.ContainsKey(u)) return;
        var rt = GetIcon();
        rt.sizeDelta = iconSize;

        // cor por facção
        if (factionDb != null)
        {
            var def = factionDb.Get(u.owner);
            var img = rt.GetComponent<Image>();
            if (def != null && img != null) img.color = def.color;
        }

        _icons[u] = rt;
    }

    void RemoveIcon(Unit u)
    {
        if (u == null) return;
        if (_icons.TryGetValue(u, out var rt))
        {
            _icons.Remove(u);
            ReturnIcon(rt);
        }
    }

    RectTransform GetIcon()
    {
        RectTransform rt;
        if (_pool.Count > 0) rt = _pool.Pop();
        else rt = Instantiate(iconPrefab, iconsRoot);
        rt.gameObject.SetActive(true);
        rt.SetAsLastSibling();
        return rt;
    }

    void ReturnIcon(RectTransform rt)
    {
        if (!rt) return;
        rt.gameObject.SetActive(false);
        rt.SetParent(iconsRoot, false);
        _pool.Push(rt);
    }

    void UpdateIcons()
    {
        if (!iconsRoot || minimapCamera == null) return;
        var rect = iconsRoot.rect;

        foreach (var kv in _icons)
        {
            var u = kv.Key;
            var rt = kv.Value;
            if (u == null) { ReturnIcon(rt); continue; }

            Vector3 vp = minimapCamera.WorldToViewportPoint(u.transform.position); // (x,y) 0..1
            bool outside =
                vp.z < 0f ||                             // atrás da câmera
                vp.x < -uvBorderTolerance || vp.x > 1f + uvBorderTolerance ||
                vp.y < -uvBorderTolerance || vp.y > 1f + uvBorderTolerance;

            if (hideIconsOutside && outside)
            {
                if (rt.gameObject.activeSelf) rt.gameObject.SetActive(false);
                continue;
            }
            else if (!rt.gameObject.activeSelf) rt.gameObject.SetActive(true);

            float uNorm = Mathf.Clamp01(vp.x);
            float vNorm = Mathf.Clamp01(vp.y);

            Vector2 anchored = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, uNorm),
                Mathf.Lerp(rect.yMin, rect.yMax, vNorm)
            );

            rt.anchoredPosition = anchored;

            if (fadeNearBorder)
            {
                var img = rt.GetComponent<Image>();
                if (img != null)
                {
                    float edge = Mathf.Min(uNorm, 1f - uNorm, vNorm, 1f - vNorm);
                    float a = Mathf.Clamp01(edge / Mathf.Max(0.0001f, fadeWidthUV));
                    var c = img.color; c.a = a; img.color = c;
                }
            }
        }
    }

    Vector2 ScreenToLocal(RectTransform rt, Vector2 screenPos, Camera uiCam)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, uiCam, out var local);
        return local;
    }

    // --- Click para mover a câmera principal ---
    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left &&
            e.button != PointerEventData.InputButton.Right) return;
        if (!minimapImage || !rtsCamera || minimapCamera == null) return;

        // converte clique na área do RawImage para viewport 0..1
        var local = ScreenToLocal(minimapImage.rectTransform, e.position, e.pressEventCamera);
        var rect = minimapImage.rectTransform.rect;
        float u = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
        float v = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);

        // ray a partir da MinimapCamera
        Ray ray = minimapCamera.ViewportPointToRay(new Vector3(u, v, 0f));
        Vector3 world;
        if (Physics.Raycast(ray, out var hit, 50000f, groundMask, QueryTriggerInteraction.Ignore))
            world = hit.point;
        else
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
            if (!plane.Raycast(ray, out float t)) return;
            world = ray.GetPoint(t);
        }

        rtsCamera.GoToXZ(new Vector2(world.x, world.z), snap: false, duration: 0.35f);
    }
    public void OnScroll(PointerEventData eventData)
    {
        // scroll up = aproximar (diminuir world height) => zoom menor
        float delta = -eventData.scrollDelta.y * zoomScrollSensitivity;
        SetZoom(zoom + delta);
    }
    public void ZoomIn() { SetZoom(zoom - zoomStepButtons); }
    public void ZoomOut() { SetZoom(zoom + zoomStepButtons); }
    void SetZoom(float z)
    {
        zoom = Mathf.Clamp(z, minZoom, maxZoom);
        // força um Sync agora (Update também fará)
        SyncMinimapCamera();
    }

    // --- Ajusta a MinimapCamera para “enquadrar” o mapa ---
    void SyncMinimapCamera()
    {
        if (!minimapCamera) return;

        minimapCamera.transform.position =
            new Vector3(boundsCenter.x, minimapCamera.transform.position.y, boundsCenter.y);

        minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        minimapCamera.orthographic = true;

        // ortho size é metade da ALTURA visível (em world units)
        minimapCamera.orthographicSize = Mathf.Max(1f, boundsSize.y * 0.5f);
    }


    bool TryGetMainCenterOnGround(out Vector2 centerXZ)
    {
        centerXZ = default;
        if (mainCamera == null) return false;

        // Ray do centro da tela para o chão
        var ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out var hit, 50000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            centerXZ = new Vector2(hit.point.x, hit.point.z);
            return true;
        }
        else
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
            if (plane.Raycast(ray, out float t))
            {
                var p = ray.GetPoint(t);
                centerXZ = new Vector2(p.x, p.z);
                return true;
            }
        }
        return false;
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        var c = new Vector3(boundsCenter.x, groundY, boundsCenter.y);
        var s = new Vector3(boundsSize.x, 0.1f, boundsSize.y);
        Gizmos.DrawCube(c, s);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(c, s);
    }


}

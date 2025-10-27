using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitListItemUI : MonoBehaviour
{
    [Header("Refs (arraste do prefab)")]
    public Image portrait;
    public TMP_Text nameText;
    public TMP_Text levelText;

    [Space]
    [Tooltip("Parte reta (90%). Image.type = Filled/Horizontal")]
    public Image barFill;

    [Tooltip("Círculo (10%). Image.type = Filled/Radial360")]
    public Image circleFill;

    [Header("Selection Visual")]
    public Outline outline;

    Unit _unit;
    public Unit Unit => _unit;

    void Awake()
    {
        if (barFill) barFill.type = Image.Type.Filled;
        if (circleFill) circleFill.type = Image.Type.Filled;
    }

    public void Bind(Unit unit)
    {
        // REFATORAÇÃO: Desinscrever do evento antigo (local)
        // if (_unit != null) _unit.OnProgressChanged -= OnUnitProgressChanged;  // EVENTO LOCAL REMOVIDO

        // Desinscrever do GameEvents se já estava inscrito
        if (_unit != null)
        {
            GameEvents.OnUnitProgressChanged -= OnUnitProgressChanged;
        }

        _unit = unit;

        if (nameText) nameText.text = unit.DisplayName;
        if (portrait) portrait.sprite = unit.def ? unit.def.icon : null;

        Refresh();

        // REFATORAÇÃO: Inscrever no GameEvents em vez do evento local
        GameEvents.OnUnitProgressChanged += OnUnitProgressChanged;
    }

    public void Unbind()
    {
        // REFATORAÇÃO: Desinscrever do GameEvents em vez do evento local
        if (_unit != null)
        {
            GameEvents.OnUnitProgressChanged -= OnUnitProgressChanged;
        }
        _unit = null;
    }

    void OnDisable() => Unbind();

    // REFATORAÇÃO: Handler agora recebe Unit como parâmetro do GameEvents
    void OnUnitProgressChanged(Unit changedUnit)
    {
        // Só atualizar se for a unidade vinculada a este UI
        if (changedUnit == _unit)
        {
            Refresh();
        }
    }

    public void RefreshNow() => Refresh();

    void Refresh()
    {
        if (_unit == null) return;
        if (levelText) levelText.text = _unit.Level.ToString();
        SetXp01(_unit.Xp01);
    }

    void SetXp01(float t)
    {
        t = Mathf.Clamp01(t);

        float straightPart = Mathf.Min(t, 0.9f) / 0.9f;
        if (barFill) barFill.fillAmount = straightPart;

        float circlePart = (t <= 0.9f) ? 0f : (t - 0.9f) / 0.1f;
        if (circleFill) circleFill.fillAmount = circlePart;
    }

    public void SetSelected(bool selected)
    {
        if (outline) outline.enabled = selected;
    }

#if UNITY_EDITOR
    [Header("Debug (Editor)")]
    [Range(0, 1)] public float debugXp01;
    void OnValidate()
    {
        if (!Application.isPlaying) SetXp01(debugXp01);
    }
#endif
}

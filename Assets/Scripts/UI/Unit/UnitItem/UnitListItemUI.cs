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
        if (_unit != null) _unit.OnProgressChanged -= OnUnitProgressChanged;
        _unit = unit;

        if (nameText) nameText.text = unit.DisplayName;
        if (portrait) portrait.sprite = unit.def ? unit.def.icon : null;

        Refresh();
        _unit.OnProgressChanged += OnUnitProgressChanged;
    }

    public void Unbind()
    {
        if (_unit != null) _unit.OnProgressChanged -= OnUnitProgressChanged;
        _unit = null;
    }

    void OnDisable() => Unbind();

    void OnUnitProgressChanged(Unit _) => Refresh();

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

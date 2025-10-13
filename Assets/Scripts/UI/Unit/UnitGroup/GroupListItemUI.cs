using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class GroupListItemUI : MonoBehaviour
{
    [Header("Refs Visuais")]
    public TMP_Text nameText;
    public Toggle expandToggle;
    public Outline outline;
    public Outline headerOutline;

    [Header("Content Aninhado")]
    [Tooltip("O RectTransform que contém os UnitListItemUI filhos.")]
    public RectTransform subContent;

    [Header("Layout")]
    [Tooltip("LayoutElement do root do item de grupo (para restaurar altura). Se nulo, será encontrado via GetComponent<LayoutElement>().")]
    [SerializeField] LayoutElement rootLayout;

    [Tooltip("O RectTransform do cabeçalho 'Name' (para altura mínima).")]
    [SerializeField] RectTransform headerRect;


    // Opcional: Adicionar referência ao ScrollRect interno (se o grupo for redimensionável)
    public ScrollRect innerScrollRect;

    private UnitGroup _groupModel;
    private float _collapsedHeight = -1f;

    // NOVO: Cache do ResizeGripHandler para controle
    private ResizeGripHandler _resizeGripHandler;


    public UnitGroup GroupModel => _groupModel;
    public IReadOnlyList<Unit> Units => _groupModel?.Units;

    void Awake()
    {
        // Certifica-se de que temos o LayoutElement e o Header Rect
        if (rootLayout == null) rootLayout = GetComponent<LayoutElement>();
        if (headerRect == null) headerRect = transform.Find("Name") as RectTransform;

        // NOVO: Tenta encontrar o ResizeGripHandler no Awake
        _resizeGripHandler = GetComponentInChildren<ResizeGripHandler>(true);

        if (headerRect != null)
        {
            _collapsedHeight = headerRect.rect.height;
        }

        if (expandToggle)
        {
            expandToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            expandToggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
    }

    public void Bind(UnitGroup group)
    {
        _groupModel = group;
        if (nameText) nameText.text = group.GroupName;

        // Tenta restaurar a altura preferida do LayoutElement a partir do modelo de dados
        if (rootLayout != null && _groupModel != null && _groupModel.PreferredHeight > 0)
        {
            rootLayout.preferredHeight = _groupModel.PreferredHeight;
        }

        RefreshVisuals();
    }


    public void Unbind()
    {
        _groupModel = null;
    }

    // GroupListItemUI.cs (SUBSTITUIR RefreshVisuals)

    public void RefreshVisuals()
    {
        if (_groupModel == null || rootLayout == null) return;

        if (expandToggle) expandToggle.SetIsOnWithoutNotify(_groupModel.IsExpanded);

        if (_groupModel.IsExpanded)
        {
            // EXPANDIDO:

            // 1. Remove qualquer restrição de altura mínima
            rootLayout.minHeight = 0f;

            // 2. Restaura o preferredHeight do modelo.
            if (_groupModel.PreferredHeight > 0)
            {
                rootLayout.preferredHeight = _groupModel.PreferredHeight;
            }

            if (innerScrollRect != null) innerScrollRect.gameObject.SetActive(true);
            if (subContent) subContent.gameObject.SetActive(true);

            // NOVO: Adiciona a checagem 'rootLayout.minHeight = 0f;' novamente (para garantir)
            rootLayout.minHeight = 0f;

            // Ativa o ResizeGrip
            if (_resizeGripHandler) _resizeGripHandler.enabled = true;
        }
        else
        {
            // RECOLHIDO:

            // 1. Salva a altura expandida atual (se o grupo não estava recolhido antes)
            if (rootLayout.preferredHeight > _collapsedHeight)
            {
                _groupModel.PreferredHeight = rootLayout.preferredHeight;
            }

            // 2. Define a nova altura mínima e preferida (altura do cabeçalho 'Name')
            rootLayout.preferredHeight = _collapsedHeight;
            rootLayout.minHeight = _collapsedHeight; // Trava o tamanho no mínimo

            if (innerScrollRect != null) innerScrollRect.gameObject.SetActive(false);
            if (subContent) subContent.gameObject.SetActive(false);

            // Desativa o ResizeGrip
            if (_resizeGripHandler) _resizeGripHandler.enabled = false;
        }
    }

    /// <summary>
    /// Atualiza apenas visuais de filhos (Scroll/subContent) respeitando IsExpanded,
    /// SEM tocar em preferredHeight/minHeight. Útil após reordenar/adicionar/remover itens.
    /// </summary>
    public void RefreshChildrenOnly()
    {
        if (_groupModel == null) return;

        bool expanded = _groupModel.IsExpanded;

        if (expandToggle) expandToggle.SetIsOnWithoutNotify(expanded);
        if (innerScrollRect) innerScrollRect.gameObject.SetActive(expanded);
        if (subContent) subContent.gameObject.SetActive(expanded);

        // não mexe em rootLayout.preferredHeight/minHeight aqui
    }


    private void OnToggleValueChanged(bool isExpanded)
    {
        if (_groupModel != null)
        {
            _groupModel.IsExpanded = isExpanded;
            RefreshVisuals(); // Dispara a ativação/desativação e o controle do ResizeGrip

            var panel = GetComponentInParent<UnitListPanel>();

            if (isExpanded && panel != null)
            {
                panel.BuildGroupContent(_groupModel, subContent);

                panel.RefreshFromSelection(panel.SelectionManager.Selection);
            }

            if (transform.parent is RectTransform parentRt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
            }
        }
    }

    public void SetSelected(bool selected)
    {
        if (outline) outline.enabled = selected;
        if (headerOutline) headerOutline.enabled = selected;
    }
    public void OnSelectButtonClicked(bool isDoubleClick)
    {
        // 1. Evita que o clique do botão seja disparado imediatamente após o drag-and-drop
        // (Isso é uma proteção extra, pois o ReorderableListItem já cuida disso, mas é bom ter)
        var dropZone = GetComponentInChildren<GroupDropZone>();
        if (dropZone != null && Time.frameCount - dropZone.LastDropFrame <= 2)
        {
            return;
        }

        var panel = GetComponentInParent<UnitListPanel>();

        if (_groupModel == null || panel == null)
        {
            panel = GetComponentInParent<UnitListPanel>();
            if (panel == null) return;
        }

        bool ctrl = panel.InputSelection != null && panel.InputSelection.IsCtrlPressed;
        bool shift = panel.InputSelection != null && panel.InputSelection.IsShiftPressed;

        var selectionManager = panel.SelectionManager;
        var unitsInGroup = _groupModel.Units;


        if (isDoubleClick) // Duplo Clique (chamado pelo mouse externo, não pelo botão)
        {
            var unitInGroup = unitsInGroup.FirstOrDefault();
            if (unitInGroup != null)
            {
                var sameTypeInGroup = unitsInGroup
                    .Where(u => u != null && u.def == unitInGroup.def);
                selectionManager.SelectExactly(sameTypeInGroup);
            }
            return;
        }

        // --- Lógica de Seleção (Chamada pelo Button ou Clique Simples) ---
        if (shift)
        {
            if (ctrl) selectionManager.AddToSelection(unitsInGroup);
            else selectionManager.SelectExactly(unitsInGroup);
        }
        else if (ctrl)
        {
            selectionManager.ToggleSet(unitsInGroup);
        }
        else
        {
            selectionManager.SelectExactly(unitsInGroup);
        }

        // Registra a âncora de intervalo para Shift+Click
        panel.CommitSelectionForGroup(_groupModel);
    }
    public void OnButtonSelectClicked()
    {
        // O Button sempre representa um clique simples (não duplo)
        OnSelectButtonClicked(false);
    }
}
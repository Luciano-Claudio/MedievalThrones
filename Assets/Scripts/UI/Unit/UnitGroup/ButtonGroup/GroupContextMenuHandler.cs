using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// Controller do menu de contexto do grupo (Rename/Delete).
/// Anexado ao prefab ButtonsGroup.
/// </summary>
public class GroupContextMenuHandler : MonoBehaviour
{
    [Header("UI Elementos")]
    [SerializeField] Button renameButton;
    [SerializeField] Button deleteButton;
    [SerializeField] TMP_InputField renameInput;

    // --- Referências Injetadas ---
    private UnitGroup _targetGroup;
    private UnitListPanel _rootPanel;
    private TMP_Text _headerText; // O TextMeshPro do cabeçalho do grupo
    private GroupHeaderContextMenu _contextMenu; // O controller que o criou (para fechar)

    void Awake()
    {
        // Garante que o input está escondido por padrão
        if (renameInput) renameInput.gameObject.SetActive(false);

        // Liga os botões aos métodos (garante que os listeners sejam limpos)
        if (renameButton) renameButton.onClick.AddListener(OnRenameClicked);
        if (deleteButton) deleteButton.onClick.AddListener(OnDeleteClicked);

        // Liga a ação de fim de edição do input para aplicar o novo nome
        if (renameInput) renameInput.onEndEdit.AddListener(OnRenameInputEndEdit);
    }

    /// <summary>
    /// CRÍTICO: Injeta as referências necessárias do item de grupo que disparou o menu.
    /// </summary>
    public void Setup(UnitListPanel panel, UnitGroup group, TMP_Text headerText, GroupHeaderContextMenu contextMenu)
    {
        _rootPanel = panel;
        _targetGroup = group;
        _headerText = headerText;
        _contextMenu = contextMenu;
    }

    // --------------------------------------------------
    // AÇÕES DO BOTÃO
    // --------------------------------------------------

    private void OnRenameClicked()
    {
        if (_targetGroup == null || renameInput == null) return;

        // 1. Exibe o input e oculta os botões (UX)
        if (renameButton) renameButton.gameObject.SetActive(false);
        if (deleteButton) deleteButton.gameObject.SetActive(false);
        renameInput.gameObject.SetActive(true);

        // 2. Preenche o input com o nome atual e dá foco
        renameInput.text = _targetGroup.GroupName;
        renameInput.ActivateInputField();
        renameInput.Select();
    }

    private void OnRenameInputEndEdit(string newName)
    {
        if (_targetGroup == null || renameInput == null) return;

        var trimmedName = newName.Trim();

        // 1. Aplica o novo nome (se for válido)
        if (!string.IsNullOrEmpty(trimmedName))
        {
            _targetGroup.GroupName = trimmedName;

            // 2. Atualiza o visual do cabeçalho do grupo
            if (_headerText) _headerText.text = trimmedName;
        }

        // 3. Fecha o menu de contexto (limpeza)
        _contextMenu?.CloseMenu();
    }

    private void OnDeleteClicked()
    {
        if (_targetGroup == null || _rootPanel == null) return;

        // --- LÓGICA DE DELETE ---

        // Os itens do grupo devem voltar para a lista principal (raiz).
        // Vamos usar o helper EnumerateAllUnitsInGroup (a ser criado no UnitListPanel)
        var unitsToRestore = _targetGroup.Units.ToList(); // Faz uma cópia da lista

        // NOVO MÉTODO (A ser criado no UnitListPanel): Deleta o grupo e move unidades
        _rootPanel.DeleteGroupAndRestoreUnits(_targetGroup, unitsToRestore);

        // Fecha o menu
        _contextMenu?.CloseMenu();
    }
}
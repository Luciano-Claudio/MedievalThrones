using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateGroupUI : MonoBehaviour
{
    [Header("Scene Refs")]
    [SerializeField] UnitListPanel rootPanel;     // Arraste o UnitList (raiz)
    [SerializeField] SelectionManager selection;  // O mesmo usado na lista
    [SerializeField] InputSelection inputSel;     // Idem
    [SerializeField] RectTransform dragRoot;      // Seu GO "DragRoot" fora da lista
    [SerializeField] Canvas canvasUI;             // O Canvas da UI (se vazio, será auto)

    [Header("UI")]
    [SerializeField] TMP_InputField nameInput;    // O input do nome
    [SerializeField] Button createButton;         // Botão "Create Group"

    [Header("Prefab")]
    [SerializeField] GroupListItemUI groupPrefab; // Seu Prefab "Group" (agora tipado)

    void Reset()
    {
        if (!canvasUI) canvasUI = GetComponentInParent<Canvas>();
        if (!rootPanel) rootPanel = FindFirstObjectByType<UnitListPanel>();
        if (!selection) selection = FindFirstObjectByType<SelectionManager>();
        if (!inputSel) inputSel = FindFirstObjectByType<InputSelection>();
        if (!dragRoot)
        {
            var t = GameObject.Find("DragRoot");
            if (t) dragRoot = t.transform as RectTransform;
        }
    }

    void Awake()
    {
        if (createButton)
        {
            createButton.onClick.RemoveListener(CreateGroup);
            createButton.onClick.AddListener(CreateGroup);
        }
    }

    /// <summary>
    /// Instancia o grupo visualmente e chama o método de dados no UnitListPanel.
    /// </summary>
    public void CreateGroup()
    {
        if (rootPanel == null || rootPanel.Content == null)
        {
            Debug.LogError("[CreateGroupUI] Root panel/content não está ligado.");
            return;
        }
        if (groupPrefab == null)
        {
            Debug.LogError("[CreateGroupUI] Prefab do Group não está ligado.");
            return;
        }

        // 1. Cria o modelo de dados (UnitListPanel adiciona o Wrapper e reconstrói a lista)
        // Isso é o código monolítico que queríamos evitar, mas que o UnitListPanel deve fazer para gerenciar _order.

        var desiredName = string.IsNullOrWhiteSpace(nameInput?.text) ? "Novo Grupo" : nameInput.text.Trim();

        // 2. Cria o modelo de dados e força a reconstrução.
        // O UnitListPanel precisa de um método para criar o modelo e retornar a referência para o grupo.
        // Vamos usar o método que criamos, mas vamos passá-lo para que o UnitListPanel gerencie.

        // HACK: Já que o UnitListPanel.Build() reconstrói a lista inteira, nós apenas chamamos
        // a ação de criação de dados e confiamos no Build() subsequente.

        rootPanel.CreateNewGroup(desiredName); // Método a ser criado no UnitListPanel

        // 3. (Opcional) Limpa foco do input
        if (nameInput) nameInput.text = string.Empty;
    }
}
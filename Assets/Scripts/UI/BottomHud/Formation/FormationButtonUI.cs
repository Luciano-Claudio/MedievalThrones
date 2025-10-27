using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper para conectar botões de UI ao MovementCommandHandler
/// Attach em cada botão de formação
/// Suporta todas as formações incluindo None
/// </summary>
public class FormationButtonUI : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private FormationType targetFormation;

    [Header("Visual Feedback (Opcional)")]
    [Tooltip("Imagem do botão que pode ser destacada quando a formação está ativa")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    [Header("Referências (Auto-encontradas)")]
    [SerializeField] private MovementCommandHandler movementCommandHandler;
    [SerializeField] private Button button;

    void Awake()
    {
        // Auto-encontrar componentes
        if (!movementCommandHandler)
            movementCommandHandler = Object.FindFirstObjectByType<MovementCommandHandler>();

        if (!button)
            button = GetComponent<Button>();

        if (!buttonImage)
            buttonImage = GetComponent<Image>();
    }

    void Start()
    {
        // Conectar callback do botão
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogError($"[FormationButtonUI] Button component não encontrado em {gameObject.name}!");
        }
    }

    void Update()
    {
        // Atualizar visual se a formação atual é esta
        UpdateVisualFeedback();
    }

    void OnButtonClicked()
    {
        if (movementCommandHandler != null)
        {
            movementCommandHandler.SetFormation(targetFormation);

            string formationName = targetFormation == FormationType.None ? "Nenhuma (Livre)" : targetFormation.ToString();
            Debug.Log($"<color=cyan>[FormationButtonUI] Formação mudada para {formationName}</color>");
        }
        else
        {
            Debug.LogError("[FormationButtonUI] MovementCommandHandler não encontrado!");
        }
    }

    /// <summary>
    /// Atualiza o feedback visual do botão baseado na formação atual
    /// </summary>
    void UpdateVisualFeedback()
    {
        if (buttonImage == null || movementCommandHandler == null)
            return;

        bool isCurrentFormation = movementCommandHandler.DefaultFormation == targetFormation;
        buttonImage.color = isCurrentFormation ? selectedColor : normalColor;
    }

    void OnDestroy()
    {
        // Limpar callback
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    void OnValidate()
    {
        // Ajudar a configurar o botão no Inspector
        if (button == null)
            button = GetComponent<Button>();

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
    }
}
using UnityEngine;

public class DisableMinimapShadows : MonoBehaviour
{
    // Este método é chamado antes que a câmera comece a renderizar
    void OnPreRender()
    {
        // Desabilita as sombras SOMENTE para esta câmera.
        // A configuração global de sombras é salva antes da renderização.
        QualitySettings.shadows = ShadowQuality.Disable;
    }

    // Este método é chamado após a câmera terminar de renderizar
    void OnPostRender()
    {
        // Restaura a configuração global de sombras
        // para não afetar as outras câmeras (como a principal do jogo).
        QualitySettings.shadows = ShadowQuality.All; // Ou o valor que você tinha na configuração de qualidade padrão
    }
}
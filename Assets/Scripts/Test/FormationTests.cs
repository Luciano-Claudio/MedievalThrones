using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Testes de validação para o sistema de formações
/// Attach em um GameObject vazio e use os Context Menus para testar
/// </summary>
public class FormationTests : MonoBehaviour
{
    [Header("Configuração de Teste")]
    [SerializeField] private int numberOfTestUnits = 15;
    [SerializeField] private GameObject unitPrefab; // Opcional: prefab para criar unidades de teste

    // ========== TESTES DE FORMAÇÃO LINE ==========

    [ContextMenu("Test 1: Line Formation - Passagem Central")]
    void TestLineFormationPassage()
    {
        Debug.Log("=== TESTE: LINE FORMATION - PASSAGEM CENTRAL ===");

        List<Unit> testUnits = CreateTestUnits(10);
        var formation = FormationCalculator.CalculateFormation(testUnits, FormationType.Line, 2f);

        // Verificar se há unidades em posições negativas e positivas (passagem)
        bool hasLeft = false;
        bool hasRight = false;
        bool hasCenter = false;

        foreach (var offset in formation.Values)
        {
            if (offset.x < -1f) hasLeft = true;
            if (offset.x > 1f) hasRight = true;
            if (offset.x >= -1f && offset.x <= 1f) hasCenter = true;
        }

        Debug.Log($"✓ Lado esquerdo: {hasLeft}");
        Debug.Log($"✓ Lado direito: {hasRight}");
        Debug.Log($"✓ Centro (passagem): {!hasCenter} (deve ser false)");

        if (hasLeft && hasRight && !hasCenter)
        {
            Debug.Log("<color=green>✓ PASSOU: Line tem passagem central!</color>");
        }
        else
        {
            Debug.LogError("✗ FALHOU: Line não tem passagem correta!");
        }

        CleanupTestUnits(testUnits);
    }

    // ========== TESTES DE FORMAÇÃO COLUMN ==========

    [ContextMenu("Test 2: Column Formation - 5 Unidades")]
    void TestColumnFormation5Units()
    {
        Debug.Log("=== TESTE: COLUMN FORMATION - 5 UNIDADES ===");

        List<Unit> testUnits = CreateTestUnits(5);
        var formation = FormationCalculator.CalculateFormation(testUnits, FormationType.Column, 2f);

        // Contar colunas únicas
        HashSet<float> uniqueXPositions = new HashSet<float>();
        foreach (var offset in formation.Values)
        {
            uniqueXPositions.Add(Mathf.Round(offset.x * 10f) / 10f); // Arredondar para evitar floating point issues
        }

        int columnCount = uniqueXPositions.Count;
        Debug.Log($"Colunas detectadas: {columnCount}");

        if (columnCount >= 2)
        {
            Debug.Log("<color=green>✓ PASSOU: 5 unidades formam pelo menos 2 colunas!</color>");
        }
        else
        {
            Debug.LogError($"✗ FALHOU: Apenas {columnCount} coluna(s) encontrada(s)!");
        }

        CleanupTestUnits(testUnits);
    }

    [ContextMenu("Test 3: Column Formation - 30 Unidades")]
    void TestColumnFormation30Units()
    {
        Debug.Log("=== TESTE: COLUMN FORMATION - 30 UNIDADES ===");

        List<Unit> testUnits = CreateTestUnits(30);
        var formation = FormationCalculator.CalculateFormation(testUnits, FormationType.Column, 2f);

        // Contar colunas únicas
        HashSet<float> uniqueXPositions = new HashSet<float>();
        foreach (var offset in formation.Values)
        {
            uniqueXPositions.Add(Mathf.Round(offset.x * 10f) / 10f);
        }

        int columnCount = uniqueXPositions.Count;
        Debug.Log($"Colunas detectadas: {columnCount}");

        if (columnCount >= 4 && columnCount <= 8)
        {
            Debug.Log($"<color=green>✓ PASSOU: 30 unidades formam {columnCount} colunas (esperado 4-8)!</color>");
        }
        else
        {
            Debug.LogWarning($"⚠ AVISO: {columnCount} colunas (esperado 4-8)");
        }

        CleanupTestUnits(testUnits);
    }

    // ========== TESTES DE FORMAÇÃO NONE ==========

    [ContextMenu("Test 4: None Formation - Offsets Zero")]
    void TestNoneFormation()
    {
        Debug.Log("=== TESTE: NONE FORMATION - OFFSETS ZERO ===");

        List<Unit> testUnits = CreateTestUnits(10);
        var formation = FormationCalculator.CalculateFormation(testUnits, FormationType.None, 2f);

        bool allZero = true;
        foreach (var offset in formation.Values)
        {
            if (offset != Vector3.zero)
            {
                allZero = false;
                break;
            }
        }

        if (allZero)
        {
            Debug.Log("<color=green>✓ PASSOU: None retorna offsets zero para todas as unidades!</color>");
        }
        else
        {
            Debug.LogError("✗ FALHOU: None não retorna offsets zero!");
        }

        CleanupTestUnits(testUnits);
    }

    // ========== TESTES DE CENTRO INTELIGENTE ==========

    [ContextMenu("Test 5: Centro Inteligente - Com Outlier")]
    void TestIntelligentCenterWithOutlier()
    {
        Debug.Log("=== TESTE: CENTRO INTELIGENTE - COM OUTLIER ===");

        List<Unit> testUnits = CreateTestUnits(11);

        // Posicionar 10 unidades próximas
        for (int i = 0; i < 10; i++)
        {
            testUnits[i].transform.position = new Vector3(i * 2f, 0, 0);
        }

        // Posicionar 1 unidade distante (outlier)
        testUnits[10].transform.position = new Vector3(100f, 0, 0);

        Vector3 intelligentCenter = FormationCalculator.CalculateIntelligentCenter(testUnits);

        // Centro deve estar próximo das 10 unidades, não da outlier
        float distanceToMainGroup = Vector3.Distance(intelligentCenter, new Vector3(9f, 0, 0));
        float distanceToOutlier = Vector3.Distance(intelligentCenter, new Vector3(100f, 0, 0));

        Debug.Log($"Centro calculado: {intelligentCenter}");
        Debug.Log($"Distância ao grupo principal: {distanceToMainGroup:F2}");
        Debug.Log($"Distância ao outlier: {distanceToOutlier:F2}");

        if (distanceToMainGroup < 20f && distanceToOutlier > 70f)
        {
            Debug.Log("<color=green>✓ PASSOU: Centro inteligente ignorou o outlier!</color>");
        }
        else
        {
            Debug.LogError("✗ FALHOU: Centro não ignorou o outlier corretamente!");
        }

        CleanupTestUnits(testUnits);
    }

    [ContextMenu("Test 6: Centro Inteligente - Sem Outliers")]
    void TestIntelligentCenterWithoutOutliers()
    {
        Debug.Log("=== TESTE: CENTRO INTELIGENTE - SEM OUTLIERS ===");

        List<Unit> testUnits = CreateTestUnits(10);

        // Posicionar todas as unidades próximas
        for (int i = 0; i < 10; i++)
        {
            testUnits[i].transform.position = new Vector3(i * 2f, 0, 0);
        }

        Vector3 intelligentCenter = FormationCalculator.CalculateIntelligentCenter(testUnits);
        Vector3 expectedCenter = new Vector3(9f, 0, 0); // Centro de 0 a 18

        float distance = Vector3.Distance(intelligentCenter, expectedCenter);

        Debug.Log($"Centro calculado: {intelligentCenter}");
        Debug.Log($"Centro esperado: {expectedCenter}");
        Debug.Log($"Diferença: {distance:F2}");

        if (distance < 2f)
        {
            Debug.Log("<color=green>✓ PASSOU: Centro inteligente está correto sem outliers!</color>");
        }
        else
        {
            Debug.LogWarning($"⚠ AVISO: Centro com diferença de {distance:F2} (esperado < 2)");
        }

        CleanupTestUnits(testUnits);
    }

    // ========== TESTE COMPLETO ==========

    [ContextMenu("Test 7: EXECUTAR TODOS OS TESTES")]
    void RunAllTests()
    {
        Debug.Log("\n" +
                  "╔═══════════════════════════════════════════════════════╗\n" +
                  "║         EXECUTANDO TODOS OS TESTES                    ║\n" +
                  "╚═══════════════════════════════════════════════════════╝\n");

        TestLineFormationPassage();
        Debug.Log("\n---\n");

        TestColumnFormation5Units();
        Debug.Log("\n---\n");

        TestColumnFormation30Units();
        Debug.Log("\n---\n");

        TestNoneFormation();
        Debug.Log("\n---\n");

        TestIntelligentCenterWithOutlier();
        Debug.Log("\n---\n");

        TestIntelligentCenterWithoutOutliers();

        Debug.Log("\n" +
                  "╔═══════════════════════════════════════════════════════╗\n" +
                  "║         TESTES CONCLUÍDOS                             ║\n" +
                  "╚═══════════════════════════════════════════════════════╝\n");
    }

    // ========== UTILITÁRIOS ==========

    /// <summary>
    /// Cria unidades de teste
    /// </summary>
    List<Unit> CreateTestUnits(int count)
    {
        List<Unit> units = new List<Unit>();

        for (int i = 0; i < count; i++)
        {
            GameObject go;

            if (unitPrefab != null)
            {
                go = Instantiate(unitPrefab);
            }
            else
            {
                go = new GameObject($"TestUnit_{i}");
                go.AddComponent<Unit>();
            }

            Unit unit = go.GetComponent<Unit>();
            units.Add(unit);
        }

        return units;
    }

    /// <summary>
    /// Limpa unidades de teste
    /// </summary>
    void CleanupTestUnits(List<Unit> units)
    {
        foreach (var unit in units)
        {
            if (unit != null)
                DestroyImmediate(unit.gameObject);
        }
    }

    // ========== TESTES VISUAIS ==========

    [ContextMenu("Visual Test: Draw All Formations")]
    void VisualTestAllFormations()
    {
        Debug.Log("=== TESTE VISUAL: TODAS AS FORMAÇÕES ===");
        Debug.Log("Veja a Scene View para visualizar as formações");

        // Este teste deve ser visto na Scene View com Gizmos habilitados
        // Implemente OnDrawGizmos para visualizar
    }

#if UNITY_EDITOR
    /// <summary>
    /// Desenha as formações para testes visuais
    /// </summary>
    void OnDrawGizmos()
    {
        // Implementar visualização de formações se necessário
        // Por exemplo, desenhar as 6 formações lado a lado
    }
#endif
}
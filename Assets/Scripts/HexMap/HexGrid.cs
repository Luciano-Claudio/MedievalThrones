using UnityEngine;

public class HexGrid : MonoBehaviour
{
    [Header("Prefab e dimensão")]
    public GameObject hexPrefab;          // seu prisma hex
    public float edgeToEdge = 1f;         // distância entre centros que compartilham a MESMA aresta (queremos 1.0)

    [Header("Forma do mapa")]
    public int cols = 8, rows = 5;

    void Start()
    {
        GenerateRect();
    }

    // ---- Geometria: converte “edge-to-edge” (2*apotema) em “size” (centro->canto)
    float Size() => edgeToEdge / Mathf.Sqrt(3f); // s = 1/√3 ≈ 0.57735 quando edgeToEdge = 1
    Quaternion TileRotation() => Quaternion.Euler(90f, 0f, 0f); // rotação X = 90°

    void GenerateRect()
    {
        var rot = TileRotation();
        float s = Size();
        float xStep = 1.5f * s;               // ≈ 0.8660254
        float zStep = Mathf.Sqrt(3f) * s;     // = 1.0

        for (int q = 0; q < cols; q++)
        {
            for (int r = 0; r < rows; r++)
            {
                float x = q * xStep;
                float z = (r + 0.5f * (q & 1)) * zStep;  // desloca linhas ímpares em +0.5
                Instantiate(hexPrefab, new Vector3(x, 0f, z), rot, transform);
            }
        }
    }
}

using UnityEngine;

/// <summary>
/// Utilitários matemáticos puros para cálculos de câmera.
/// Facilita testes unitários e reutilização.
/// Sugestão de Refatoração #8 da documentação.
/// </summary>
public static class CameraMath
{
    /// <summary>
    /// Converte altura e ângulo de tilt em offset 3D para o CinemachineFollow.
    /// Retorna o vetor de offset no espaço mundial.
    /// </summary>
    /// <param name="height">Altura da câmera acima do pivot</param>
    /// <param name="tiltDegrees">Ângulo de inclinação em graus</param>
    /// <param name="forwardDirection">Direção forward do rig (para calcular "trás")</param>
    public static Vector3 TiltToOffset(float height, float tiltDegrees, Vector3 forwardDirection)
    {
        float tiltRad = tiltDegrees * Mathf.Deg2Rad;
        float distance = (Mathf.Tan(tiltRad) > 0.0001f)
            ? height / Mathf.Tan(tiltRad)
            : height * 2f;

        Vector3 back = -forwardDirection.normalized;
        return back * distance + Vector3.up * height;
    }

    /// <summary>
    /// Suavização cúbica (ease in-out) para interpolação de movimento.
    /// Entrada e saída normalizadas 0..1
    /// </summary>
    public static float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Interpola com suavização cúbica entre dois pontos
    /// </summary>
    public static Vector3 SmoothLerp(Vector3 start, Vector3 end, float t)
    {
        float smoothT = SmoothStep01(t);
        return Vector3.LerpUnclamped(start, end, smoothT);
    }

    /// <summary>
    /// Clamp de posição 2D (XZ) dentro de bounds retangulares
    /// </summary>
    public static Vector3 ClampToBounds(Vector3 position, Vector2 boundsCenter, Vector2 boundsSize)
    {
        var half = boundsSize * 0.5f;
        float minX = boundsCenter.x - half.x;
        float maxX = boundsCenter.x + half.x;
        float minZ = boundsCenter.y - half.y;
        float maxZ = boundsCenter.y + half.y;

        return new Vector3(
            Mathf.Clamp(position.x, minX, maxX),
            position.y,
            Mathf.Clamp(position.z, minZ, maxZ)
        );
    }

    /// <summary>
    /// Converte posição XZ (2D do mundo) em Vector3 mantendo a altura Y
    /// </summary>
    public static Vector3 XZToVector3(Vector2 xz, float y)
    {
        return new Vector3(xz.x, y, xz.y);
    }

    /// <summary>
    /// Calcula o centro e tamanho de bounds a partir de múltiplos terrenos
    /// </summary>
    public static bool CalculateTerrainBounds(Terrain[] terrains, out Vector2 center, out Vector2 size)
    {
        center = Vector2.zero;
        size = Vector2.zero;

        if (terrains == null || terrains.Length == 0)
            return false;

        Bounds totalBounds;

        if (terrains.Length == 1)
        {
            var t = terrains[0];
            var pos = t.transform.position;
            var terrainSize = t.terrainData.size;
            totalBounds = new Bounds(
                pos + new Vector3(terrainSize.x, 0, terrainSize.z) * 0.5f,
                new Vector3(terrainSize.x, 0, terrainSize.z)
            );
        }
        else
        {
            totalBounds = new Bounds();
            for (int i = 0; i < terrains.Length; i++)
            {
                var t = terrains[i];
                var sz = t.terrainData.size;
                var p = t.transform.position;
                var bb = new Bounds(
                    p + new Vector3(sz.x, 0, sz.z) * 0.5f,
                    new Vector3(sz.x, 0, sz.z)
                );
                if (i == 0)
                    totalBounds = bb;
                else
                    totalBounds.Encapsulate(bb);
            }
        }

        center = new Vector2(totalBounds.center.x, totalBounds.center.z);
        size = new Vector2(totalBounds.size.x, totalBounds.size.z);
        return true;
    }

    /// <summary>
    /// Verifica se uma posição de tela está dentro da borda para edge-pan
    /// </summary>
    public static Vector2 GetEdgePanDirection(Vector2 screenPos, int edgeThickness, int screenWidth, int screenHeight)
    {
        Vector2 direction = Vector2.zero;

        if (screenPos.x <= edgeThickness)
            direction.x = -1;
        else if (screenPos.x >= screenWidth - edgeThickness)
            direction.x = 1;

        if (screenPos.y <= edgeThickness)
            direction.y = -1;
        else if (screenPos.y >= screenHeight - edgeThickness)
            direction.y = 1;

        return direction;
    }
}
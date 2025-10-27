using UnityEngine;

public enum FactionId { Neutral = 0, Player1 = 1, Player2 = 2, Player3 = 3, Player4 = 4, PvE = 3 }

public enum ResourceType { Wood, Stone, Iron, Mithril, Food, Gold }

public enum DamageType { Slashing, Piercing, Blunt, Siege, Fire, Magic }

public enum TerrainType { Normal, Mud, Snow, Sand, RoadDirt, RoadPaved }

public enum UnitType { Worker, Warrior, Spearman, Archer, CavalryLight, Ram, Catapult, Hero }

/// <summary>
/// Tipos de formação para tropas
/// </summary>
public enum FormationType
{
    None = -1,          // Sem formação: movimentação livre sem cálculos de formação
    Line = 0,           // Linha: múltiplas linhas de 10 unidades com passagem central
    Column = 1,         // Coluna: múltiplas colunas de até 10 unidades com passagem central
    Triangular = 2,     // Triangular: triângulos inscritos, frente agressiva
    Wedge = 3,          // Ponta de Flecha (Cunha): apenas a ponta do triângulo
    Circular = 4,       // Circular: círculos concêntricos para defesa 360°
    Square = 5          // Quadrática: quadrado denso sem passagem central
}

/// <summary>
/// Tipo de comando de unidade (para os botões do Bottom HUD)
/// </summary>
public enum UnitCommandType
{
    Move = 0,           // Mover/Coletar/Atacar (padrão)
    Patrol = 1,         // Patrulhar automaticamente
    Defend = 2,         // Postura defensiva (buff após 30s)
    Formation = 3,      // Abrir popup de formações
    Build = 4           // Abrir popup de construções (futuro)
}

/// <summary>
/// Prioridade de posicionamento na formação
/// Valores maiores = mais à frente
/// </summary>
public enum FormationPriority
{
    VeryLow = 0,    // Workers (trás)
    Low = 1,        // Siege (Ram, Catapult)
    Medium = 2,     // Cavalry, Archers
    High = 3,       // Spearman
    VeryHigh = 4,   // Warrior
    Maximum = 5     // Hero (frente)
}
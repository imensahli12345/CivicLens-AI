# utils/priority_calculator.py

GRAVITY_WEIGHTS = {
    "FAIBLE":   1,
    "MOYENNE":  2,
    "ELEVEE":   3,
    "CRITIQUE": 4
}

DOMAIN_WEIGHTS = {
    "ROUTE":          1.2,
    "EAU":            1.3,
    "ECLAIRAGE":      1.0,
    "DECHETS":        1.0,
    "TRANSPORT":      1.1,
    "INFRASTRUCTURE": 1.1,
    "AUTRE":          0.9
}

def calculate_priority(
    gravity:    str,
    vote_count: int,
    domain:     str,
    zone_score: float = 1.0
) -> dict:
    """
    Calcule la priorité d'un signalement.

    Formule :
      score = (gravité × 10 + votes_bonus) × facteur_domaine × zone_score

    Paramètres :
      gravity    : FAIBLE / MOYENNE / ELEVEE / CRITIQUE
      vote_count : nombre de votes citoyens
      domain     : ROUTE / EAU / ECLAIRAGE / DECHETS / TRANSPORT / INFRASTRUCTURE / AUTRE
      zone_score : score zone géographique (0.5 → 2.0)

    Seuils de priorité :
      >= 60 → URGENTE
      >= 38 → HAUTE
      >= 20 → NORMALE
      <  20 → BASSE
    """
    base_score    = GRAVITY_WEIGHTS.get(gravity, 1) * 10
    vote_bonus    = min(vote_count * 0.5, 20)          # plafonné à +20 pts
    domain_factor = DOMAIN_WEIGHTS.get(domain, 1.0)

    final_score = (base_score + vote_bonus) * domain_factor * zone_score

    if final_score >= 60:
        priority = "URGENTE"
    elif final_score >= 38:
        priority = "HAUTE"
    elif final_score >= 20:
        priority = "NORMALE"
    else:
        priority = "BASSE"

    return {
        "score":    round(final_score, 2),
        "priority": priority
    }
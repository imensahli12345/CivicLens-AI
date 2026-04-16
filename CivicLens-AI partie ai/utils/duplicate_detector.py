# utils/duplicate_detector.py
import numpy as np

# ── Embeddings multilingues (comprend le français) ────────────────
_embedder = None

def get_embedder():
    global _embedder
    if _embedder is None:
        from sentence_transformers import SentenceTransformer
        print("[DuplicateDetector] Chargement du modèle d'embeddings...")
        _embedder = SentenceTransformer('paraphrase-multilingual-MiniLM-L12-v2')
        print("[DuplicateDetector] Modèle prêt.")
    return _embedder

def haversine_km(lat1: float, lon1: float, lat2: float, lon2: float) -> float:
    """Distance en km entre deux points GPS (formule Haversine)."""
    R = 6371.0
    dlat = np.radians(lat2 - lat1)
    dlon = np.radians(lon2 - lon1)
    a = (np.sin(dlat / 2) ** 2 +
         np.cos(np.radians(lat1)) * np.cos(np.radians(lat2)) * np.sin(dlon / 2) ** 2)
    return R * 2 * np.arcsin(np.sqrt(a))

# ── Seuils géographiques adaptatifs selon le domaine ─────────────
DOMAIN_GEO_THRESHOLDS = {
    "ROUTE":          0.10,   # 100m  — trou localisé précisément
    "ECLAIRAGE":      0.15,   # 150m  — lampadaire précis
    "DECHETS":        0.20,   # 200m  — dépôt sauvage localisé
    "INFRASTRUCTURE": 0.25,   # 250m  — bâtiment/mur spécifique
    "AUTRE":          0.30,   # 300m  — par défaut
    "EAU":            1.00,   # 1km   — panne réseau touche tout un quartier
    "TRANSPORT":      1.00,   # 1km   — arrêt de bus couvre un grand secteur
}

def get_geo_threshold(domain: str) -> float:
    """Retourne le seuil géographique adapté au domaine."""
    return DOMAIN_GEO_THRESHOLDS.get(domain.upper(), 0.30)


def is_duplicate(
    new_desc: str,
    new_lat:  float,
    new_lon:  float,
    existing_reports: list,
    domain:   str = "AUTRE",
    text_threshold: float = 0.72,   # ✅ CORRIGÉ : 0.82 → 0.72
                                    # Raison : paraphrase-multilingual-MiniLM-L12-v2
                                    # donne ~0.75 pour des phrases synonymes du même
                                    # problème (ex: "nid de poule" ≈ "trou chaussée").
                                    # 0.82 était trop strict → faux négatifs.
                                    # 0.72 attrape les paraphrases sans faux positifs.
) -> dict:
    """
    Détecte si un signalement est doublon d'un existant.
    Seuil géographique adaptatif selon le domaine.
    Si doublon → regroupe + incrémente les votes automatiquement.

    Returns:
        Cas non-doublon :
            {"is_duplicate": False, "geo_threshold_used_km": X, "domain": Y}

        Cas doublon :
            {
                "is_duplicate": True,
                "similar_report_id": 42,
                "text_similarity": 0.91,
                "distance_km": 0.08,
                "geo_threshold_used_km": 0.10,
                "domain": "ROUTE",
                "action": "increment_votes",
                "new_vote_count": 6,
                "message": "Signalement groupé avec #42 — votes: 6"
            }
    """
    if not existing_reports:
        return {"is_duplicate": False}

    from sklearn.metrics.pairwise import cosine_similarity

    embedder = get_embedder()

    # Seuil géographique adaptatif
    geo_threshold_km = get_geo_threshold(domain)

    # Embeddings texte
    new_embedding       = embedder.encode([new_desc])
    existing_texts      = [r['description'] for r in existing_reports]
    existing_embeddings = embedder.encode(existing_texts)
    similarities        = cosine_similarity(new_embedding, existing_embeddings)[0]

    # Chercher le meilleur doublon (plus similaire dans le rayon)
    best_match    = None
    best_sim      = 0.0
    best_distance = None

    for i, report in enumerate(existing_reports):
        distance_km = haversine_km(new_lat, new_lon, report['lat'], report['lon'])
        text_sim    = float(similarities[i])

        if distance_km <= geo_threshold_km and text_sim >= text_threshold:
            if text_sim > best_sim:
                best_sim      = text_sim
                best_match    = report
                best_distance = distance_km

    if best_match is None:
        return {
            "is_duplicate":          False,
            "geo_threshold_used_km": geo_threshold_km,
            "domain":                domain
        }

    # ── Doublon détecté → grouper + incrémenter votes ────────────
    current_votes = best_match.get('vote_count', 0)
    new_votes     = current_votes + 1

    return {
        "is_duplicate":          True,
        "similar_report_id":     best_match['id'],
        "text_similarity":       round(best_sim, 3),
        "distance_km":           round(best_distance, 3),
        "geo_threshold_used_km": geo_threshold_km,
        "domain":                domain,
        "action":                "increment_votes",
        "new_vote_count":        new_votes,
        "message":               f"Signalement groupé avec #{best_match['id']} — votes: {new_votes}"
    }
# utils/duplicate_detector.py
import numpy as np

# ── Embeddings multilingues (comprend le français) ────────────────
# Téléchargé automatiquement la première fois (~90MB)
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

def is_duplicate(
    new_desc: str,
    new_lat:  float,
    new_lon:  float,
    existing_reports: list,
    text_threshold: float = 0.82,
    geo_threshold_km: float = 0.3
) -> dict:
    """
    Détecte si un signalement est doublon d'un existant.

    Args:
        new_desc          : description du nouveau signalement
        new_lat / new_lon : position GPS du nouveau signalement
        existing_reports  : liste de dicts {id, description, lat, lon}
        text_threshold    : seuil similarité cosine texte (0 à 1)
        geo_threshold_km  : rayon géographique en km (0.3 = 300m)

    Returns:
        {"is_duplicate": False}
        ou
        {"is_duplicate": True, "similar_report_id": X,
         "text_similarity": 0.91, "distance_km": 0.12}
    """
    if not existing_reports:
        return {"is_duplicate": False}

    from sklearn.metrics.pairwise import cosine_similarity

    embedder = get_embedder()

    # Embeddings texte
    new_embedding       = embedder.encode([new_desc])
    existing_texts      = [r['description'] for r in existing_reports]
    existing_embeddings = embedder.encode(existing_texts)

    similarities = cosine_similarity(new_embedding, existing_embeddings)[0]

    for i, report in enumerate(existing_reports):
        distance_km = haversine_km(new_lat, new_lon, report['lat'], report['lon'])
        text_sim    = float(similarities[i])

        # Doublon = proche géographiquement ET texte similaire
        if distance_km <= geo_threshold_km and text_sim >= text_threshold:
            return {
                "is_duplicate":      True,
                "similar_report_id": report['id'],
                "text_similarity":   round(text_sim, 3),
                "distance_km":       round(distance_km, 3)
            }

    return {"is_duplicate": False}
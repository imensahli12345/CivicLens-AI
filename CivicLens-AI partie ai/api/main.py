# api/main.py
from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List, Optional
from PIL import Image
import torch
import torch.nn as nn
import io
import pickle
import numpy as np
from torchvision import transforms, models

import re

from utils.priority_calculator import calculate_priority
from utils.zone_analyzer import get_zone_score
from utils.duplicate_detector import is_duplicate

# ── Application ───────────────────────────────────────────────────
app = FastAPI(
    title="CivicLens AI Service",
    description="Microservice IA pour l'analyse des signalements urbains",
    version="9.1"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:4200", "http://localhost:3000"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ── Configuration ─────────────────────────────────────────────────
CONFIDENCE_THRESHOLD = 0.50
MAX_IMAGES           = 3

# ══════════════════════════════════════════════════════════════════
# SOLUTION 2 — Mots d'exagération
# ══════════════════════════════════════════════════════════════════
EXAGGERATION_WORDS = [
    "catastrophe", "apocalypse", "désastre total",
    "scandaleux", "honte", "inacceptable", "inadmissible",
    "500 familles", "1000 personnes", "tout le quartier",
    "toute la ville", "des milliers", "toute la population",
    "urgence absolue", "urgence totale", "urgence critique",
    "extrêmement dangereux", "mortellement dangereux",
    "danger de mort immédiat", "risque mortel",
    "depuis des années", "depuis toujours", "jamais réparé",
    "personne ne fait rien", "complètement abandonné",
    "effondrement total", "complètement effondré",
    "totalement détruit", "complètement cassé",
]

def detect_exaggeration(description: str) -> dict:
    description_lower = description.lower()
    found = [w for w in EXAGGERATION_WORDS if w in description_lower]
    if len(found) == 0:   level = "none"
    elif len(found) == 1: level = "low"
    elif len(found) == 2: level = "medium"
    else:                 level = "high"
    return {
        "exaggeration_count": len(found),
        "exaggeration_words": found,
        "exaggeration_level": level
    }

def apply_exaggeration_penalty(gravity: str, exag_info: dict) -> tuple:
    GRAVITY_LEVELS = ["FAIBLE", "MOYENNE", "ELEVEE", "CRITIQUE"]
    idx   = GRAVITY_LEVELS.index(gravity)
    level = exag_info["exaggeration_level"]
    if level in ("none", "low"):
        return gravity, None
    elif level == "medium" and idx > 0:
        new_gravity = GRAVITY_LEVELS[idx - 1]
        return new_gravity, (f"Description potentiellement exagérée "
                             f"({exag_info['exaggeration_count']} indicateurs) — "
                             f"gravité ajustée: {gravity}→{new_gravity}")
    elif level == "high" and idx > 0:
        steps       = min(2, idx)
        new_gravity = GRAVITY_LEVELS[idx - steps]
        return new_gravity, (f"Description très exagérée "
                             f"({', '.join(exag_info['exaggeration_words'][:3])}) — "
                             f"gravité ajustée: {gravity}→{new_gravity}")
    return gravity, None


# ══════════════════════════════════════════════════════════════════
# SOLUTION 1 — Cohérence texte / image
# ══════════════════════════════════════════════════════════════════
def check_text_image_coherence(text_gravity: str, visual_score: float) -> dict:
    GRAVITY_LEVELS = ["FAIBLE", "MOYENNE", "ELEVEE", "CRITIQUE"]
    idx = GRAVITY_LEVELS.index(text_gravity)
    if visual_score < 0.20:
        if idx >= 2:
            return {
                "coherent":         False,
                "adjusted_gravity": GRAVITY_LEVELS[idx - 1],
                "message": (f"Incohérence texte/image — "
                            f"gravité ajustée: {text_gravity}→{GRAVITY_LEVELS[idx-1]} "
                            f"(image montre peu de dommages, score={visual_score})")
            }
    if visual_score >= 0.45 and idx == 0:
        return {
            "coherent":         True,
            "adjusted_gravity": "MOYENNE",
            "message":          f"Image confirme dommage réel → gravité: FAIBLE→MOYENNE"
        }
    return {"coherent": True, "adjusted_gravity": text_gravity, "message": None}


# ══════════════════════════════════════════════════════════════════
# MULTI-IMAGE — Moyenne des probabilités (Stratégie 2)
# ══════════════════════════════════════════════════════════════════
def analyze_multiple_images(images_pil: list) -> dict:
    """
    Analyse plusieurs images — Option A (consensus filtering).

    Logique :
      1. Chaque image vote pour une classe
      2. On détermine la classe majoritaire
      3. On calcule la moyenne des confidences UNIQUEMENT
         sur les images qui ont voté pour la classe majoritaire
      4. Les images discordantes sont exclues du calcul final

    Exemple :
      Image 1 : AUTRE  0.985  <- discordante -> exclue
      Image 2 : ROUTE  0.834  <- cohérente   -> incluse
      Image 3 : ROUTE  0.885  <- cohérente   -> incluse
      Confidence finale = (0.834 + 0.885) / 2 = 0.860  OK
    """
    all_probs = []
    per_image = []

    for i, img in enumerate(images_pil):
        tensor = transform(img).unsqueeze(0)
        with torch.no_grad():
            outputs = classifier(tensor)
            probs   = torch.softmax(outputs, dim=1)[0].numpy()

        predicted_idx   = int(np.argmax(probs))
        predicted_class = CLASS_NAMES[predicted_idx]
        predicted_conf  = float(probs[predicted_idx])

        all_probs.append(probs)
        per_image.append({
            "image_index": i + 1,
            "domain":      predicted_class,
            "confidence":  round(predicted_conf, 3),
            "consistent":  None  # sera mis a jour apres
        })

    # 1. Determiner la classe majoritaire
    votes           = [p["domain"] for p in per_image]
    majority        = max(set(votes), key=votes.count)
    agreements      = votes.count(majority)
    consensus_score = round(agreements / len(images_pil), 2)

    # 2. Marquer les images coherentes / discordantes
    for p in per_image:
        p["consistent"] = (p["domain"] == majority)

    # 3. Moyenne UNIQUEMENT sur les images coherentes
    consistent_probs = [
        all_probs[i] for i, p in enumerate(per_image)
        if p["consistent"]
    ]
    avg_probs    = np.mean(consistent_probs, axis=0)
    final_idx    = int(np.argmax(avg_probs))
    final_domain = CLASS_NAMES[final_idx]
    final_conf   = float(avg_probs[final_idx])

    avg_probs_dict = {
        CLASS_NAMES[i]: round(float(avg_probs[i]), 3)
        for i in range(len(CLASS_NAMES))
    }

    return {
        "domain":            final_domain,
        "confidence":        round(final_conf, 3),
        "images_analyzed":   len(images_pil),
        "images_consistent": agreements,
        "images_discarded":  len(images_pil) - agreements,
        "consensus_score":   consensus_score,
        "per_image":         per_image,
        "avg_probs":         avg_probs_dict,
    }


# ── Mots clés par domaine (fallback sans image) ───────────────────
DOMAIN_KEYWORDS = {
    "EAU": [
        "eau", "potable", "robinet", "fuite", "coupure eau",
        "pas d'eau", "manque eau", "réseau eau", "tuyau",
        "canalisation", "assainissement", "citerne"
    ],
    "TRANSPORT": [
        "bus", "transport", "taxi", "arrêt", "ligne",
        "horaire", "retard", "tram", "métro", "véhicule",
        "itinéraire", "pas de bus", "absence bus", "service bus"
    ],
    "ECLAIRAGE": [
        "lumière", "lampadaire", "éclairage", "électricité",
        "coupure électrique", "noir", "obscurité", "lampe",
        "ampoule", "panne électrique"
    ],
    "ROUTE": [
        "route", "trou", "nid", "chaussée", "asphalte",
        "trottoir", "bitume", "rue", "avenue", "pont", "dos d'âne"
    ],
    "INFRASTRUCTURE": [
        "bâtiment", "mur", "fissure", "école", "hôpital",
        "infrastructure", "construction", "effondrement", "toiture"
    ],
    "DECHETS": [
        "déchet", "ordure", "poubelle", "décharge", "sauvage",
        "détritus", "saleté", "encombrant", "rats", "sanitaire"
    ],
    "AUTRE": []
}

def classify_domain_from_text(description: str) -> tuple:
    description_lower = description.lower()
    scores = {domain: 0 for domain in DOMAIN_KEYWORDS}
    for domain, keywords in DOMAIN_KEYWORDS.items():
        for keyword in keywords:
            if keyword in description_lower:
                scores[domain] += 1
    best_domain = max(
        {k: v for k, v in scores.items() if k != "AUTRE"},
        key=scores.get, default="AUTRE"
    )
    best_score = scores[best_domain]
    if best_score == 0:
        return ("AUTRE", 0.50)
    return (best_domain, round(min(0.50 + best_score * 0.10, 0.90), 2))


# ── Features visuelles ────────────────────────────────────────────
def extract_visual_features(img: Image.Image) -> dict:
    img_gray = np.array(img.convert("L").resize((224, 224)), dtype=np.float32)
    h, w     = img_gray.shape
    cx1, cx2 = int(w * 0.30), int(w * 0.70)
    cy1, cy2 = int(h * 0.30), int(h * 0.70)
    center   = img_gray[cy1:cy2, cx1:cx2]
    border   = np.concatenate([
        img_gray[:cy1, :].flatten(), img_gray[cy2:, :].flatten(),
        img_gray[:, :cx1].flatten(), img_gray[:, cx2:].flatten(),
    ])
    brightness_diff = abs(float(np.mean(center)) - float(np.mean(border))) / 128.0
    texture_diff    = abs(float(np.std(center))  - float(np.std(border)))  / 64.0
    deep_damage     = max(0.0, float(np.mean(center < 50)) - float(np.mean(border < 50)))
    adjustment = 0.0
    if brightness_diff > 0.20: adjustment += brightness_diff * 0.5
    if texture_diff    > 0.15: adjustment += texture_diff    * 0.4
    if deep_damage     > 0.10: adjustment += deep_damage     * 0.8
    adjustment = max(-1.0, min(1.0, adjustment))
    return {
        "visual_adjustment": round(adjustment, 3),
        "brightness_diff":   round(brightness_diff, 3),
        "texture_diff":      round(texture_diff, 3),
        "deep_damage":       round(deep_damage, 3),
    }

def combine_gravity(text_gravity: str, visual_features: dict) -> tuple:
    GRAVITY_LEVELS = ["FAIBLE", "MOYENNE", "ELEVEE", "CRITIQUE"]
    idx        = GRAVITY_LEVELS.index(text_gravity)
    adjustment = visual_features["visual_adjustment"]
    if adjustment >= 0.45 and idx < 3:
        fg = GRAVITY_LEVELS[idx + 1]
        return fg, f"image+text (augmenté: {text_gravity}→{fg}, score={adjustment})"
    elif adjustment <= -0.40 and idx > 0:
        fg = GRAVITY_LEVELS[idx - 1]
        return fg, f"image+text (diminué: {text_gravity}→{fg}, score={adjustment})"
    return text_gravity, f"image+text (confirmé, score={adjustment})"


# ── Charger les modèles au démarrage ─────────────────────────────
print("[CivicLens AI] Chargement des modèles...")

try:
    checkpoint  = torch.load("models/classifier_best.pth", map_location="cpu")
    CLASS_NAMES = checkpoint['class_names']
    NUM_CLASSES = len(CLASS_NAMES)
    classifier  = models.mobilenet_v2(weights=None)
    classifier.classifier = nn.Sequential(
        nn.Dropout(p=0.3),
        nn.Linear(classifier.last_channel, 256),
        nn.ReLU(),
        nn.Dropout(p=0.2),
        nn.Linear(256, NUM_CLASSES)
    )
    classifier.load_state_dict(checkpoint['model_state'])
    classifier.eval()
    print(f"[OK] Classificateur domaine — classes : {CLASS_NAMES}")
    CLASSIFIER_READY = True
except Exception as e:
    print(f"[ATTENTION] Classificateur non disponible : {e}")
    CLASS_NAMES      = []
    CLASSIFIER_READY = False

try:
    gravity_model = pickle.load(open("models/gravity_model.pkl", "rb"))
    try:
        embedder_name = pickle.load(open("models/gravity_embedder_name.pkl", "rb"))
    except Exception:
        embedder_name = "intfloat/multilingual-e5-base"
    print(f"[Gravité] Chargement embedder : {embedder_name}...")
    from sentence_transformers import SentenceTransformer
    gravity_embedder = SentenceTransformer(embedder_name)
    print(f"[OK] Modèle gravité multilingue chargé ({embedder_name})")
    print(f"     Langues supportées : français + arabe + tunisien")
    GRAVITY_READY = True
except Exception as e:
    print(f"[ATTENTION] Modèle gravité non disponible : {e}")
    gravity_embedder = None
    GRAVITY_READY = False

transform = transforms.Compose([
    transforms.Resize((224, 224)),
    transforms.ToTensor(),
    transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
])

GRAVITY_MAP = {0: "FAIBLE", 1: "MOYENNE", 2: "ELEVEE", 3: "CRITIQUE"}


# ══════════════════════════════════════════════════════════════════
# DÉTECTION DE LANGUE — sans librairie externe
# ══════════════════════════════════════════════════════════════════
# Règles basées sur les caractères Unicode :
#   - Arabe standard   : blocs Unicode 0600–06FF (ex: ا ب ت ...)
#   - Tunisien dialectal : chiffres arabes latins 3/7/5/9 + mots clés TN
#   - Français         : tout le reste (latin + accents)

TN_KEYWORDS = [
    "treek","7ouma","3arbiyyet","maqta3","7ofra","maqtou3",
    "3imara","infajar","wa9a3","blessa","jerou7","sbitar",
    "iyyem","khatra","mish","chwaya","bezzef","9wiya",
    "ykhrouj","mkesser","3ajel","yji","ykhwi","fi5",
    "ta7t","fau9","sout7","3aal","3and","fil","lil","mil",
    "il gaz","il ma","il bus","il treek","il 7ouma",
]

def detect_language(text: str) -> dict:
    """
    Détecte la langue d'un texte : 'ar' / 'tn' / 'fr'

    Retourne :
      {
        "language":    "ar" | "tn" | "fr",
        "language_label": "Arabe" | "Tunisien" | "Français",
        "arabic_ratio":   0.0 → 1.0,
        "tn_score":       int (nb mots-clés tunisiens trouvés)
      }
    """
    if not text or not text.strip():
        return {"language": "fr", "language_label": "Français",
                "arabic_ratio": 0.0, "tn_score": 0}

    text_lower = text.lower()

    # ── 1. Ratio caractères arabes ────────────────────────────────
    arabic_chars = len(re.findall(r'[\u0600-\u06FF]', text))
    total_alpha  = len(re.findall(r'[^\s\d\W]', text))
    arabic_ratio = arabic_chars / total_alpha if total_alpha > 0 else 0.0

    # ── 2. Score tunisien (mots-clés dialectaux) ──────────────────
    tn_score = sum(1 for kw in TN_KEYWORDS if kw in text_lower)

    # ── 3. Décision ───────────────────────────────────────────────
    if arabic_ratio >= 0.4:
        # Majorité de caractères arabes → arabe standard
        lang = "ar"
        label = "Arabe"
    elif tn_score >= 2 or (
        tn_score >= 1 and
        any(c.isdigit() and c in "3579" for c in text) and
        arabic_ratio < 0.1
    ):
        # Mots-clés tunisiens + chiffres arabes latinos → tunisien
        lang = "tn"
        label = "Tunisien"
    else:
        lang = "fr"
        label = "Français"

    return {
        "language":       lang,
        "language_label": label,
        "arabic_ratio":   round(arabic_ratio, 2),
        "tn_score":       tn_score
    }
print("[CivicLens AI] Service prêt !\n")


# ══════════════════════════════════════════════════════════════════
# ENDPOINT PRINCIPAL — Analyser un signalement
# 3 champs image séparés → Swagger affiche 3 boutons upload
# ══════════════════════════════════════════════════════════════════
@app.post("/analyze")
async def analyze_report(
    description: str                   = Form(...),
    latitude:    float                 = Form(...),
    longitude:   float                 = Form(...),
    image1:      Optional[UploadFile]  = File(None),
    image2:      Optional[UploadFile]  = File(None),
    image3:      Optional[UploadFile]  = File(None),
):
    """
    Analyse un signalement urbain.
    Accepte jusqu'à 3 images via image1, image2, image3.

    Fonctionnalités :
      - Multi-image ensemble averaging (moyenne des probabilités)
      - Confidence check < 50% → warning
      - Solution 1 : cohérence texte/image
      - Solution 2 : détection mots exagération
    """

    # ── Lire les images fournies ──────────────────────────────────
    images_pil = []
    for img_file in [image1, image2, image3]:
        if img_file and img_file.filename != "":
            try:
                img_bytes = await img_file.read()
                img_pil   = Image.open(io.BytesIO(img_bytes)).convert("RGB")
                images_pil.append(img_pil)
            except Exception as e:
                raise HTTPException(400, f"Erreur lecture image : {e}")

    has_image      = len(images_pil) > 0
    multi_result   = None
    domain         = None
    confidence     = 0.0
    low_confidence = False
    visual_features= None
    gravity_source = "text"
    warnings       = []

    # ── 1. Classifier le domaine ──────────────────────────────────
    if has_image and CLASSIFIER_READY:

        if len(images_pil) == 1:
            # Une seule image → analyse classique
            tensor = transform(images_pil[0]).unsqueeze(0)
            with torch.no_grad():
                outputs = classifier(tensor)
                probs   = torch.softmax(outputs, dim=1)[0]
                idx     = probs.argmax().item()
            domain     = CLASS_NAMES[idx]
            confidence = round(float(probs[idx]), 3)

        else:
            # 2 ou 3 images → moyenne des probabilités (Stratégie 2)
            multi_result = analyze_multiple_images(images_pil)
            domain       = multi_result["domain"]
            confidence   = multi_result["confidence"]

            # Avertir si les images ne sont pas d'accord
            if multi_result["consensus_score"] < 0.5:
                warnings.append(
                    f"Consensus faible entre les {len(images_pil)} images "
                    f"(score={multi_result['consensus_score']}) — "
                    "les images semblent montrer des problèmes différents"
                )

        # Vérification confiance minimale
        if confidence < CONFIDENCE_THRESHOLD:
            low_confidence = True
            warnings.append(
                f"Domaine incertain (confiance {confidence*100:.0f}%) — "
                "veuillez préciser la description ou prendre une meilleure photo"
            )
    else:
        # Pas d'image → classification par mots clés texte
        domain, confidence = classify_domain_from_text(description)

    # ── 2. Gravité depuis le texte (base) ────────────────────────
    # Multilingue : français + arabe standard + tunisien dialectal
    gravity_confidence = None
    if GRAVITY_READY:
        desc_vec       = gravity_embedder.encode(
                             ["query: " + description],
                             convert_to_numpy=True
                         )
        gravity_idx    = gravity_model.predict(desc_vec)[0]
        gravity_probs  = gravity_model.predict_proba(desc_vec)[0]
        gravity_confidence = round(float(gravity_probs.max()), 3)
        text_gravity   = GRAVITY_MAP[int(gravity_idx)]
    else:
        text_gravity = "MOYENNE"

    gravity = text_gravity

    # ── 3. SOLUTION 2 — Anti-exagération (toujours active) ────────
    exag_info = detect_exaggeration(description)
    if exag_info["exaggeration_level"] != "none":
        gravity, exag_msg = apply_exaggeration_penalty(gravity, exag_info)
        if exag_msg:
            warnings.append(exag_msg)
            gravity_source += " + anti-exaggeration"

    # ── 4. SOLUTION 1 — Cohérence texte/image ────────────────────
    coherence_info = None
    if has_image and images_pil:
        visual_features = extract_visual_features(images_pil[0])
        coherence_info  = check_text_image_coherence(
            gravity, visual_features["visual_adjustment"]
        )
        if not coherence_info["coherent"] and coherence_info["message"]:
            warnings.append(coherence_info["message"])
            gravity        = coherence_info["adjusted_gravity"]
            gravity_source += " + image-coherence-check"
        else:
            gravity, src   = combine_gravity(gravity, visual_features)
            gravity_source += f" + {src}"

    # ── 5. Zone géographique ──────────────────────────────────────
    zone_info = get_zone_score(latitude, longitude)

    # ── 6. Détection de langue ────────────────────────────────────
    lang_info = detect_language(description)

    # ── 7. Priorité finale ────────────────────────────────────────
    priority_result = calculate_priority(
        gravity    = gravity,
        vote_count = 0,
        domain     = domain,
        zone_score = zone_info["zone_score"]
    )

    # ── Réponse ───────────────────────────────────────────────────
    response = {
        "domain":             domain,
        "confidence":         confidence,
        "low_confidence":     low_confidence,
        "gravity":            gravity,
        "gravity_confidence": gravity_confidence,
        "gravity_source":     gravity_source,
        "language_detected":  lang_info["language"],
        "language_label":     lang_info["language_label"],
        "priority":           priority_result["priority"],
        "score":              priority_result["score"],
        "address":            zone_info.get("address", {}),
        "zone_type":          zone_info["zone_type"],
        "zone_score":         zone_info["zone_score"],
        "zone_details":       zone_info["zone_details"],
        "traffic_score":      zone_info.get("traffic_score", 0.0),
        "traffic_details":    zone_info.get("traffic_details", []),
        "has_image":          has_image,
        "images_count":       len(images_pil),
        "warnings":           warnings,
        "manipulation_flags": {
            "exaggeration_detected": exag_info["exaggeration_level"] != "none",
            "exaggeration_level":    exag_info["exaggeration_level"],
            "exaggeration_words":    exag_info["exaggeration_words"],
            "image_text_coherent":   coherence_info["coherent"] if coherence_info else None,
        }
    }

    if multi_result:
        response["multi_image_analysis"] = {
            "images_analyzed":   multi_result["images_analyzed"],
            "images_consistent": multi_result["images_consistent"],
            "images_discarded":  multi_result["images_discarded"],
            "consensus_score":   multi_result["consensus_score"],
            "per_image":         multi_result["per_image"],
            "avg_probs":         multi_result["avg_probs"],
        }

    if visual_features:
        response["visual_features"] = visual_features

    return response


# ══════════════════════════════════════════════════════════════════
# ENDPOINT — Détecter les doublons
# ══════════════════════════════════════════════════════════════════
class ExistingReport(BaseModel):
    id:          int
    description: str
    lat:         float
    lon:         float
    vote_count:  int = 0

class DuplicateRequest(BaseModel):
    description:      str
    latitude:         float
    longitude:        float
    domain:           str = "AUTRE"
    existing_reports: List[ExistingReport] = []

@app.post("/check-duplicate")
async def check_duplicate(request: DuplicateRequest):
    """
    Seuil géographique adaptatif selon le domaine.
    Si doublon → regroupe + incrémente votes automatiquement.
    """
    reports_list = [r.dict() for r in request.existing_reports]
    result = is_duplicate(
        new_desc         = request.description,
        new_lat          = request.latitude,
        new_lon          = request.longitude,
        existing_reports = reports_list,
        domain           = request.domain,
    )
    return result


# ══════════════════════════════════════════════════════════════════
# ENDPOINT — Mettre à jour la priorité après un vote
# ══════════════════════════════════════════════════════════════════
class UpdatePriorityRequest(BaseModel):
    gravity:    str
    vote_count: int
    domain:     str
    latitude:   float
    longitude:  float

@app.post("/update-priority")
async def update_priority(request: UpdatePriorityRequest):
    """Recalcule la priorité après un vote citoyen."""
    zone_info = get_zone_score(request.latitude, request.longitude)
    result    = calculate_priority(
        gravity    = request.gravity,
        vote_count = request.vote_count,
        domain     = request.domain,
        zone_score = zone_info["zone_score"]
    )
    return {
        "priority":   result["priority"],
        "score":      result["score"],
        "address":    zone_info.get("region", {}),
        "zone_score": zone_info["zone_score"]
    }


# ══════════════════════════════════════════════════════════════════
# ENDPOINT — Health check
# ══════════════════════════════════════════════════════════════════

# ══════════════════════════════════════════════════════════════════
# ENDPOINT — Expliquer la priorité en français
# ══════════════════════════════════════════════════════════════════
class ExplainRequest(BaseModel):
    gravity:      str
    priority:     str
    score:        float
    vote_count:   int
    domain:       str
    zone_type:    str
    zone_score:   float
    zone_details: list = []
    has_image:    bool = False
    images_count: int  = 0
    gravity_source: str = ""
    manipulation_flags: dict = {}

@app.post("/explain")
async def explain_priority(request: ExplainRequest):
    """
    Génère une explication en français de la priorité calculée.
    Exemple : "URGENTE car : gravité ELEVEE + école à proximité + 8 votes citoyens"
    """
    reasons   = []
    details   = []

    # ── 1. Gravité ────────────────────────────────────────────────
    gravity_fr = {
        "FAIBLE":   "gravité FAIBLE (problème mineur)",
        "MOYENNE":  "gravité MOYENNE (problème modéré)",
        "ELEVEE":   "gravité ÉLEVÉE (problème sérieux)",
        "CRITIQUE": "gravité CRITIQUE (problème grave)"
    }
    reasons.append(gravity_fr.get(request.gravity, f"gravité {request.gravity}"))

    # ── 2. Zone géographique ─────────────────────────────────────
    if request.zone_score >= 1.8:
        reasons.append("zone très fréquentée (score élevé)")
    elif request.zone_score >= 1.4:
        reasons.append("zone fréquentée")

    # Détails des POI proches
    for detail in request.zone_details[:3]:
        if any(k in detail.lower() for k in ["école", "hôpital", "clinique", "université", "maternelle"]):
            reasons.append(f"{detail} à proximité")
        elif "arrêt bus" in detail.lower():
            details.append(f"{detail} à proximité")
        elif "axe routier principal" in detail.lower():
            details.append("axe routier principal")

    # ── 3. Votes citoyens ─────────────────────────────────────────
    if request.vote_count >= 10:
        reasons.append(f"{request.vote_count} votes citoyens (très signalé)")
    elif request.vote_count >= 5:
        reasons.append(f"{request.vote_count} votes citoyens")
    elif request.vote_count >= 1:
        details.append(f"{request.vote_count} vote(s) citoyen(s)")

    # ── 4. Domaine ────────────────────────────────────────────────
    domain_fr = {
        "EAU":            "réseau eau (impact sanitaire élevé)",
        "ROUTE":          "voirie (impact mobilité)",
        "ECLAIRAGE":      "éclairage public",
        "INFRASTRUCTURE": "infrastructure publique",
        "DECHETS":        "gestion des déchets",
        "TRANSPORT":      "transport public",
        "AUTRE":          "autre problème urbain"
    }
    details.append(f"domaine : {domain_fr.get(request.domain, request.domain)}")

    # ── 5. Image analysée ─────────────────────────────────────────
    if request.has_image and request.images_count > 1:
        details.append(f"{request.images_count} photos analysées (consensus visuel)")
    elif request.has_image:
        details.append("photo analysée")

    # ── 6. Anti-manipulation ──────────────────────────────────────
    flags = request.manipulation_flags
    if flags.get("exaggeration_detected"):
        details.append(f"description ajustée (exagération détectée : niveau {flags.get('exaggeration_level', '')})")
    if flags.get("image_text_coherent") is False:
        details.append("incohérence texte/image corrigée")

    # ── Construction de la phrase d'explication ───────────────────
    priority_fr = {
        "URGENTE": "URGENTE",
        "HAUTE":   "HAUTE",
        "NORMALE": "NORMALE",
        "BASSE":   "BASSE"
    }

    main_reasons = " + ".join(reasons) if reasons else "paramètres standards"
    explanation  = f"Priorité {priority_fr.get(request.priority, request.priority)} car : {main_reasons}"

    # Phrase détaillée
    all_factors = reasons + details
    detailed    = (
        f"Ce signalement a reçu une priorité {request.priority} "
        f"(score={request.score}) en raison de : {', '.join(all_factors)}."
    )

    # ── Recommandation pour les agents municipaux ─────────────────
    recommendations = {
        "URGENTE": "Intervention requise dans les 24h. Mobiliser une équipe en urgence.",
        "HAUTE":   "Planifier une intervention dans la semaine.",
        "NORMALE": "Planifier lors de la prochaine tournée d'entretien.",
        "BASSE":   "Inscrire au planning d'entretien courant."
    }

    return {
        "explanation":      explanation,
        "detailed":         detailed,
        "reasons":          reasons,
        "details":          details,
        "recommendation":   recommendations.get(request.priority, ""),
        "score_breakdown": {
            "gravity_weight": request.gravity,
            "zone_score":     request.zone_score,
            "vote_count":     request.vote_count,
            "domain":         request.domain,
            "final_score":    request.score
        }
    }


# ══════════════════════════════════════════════════════════════════
# ENDPOINT — Statistiques globales des signalements
# ══════════════════════════════════════════════════════════════════
class StatsRequest(BaseModel):
    reports: List[dict]

@app.post("/stats")
async def get_stats(request: StatsRequest):
    """
    Calcule les statistiques globales d'une liste de signalements.

    Chaque rapport doit contenir :
      - domain   : ROUTE / EAU / ECLAIRAGE / DECHETS / TRANSPORT / INFRASTRUCTURE / AUTRE
      - gravity  : FAIBLE / MOYENNE / ELEVEE / CRITIQUE
      - priority : BASSE / NORMALE / HAUTE / URGENTE
      - latitude / longitude (optionnel — pour zones critiques)

    Retourne :
      - total, par_domaine, par_gravite, par_priorite
      - taux_urgence  : % signalements URGENTS
      - zones_critiques : coordonnées les plus signalées
      - domaine_dominant : domaine avec le plus de signalements
      - gravite_moyenne  : score moyen de gravité (1=FAIBLE→4=CRITIQUE)
    """
    reports = request.reports
    total   = len(reports)

    if total == 0:
        return {
            "total":            0,
            "par_domaine":      {},
            "par_gravite":      {},
            "par_priorite":     {},
            "taux_urgence":     0.0,
            "domaine_dominant": None,
            "gravite_moyenne":  0.0,
            "zones_critiques":  [],
            "message":          "Aucun signalement fourni"
        }

    DOMAINES   = ["ROUTE","EAU","ECLAIRAGE","DECHETS","TRANSPORT","INFRASTRUCTURE","AUTRE"]
    GRAVITES   = ["FAIBLE","MOYENNE","ELEVEE","CRITIQUE"]
    PRIORITES  = ["BASSE","NORMALE","HAUTE","URGENTE"]
    GRAV_SCORE = {"FAIBLE": 1, "MOYENNE": 2, "ELEVEE": 3, "CRITIQUE": 4}

    # ── Comptages ─────────────────────────────────────────────────
    par_domaine  = {d: 0 for d in DOMAINES}
    par_gravite  = {g: 0 for g in GRAVITES}
    par_priorite = {p: 0 for p in PRIORITES}
    zone_counter = {}
    grav_total   = 0

    for r in reports:
        dom  = str(r.get("domain",   "AUTRE")).upper()
        grav = str(r.get("gravity",  "FAIBLE")).upper()
        prio = str(r.get("priority", "BASSE")).upper()

        if dom  in par_domaine:  par_domaine[dom]   += 1
        if grav in par_gravite:  par_gravite[grav]  += 1
        if prio in par_priorite: par_priorite[prio] += 1

        grav_total += GRAV_SCORE.get(grav, 1)

        # Agréger par zone (arrondi à 2 décimales ≈ 1km)
        lat = r.get("latitude")
        lon = r.get("longitude")
        if lat is not None and lon is not None:
            zone_key = f"{round(float(lat), 2)},{round(float(lon), 2)}"
            zone_counter[zone_key] = zone_counter.get(zone_key, 0) + 1

    # ── Calculs dérivés ───────────────────────────────────────────
    taux_urgence    = round(par_priorite["URGENTE"] / total * 100, 1)
    gravite_moyenne = round(grav_total / total, 2)
    domaine_dominant = max(par_domaine, key=par_domaine.get) if par_domaine else None

    # Top 5 zones les plus signalées
    zones_critiques = sorted(
        [{"zone": k, "count": v} for k, v in zone_counter.items()],
        key=lambda x: x["count"],
        reverse=True
    )[:5]

    # ── Résumé lisible ────────────────────────────────────────────
    nb_urgents   = par_priorite["URGENTE"]
    nb_critiques = par_gravite["CRITIQUE"]
    resume = (
        f"{total} signalements : "
        f"{nb_urgents} urgents ({taux_urgence}%), "
        f"{nb_critiques} de gravite CRITIQUE, "
        f"domaine dominant : {domaine_dominant}"
    )

    return {
        "total":             total,
        "par_domaine":       par_domaine,
        "par_gravite":       par_gravite,
        "par_priorite":      par_priorite,
        "taux_urgence":      taux_urgence,
        "domaine_dominant":  domaine_dominant,
        "gravite_moyenne":   gravite_moyenne,
        "zones_critiques":   zones_critiques,
        "resume":            resume
    }


# ══════════════════════════════════════════════════════════════════
# ENDPOINT — Détecter la langue d'un texte
# ══════════════════════════════════════════════════════════════════
class LangRequest(BaseModel):
    text: str

@app.post("/detect-language")
async def detect_lang(request: LangRequest):
    """
    Détecte la langue d'un texte : Français / Arabe / Tunisien.
    Aucun modèle IA requis — basé sur caractères Unicode + mots-clés.

    Exemples :
      "حفرة عميقة في الطريق"                    → ar (Arabe)
      "fi treek 7ofra kbira w khatra"           → tn (Tunisien)
      "Nid de poule dangereux sur la route"     → fr (Français)
    """
    result = detect_language(request.text)
    return {
        "text":           request.text[:80],
        "language":       result["language"],
        "language_label": result["language_label"],
        "arabic_ratio":   result["arabic_ratio"],
        "tn_score":       result["tn_score"],
        "details": {
            "ar": "Arabe standard (caractères Unicode 0600-06FF)",
            "tn": "Tunisien dialectal (chiffres arabes latins + mots-clés)",
            "fr": "Français (latin)"
        }[result["language"]]
    }


@app.get("/health")
def health():
    return {
        "status":               "ok",
        "version":              "9.3",
        "classifier_ready":     CLASSIFIER_READY,
        "gravity_ready":        GRAVITY_READY,
        "classes":              CLASS_NAMES,
        "confidence_threshold": CONFIDENCE_THRESHOLD,
        "max_images":           MAX_IMAGES,
        "features": [
            "multi-image ensemble averaging (image1/image2/image3)",
            "consensus score between images",
            "confidence check (<50% → warning)",
            "solution1: image/text coherence check",
            "solution2: exaggeration words detection",
            "combined anti-manipulation protection",
            "adaptive geo thresholds per domain",
            "duplicate grouping + auto vote increment",
            "/explain endpoint — explication priorite en francais",
            "zone cache — Overpass API appelée une seule fois par zone",
            "/stats endpoint — statistiques globales des signalements",
            "gravity_confidence — score de confiance de la gravite",
            "language_detected — detection automatique FR / AR / TN",
            "/detect-language endpoint — detection langue standalone"
        ]
    }

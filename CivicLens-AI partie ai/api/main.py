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
from torchvision import transforms, models

from utils.priority_calculator import calculate_priority
from utils.zone_analyzer import get_zone_score
from utils.duplicate_detector import is_duplicate

# ── Application ───────────────────────────────────────────────────
app = FastAPI(
    title="CivicLens AI Service",
    description="Microservice IA pour l'analyse des signalements urbains",
    version="2.0"
)

# CORS → Angular peut appeler FastAPI
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:4200", "http://localhost:3000"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ── Domaines sans image (classification par texte uniquement) ─────
TEXT_ONLY_DOMAINS = ["TRANSPORT", "EAU_POTABLE"]

# Mots clés par domaine pour la classification texte
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
    """Classifie le domaine depuis le texte quand pas d'image."""
    description_lower = description.lower()
    scores = {domain: 0 for domain in DOMAIN_KEYWORDS}

    for domain, keywords in DOMAIN_KEYWORDS.items():
        for keyword in keywords:
            if keyword in description_lower:
                scores[domain] += 1

    # Ignorer AUTRE dans la recherche du meilleur
    best_domain = max(
        {k: v for k, v in scores.items() if k != "AUTRE"},
        key=scores.get,
        default="AUTRE"
    )
    best_score = scores[best_domain]

    if best_score == 0:
        return ("AUTRE", 0.50)

    confidence = min(0.50 + best_score * 0.10, 0.90)
    return (best_domain, round(confidence, 2))


# ── Charger les modèles au démarrage ─────────────────────────────
print("[CivicLens AI] Chargement des modèles...")

# Modèle classification domaine (image)
try:
    checkpoint  = torch.load("models/classifier_best.pth", map_location="cpu")
    CLASS_NAMES = checkpoint['class_names']
    NUM_CLASSES = len(CLASS_NAMES)

    classifier = models.mobilenet_v2(pretrained=False)
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
    print("  → Entraîne d'abord le modèle avec : python training/train_classifier.py")
    CLASS_NAMES      = []
    CLASSIFIER_READY = False

# Modèle gravité (texte)
try:
    gravity_model = pickle.load(open("models/gravity_model.pkl",      "rb"))
    gravity_vec   = pickle.load(open("models/gravity_vectorizer.pkl", "rb"))
    print("[OK] Modèle gravité chargé")
    GRAVITY_READY = True
except Exception as e:
    print(f"[ATTENTION] Modèle gravité non disponible : {e}")
    print("  → Entraîne d'abord : python training/train_gravity.py")
    GRAVITY_READY = False

# Transform image
transform = transforms.Compose([
    transforms.Resize((224, 224)),
    transforms.ToTensor(),
    transforms.Normalize([0.485, 0.456, 0.406],
                         [0.229, 0.224, 0.225])
])

GRAVITY_MAP = {0: "FAIBLE", 1: "MOYENNE", 2: "ELEVEE", 3: "CRITIQUE"}

print("[CivicLens AI] Service prêt !\n")


# ══════════════════════════════════════════════════════════════════
# ENDPOINT PRINCIPAL — Analyser un signalement
# ══════════════════════════════════════════════════════════════════
@app.post("/analyze")
async def analyze_report(
    description: str         = Form(...),
    latitude:    float       = Form(...),
    longitude:   float       = Form(...),
    image:       Optional[UploadFile] = File(None)
):
    """
    Analyse un signalement urbain.
    L'image est optionnelle (signalements sans image : transport, eau potable...).
    """
    has_image = image is not None and image.filename != ""
    domain     = None
    confidence = 0.0

    # ── 1. Classifier le domaine ──────────────────────────────────
    if has_image and CLASSIFIER_READY:
        # Cas avec image → MobileNetV2
        try:
            img_bytes = await image.read()
            img       = Image.open(io.BytesIO(img_bytes)).convert("RGB")
            tensor    = transform(img).unsqueeze(0)

            with torch.no_grad():
                outputs = classifier(tensor)
                probs   = torch.softmax(outputs, dim=1)[0]
                idx     = probs.argmax().item()

            domain     = CLASS_NAMES[idx]
            confidence = round(float(probs[idx]), 3)

        except Exception as e:
            raise HTTPException(status_code=400, detail=f"Erreur lecture image : {e}")
    else:
        # Cas sans image → classification par mots clés texte
        domain, confidence = classify_domain_from_text(description)

    # ── 2. Classifier la gravité (toujours depuis le texte) ───────
    if GRAVITY_READY:
        desc_vec    = gravity_vec.transform([description])
        gravity_idx = gravity_model.predict(desc_vec)[0]
        gravity     = GRAVITY_MAP[int(gravity_idx)]
    else:
        # Fallback si modèle non entraîné
        gravity = "MOYENNE"

    # ── 3. Analyser la zone géographique ──────────────────────────
    zone_info = get_zone_score(latitude, longitude)

    # ── 4. Calculer la priorité ───────────────────────────────────
    priority_result = calculate_priority(
        gravity    = gravity,
        vote_count = 0,
        domain     = domain,
        zone_score = zone_info["zone_score"]
    )

    return {
        "domain":       domain,
        "confidence":   confidence,
        "gravity":      gravity,
        "priority":     priority_result["priority"],
        "score":        priority_result["score"],
        "zone_type":    zone_info["zone_type"],
        "zone_score":   zone_info["zone_score"],
        "zone_details": zone_info["zone_details"],
        "has_image":    has_image,
        "source":       "image+text" if has_image else "text_only"
    }


# ══════════════════════════════════════════════════════════════════
# ENDPOINT — Détecter les doublons
# ══════════════════════════════════════════════════════════════════
class ExistingReport(BaseModel):
    id:          int
    description: str
    lat:         float
    lon:         float

class DuplicateRequest(BaseModel):
    description:      str
    latitude:         float
    longitude:        float
    existing_reports: List[ExistingReport] = []

@app.post("/check-duplicate")
async def check_duplicate(request: DuplicateRequest):
    """
    Vérifie si un signalement est doublon d'un existant.
    Compare la similarité textuelle + distance géographique.
    """
    reports_list = [r.dict() for r in request.existing_reports]
    result = is_duplicate(
        request.description,
        request.latitude,
        request.longitude,
        reports_list
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
    """
    Recalcule la priorité d'un signalement après un vote citoyen.
    Appelé par .NET à chaque nouveau vote.
    """
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
        "zone_score": zone_info["zone_score"]
    }


# ══════════════════════════════════════════════════════════════════
# ENDPOINT — Health check
# ══════════════════════════════════════════════════════════════════
@app.get("/health")
def health():
    return {
        "status":           "ok",
        "version":          "2.0",
        "classifier_ready": CLASSIFIER_READY,
        "gravity_ready":    GRAVITY_READY,
        "classes":          CLASS_NAMES
    }
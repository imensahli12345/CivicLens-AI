# test_api.py — Tester le microservice IA sans .NET
# Lance d'abord : uvicorn api.main:app --reload --port 8001

import requests
import json
import os

BASE_URL = "http://localhost:8001"

def separator(title):
    print(f"\n{'='*55}")
    print(f"  {title}")
    print('='*55)

# ── Test 1 : Health check ─────────────────────────────────────────
separator("TEST 1 — Health Check")
r = requests.get(f"{BASE_URL}/health")
print(json.dumps(r.json(), indent=2, ensure_ascii=False))

# ── Test 2 : Signalement AVEC image ──────────────────────────────
separator("TEST 2 — Signalement avec image (ROUTE)")

# Cherche une image dans data/raw/ROUTE/
image_path = None
route_dir  = "data/raw/ROUTE"
if os.path.exists(route_dir):
    for f in os.listdir(route_dir):
        if f.lower().endswith((".jpg", ".jpeg", ".png")):
            image_path = os.path.join(route_dir, f)
            break

if image_path:
    print(f"Image utilisée : {image_path}")
    with open(image_path, "rb") as img:
        r = requests.post(f"{BASE_URL}/analyze",
            files={"image": img},
            data={
                "description": "Grand nid de poule au milieu de la route, très dangereux pour les voitures",
                "latitude":  "36.8065",
                "longitude": "10.1815"
            }
        )
    print("\nRésultat :")
    print(json.dumps(r.json(), indent=2, ensure_ascii=False))
else:
    print("[SKIP] Aucune image trouvée dans data/raw/ROUTE/")

# ── Test 3 : Signalement SANS image (TRANSPORT) ───────────────────
separator("TEST 3 — Signalement sans image (TRANSPORT)")
r = requests.post(f"{BASE_URL}/analyze",
    data={
        "description": "Le bus ligne 15 n'est pas passé depuis 3 jours, des centaines de travailleurs sont bloqués",
        "latitude":  "36.8100",
        "longitude": "10.1900"
    }
)
print(json.dumps(r.json(), indent=2, ensure_ascii=False))

# ── Test 4 : Signalement SANS image (EAU potable) ────────────────
separator("TEST 4 — Signalement sans image (EAU potable)")
r = requests.post(f"{BASE_URL}/analyze",
    data={
        "description": "Le quartier est privé d'eau potable depuis 5 jours, plus de 500 familles affectées",
        "latitude":  "36.8200",
        "longitude": "10.1700"
    }
)
print(json.dumps(r.json(), indent=2, ensure_ascii=False))

# ── Test 5 : Détection de doublons ───────────────────────────────
separator("TEST 5 — Détection de doublons")
existing = [
    {
        "id": 42,
        "description": "Nid de poule dangereux avenue Habib Bourguiba",
        "lat": 36.8065,
        "lon": 10.1815
    }
]
r = requests.post(f"{BASE_URL}/check-duplicate",
    json={
        "description":      "Gros trou dans la route avenue Bourguiba",
        "latitude":         36.8067,
        "longitude":        10.1816,
        "existing_reports": existing
    }
)
print("Même zone, description similaire :")
print(json.dumps(r.json(), indent=2, ensure_ascii=False))

# Test doublon négatif (loin géographiquement)
r = requests.post(f"{BASE_URL}/check-duplicate",
    json={
        "description":      "Nid de poule dangereux",
        "latitude":         36.9000,   # loin
        "longitude":        10.3000,
        "existing_reports": existing
    }
)
print("\nZone différente :")
print(json.dumps(r.json(), indent=2, ensure_ascii=False))

# ── Test 6 : Update priorité après vote ──────────────────────────
separator("TEST 6 — Mise à jour priorité après votes")
r = requests.post(f"{BASE_URL}/update-priority",
    json={
        "gravity":    "ELEVEE",
        "vote_count": 25,
        "domain":     "ROUTE",
        "latitude":   36.8065,
        "longitude":  10.1815
    }
)
print(json.dumps(r.json(), indent=2, ensure_ascii=False))

print(f"\n{'='*55}")
print("  ✅ Tous les tests terminés !")
print(f"{'='*55}\n")
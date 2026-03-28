# test_api.py — Tests complets multilingues (FR + AR + TN) avec et sans image
# Lance d'abord : uvicorn api.main:app --reload --port 8001

import requests
import json
import os

BASE_URL = "http://localhost:8001"

passed = 0
failed = 0
total  = 0

def separator(title):
    print(f"\n{'='*60}")
    print(f"  {title}")
    print('='*60)

def check(label, condition, got=None):
    global passed, failed, total
    total += 1
    if condition:
        passed += 1
        print(f"  OK  {label}")
    else:
        failed += 1
        print(f"  NOK {label}  (obtenu: {got})")

def print_gravity(r_json):
    g    = r_json.get("gravity", "?")
    conf = r_json.get("gravity_confidence")
    lang = r_json.get("language_detected", "?")
    conf_str = f"{conf*100:.0f}%" if conf else "N/A"
    print(f"  -> gravity={g} | confidence={conf_str} | langue={lang}")

# Cherche une image disponible dans les dossiers raw
def find_image(folders=["data/raw/ROUTE","data/raw/INFRASTRUCTURE","data/raw/EAU","data/raw/DECHETS"]):
    for folder in folders:
        if os.path.exists(folder):
            for f in os.listdir(folder):
                if f.lower().endswith((".jpg", ".jpeg", ".png")):
                    return os.path.join(folder, f)
    return None

IMAGE_PATH = find_image()

# ══════════════════════════════════════════════════════════════════
# TEST 1 — Health Check
# ══════════════════════════════════════════════════════════════════
separator("TEST 1 -- Health Check")
r    = requests.get(f"{BASE_URL}/health")
data = r.json()
print(json.dumps(data, indent=2, ensure_ascii=False))
check("status = ok",        data.get("status") == "ok")
check("version presente",   "version" in data)
check("gravity_ready",      data.get("gravity_ready") in [True, False])

# ══════════════════════════════════════════════════════════════════
# FRANCAIS — Tests texte seul : 4 niveaux de gravite
# ══════════════════════════════════════════════════════════════════
separator("TEST 2 -- FRANCAIS : gravite FAIBLE (texte seul)")
r    = requests.post(f"{BASE_URL}/analyze", data={
    "description": "Petite fissure sur le trottoir, pas dangereuse",
    "latitude": "36.8065", "longitude": "10.1815"
})
data = r.json()
print_gravity(data)
check("address present",              isinstance(data.get("address"), dict), data.get("address"))
check("gravity = FAIBLE ou MOYENNE",   data.get("gravity") in ["FAIBLE","MOYENNE"], data.get("gravity"))
check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
check("gravity_confidence >= 0.5",     (data.get("gravity_confidence") or 0) >= 0.5, data.get("gravity_confidence"))

separator("TEST 3 -- FRANCAIS : gravite MOYENNE (texte seul)")
r    = requests.post(f"{BASE_URL}/analyze", data={
    "description": "Nid de poule assez profond sur la route principale, plusieurs voitures abimees",
    "latitude": "36.8065", "longitude": "10.1815"
})
data = r.json()
print_gravity(data)
check("address present",              isinstance(data.get("address"), dict), data.get("address"))
check("gravity = MOYENNE ou ELEVEE",   data.get("gravity") in ["MOYENNE","ELEVEE"], data.get("gravity"))
check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
check("gravity_confidence >= 0.5",     (data.get("gravity_confidence") or 0) >= 0.5, data.get("gravity_confidence"))

separator("TEST 4 -- FRANCAIS : gravite ELEVEE (texte seul)")
r    = requests.post(f"{BASE_URL}/analyze", data={
    "description": "Rupture de canalisation causant une inondation de rue, quartier entier sans eau depuis 3 jours",
    "latitude": "36.8065", "longitude": "10.1815"
})
data = r.json()
print_gravity(data)
check("address present",              isinstance(data.get("address"), dict), data.get("address"))
check("gravity >= ELEVEE",             data.get("gravity") in ["ELEVEE","CRITIQUE"], data.get("gravity"))
check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
check("gravity_confidence >= 0.6",     (data.get("gravity_confidence") or 0) >= 0.6, data.get("gravity_confidence"))

separator("TEST 5 -- FRANCAIS : gravite CRITIQUE (texte seul)")
r    = requests.post(f"{BASE_URL}/analyze", data={
    "description": "Effondrement total d'un batiment habite, explosion de gaz, victimes possibles sous les decombres",
    "latitude": "36.8065", "longitude": "10.1815"
})
data = r.json()
print_gravity(data)
check("address present",              isinstance(data.get("address"), dict), data.get("address"))
check("gravity = CRITIQUE",            data.get("gravity") == "CRITIQUE", data.get("gravity"))
check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
check("gravity_confidence >= 0.7",     (data.get("gravity_confidence") or 0) >= 0.7, data.get("gravity_confidence"))

# ══════════════════════════════════════════════════════════════════
# FRANCAIS — Test avec image
# ══════════════════════════════════════════════════════════════════
separator("TEST 6 -- FRANCAIS + IMAGE : coherence texte/image")
if IMAGE_PATH:
    print(f"  Image: {IMAGE_PATH}")
    with open(IMAGE_PATH, "rb") as img:
        r = requests.post(f"{BASE_URL}/analyze",
            files={"image1": img},
            data={
                "description": "Grand nid de poule au milieu de la route, tres dangereux pour les voitures",
                "latitude": "36.8065", "longitude": "10.1815"
            }
        )
    data = r.json()
    print_gravity(data)
    check("has_image = True",              data.get("has_image") == True)
    check("address present",               isinstance(data.get("address"), dict), data.get("address"))
    check("gravity presente",              data.get("gravity") in ["FAIBLE","MOYENNE","ELEVEE","CRITIQUE"])
    check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
    check("priority presente",             data.get("priority") in ["BASSE","NORMALE","HAUTE","URGENTE"])
else:
    print("  [SKIP] Aucune image dans data/raw/")
    check("test image FR ignore (pas d'image)", True)

# ══════════════════════════════════════════════════════════════════
# ARABE STANDARD — Tests texte seul : 4 niveaux de gravite
# ══════════════════════════════════════════════════════════════════
separator("TEST 7 -- ARABE : gravite FAIBLE (texte seul)")
r    = requests.post(f"{BASE_URL}/analyze", data={
    "description": "\u0634\u0642\u0648\u0642 \u0635\u063a\u064a\u0631\u0629 \u0641\u064a \u0627\u0644\u0631\u0635\u064a\u0641 \u0644\u0627 \u062a\u0634\u0643\u0644 \u062e\u0637\u0631\u0627",
    "latitude": "36.8065", "longitude": "10.1815"
})
data = r.json()
print_gravity(data)
check("gravity = FAIBLE ou MOYENNE",   data.get("gravity") in ["FAIBLE","MOYENNE"], data.get("gravity"))
check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
check("gravity_confidence >= 0.5",     (data.get("gravity_confidence") or 0) >= 0.5, data.get("gravity_confidence"))

separator("TEST 8 -- ARABE : gravite MOYENNE (texte seul)")
r    = requests.post(f"{BASE_URL}/analyze", data={
    "description": "\u062d\u0641\u0631\u0629 \u0639\u0645\u064a\u0642\u0629 \u0641\u064a \u0648\u0633\u0637 \u0627\u0644\u0637\u0631\u064a\u0642 \u0627\u0644\u0631\u0626\u064a\u0633\u064a \u062a\u0633\u0628\u0628 \u0623\u0636\u0631\u0627\u0631\u0627 \u0644\u0644\u0633\u064a\u0627\u0631\u0627\u062a",
    "latitude": "36.8065", "longitude": "10.1815"
})
data = r.json()
print_gravity(data)
check("gravity >= MOYENNE",            data.get("gravity") in ["MOYENNE","ELEVEE","CRITIQUE"], data.get("gravity"))
check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
check("gravity_confidence >= 0.5",     (data.get("gravity_confidence") or 0) >= 0.5, data.get("gravity_confidence"))

separator("TEST 9 -- ARABE : gravite ELEVEE (texte seul)")
r    = requests.post(f"{BASE_URL}/analyze", data={
    "description": "\u0627\u0646\u0647\u064a\u0627\u0631 \u062c\u0632\u0626\u064a \u0644\u0644\u0637\u0631\u064a\u0642 \u0627\u0644\u0631\u0626\u064a\u0633\u064a \u0648\u0627\u0646\u0642\u0637\u0627\u0639 \u0627\u0644\u0645\u064a\u0627\u0647 \u0645\u0646\u0630 \u062b\u0644\u0627\u062b\u0629 \u0623\u064a\u0627\u0645 \u0641\u064a \u0627\u0644\u062d\u064a \u0628\u0623\u0643\u0645\u0644\u0647",
    "latitude": "36.8065", "longitude": "10.1815"
})
data = r.json()
print_gravity(data)
check("gravity >= ELEVEE",             data.get("gravity") in ["ELEVEE","CRITIQUE"], data.get("gravity"))
check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
check("gravity_confidence >= 0.6",     (data.get("gravity_confidence") or 0) >= 0.6, data.get("gravity_confidence"))

separator("TEST 10 -- ARABE : gravite CRITIQUE (texte seul)")
r    = requests.post(f"{BASE_URL}/analyze", data={
    "description": "\u0627\u0646\u0641\u062c\u0627\u0631 \u063a\u0627\u0632 \u0641\u064a \u0639\u0645\u0627\u0631\u0629 \u0633\u0643\u0646\u064a\u0629 \u0645\u0639 \u0636\u062d\u0627\u064a\u0627 \u0645\u062d\u062a\u0645\u0644\u064a\u0646 \u0648\u062d\u0631\u064a\u0642 \u064a\u0645\u062a\u062f \u0644\u0644\u0645\u0628\u0627\u0646\u064a \u0627\u0644\u0645\u062c\u0627\u0648\u0631\u0629",
    "latitude": "36.8065", "longitude": "10.1815"
})
data = r.json()
print_gravity(data)
check("gravity = CRITIQUE",            data.get("gravity") == "CRITIQUE", data.get("gravity"))
check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
check("gravity_confidence >= 0.7",     (data.get("gravity_confidence") or 0) >= 0.7, data.get("gravity_confidence"))

# ══════════════════════════════════════════════════════════════════
# ARABE + IMAGE
# ══════════════════════════════════════════════════════════════════
separator("TEST 11 -- ARABE + IMAGE : coherence texte/image")
if IMAGE_PATH:
    print(f"  Image: {IMAGE_PATH}")
    with open(IMAGE_PATH, "rb") as img:
        r = requests.post(f"{BASE_URL}/analyze",
            files={"image1": img},
            data={
                "description": "\u062d\u0641\u0631\u0629 \u0643\u0628\u064a\u0631\u0629 \u0641\u064a \u0645\u0646\u062a\u0635\u0641 \u0627\u0644\u0637\u0631\u064a\u0642 \u0627\u0644\u0631\u0626\u064a\u0633\u064a \u062e\u0637\u064a\u0631\u0629 \u062c\u062f\u0627 \u0639\u0644\u0649 \u0627\u0644\u0633\u064a\u0627\u0631\u0627\u062a",
                "latitude": "36.8065", "longitude": "10.1815"
            }
        )
    data = r.json()
    print_gravity(data)
    check("has_image = True",              data.get("has_image") == True)
    check("gravity presente",              data.get("gravity") in ["FAIBLE","MOYENNE","ELEVEE","CRITIQUE"])
    check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
    check("gravity >= MOYENNE",            data.get("gravity") in ["MOYENNE","ELEVEE","CRITIQUE"], data.get("gravity"))
else:
    print("  [SKIP] Aucune image dans data/raw/")
    check("test image AR ignore (pas d'image)", True)

# ══════════════════════════════════════════════════════════════════
# TUNISIEN DIALECTAL — Tests texte seul : 4 niveaux de gravite
# ══════════════════════════════════════════════════════════════════
separator("TEST 12 -- TUNISIEN : gravite FAIBLE (texte seul)")
r    = requests.post(f"{BASE_URL}/analyze", data={
    "description": "ghar sghir fil trotor mish khatar w mish 3ajel",
    "latitude": "36.8065", "longitude": "10.1815"
})
data = r.json()
print_gravity(data)
check("gravity = FAIBLE ou MOYENNE",   data.get("gravity") in ["FAIBLE","MOYENNE"], data.get("gravity"))
check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
check("gravity_confidence >= 0.5",     (data.get("gravity_confidence") or 0) >= 0.5, data.get("gravity_confidence"))

separator("TEST 13 -- TUNISIEN : gravite MOYENNE (texte seul)")
r    = requests.post(f"{BASE_URL}/analyze", data={
    "description": "fi treek 7ofra kbira w khatra lel 3arbiyyet w il nas",
    "latitude": "36.8065", "longitude": "10.1815"
})
data = r.json()
print_gravity(data)
check("gravity >= MOYENNE",            data.get("gravity") in ["MOYENNE","ELEVEE","CRITIQUE"], data.get("gravity"))
check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
check("gravity_confidence >= 0.5",     (data.get("gravity_confidence") or 0) >= 0.5, data.get("gravity_confidence"))

separator("TEST 14 -- TUNISIEN : gravite ELEVEE (texte seul)")
r    = requests.post(f"{BASE_URL}/analyze", data={
    "description": "treek 5sar w il 7ouma maqtou3a w il ma maqta3 min 3 iyyem",
    "latitude": "36.8065", "longitude": "10.1815"
})
data = r.json()
print_gravity(data)
check("gravity >= ELEVEE",             data.get("gravity") in ["ELEVEE","CRITIQUE"], data.get("gravity"))
check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
check("gravity_confidence >= 0.5",     (data.get("gravity_confidence") or 0) >= 0.5, data.get("gravity_confidence"))

separator("TEST 15 -- TUNISIEN : gravite CRITIQUE (texte seul)")
r    = requests.post(f"{BASE_URL}/analyze", data={
    "description": "il gaz infajar fil 3imara w fi blessa w jerou7 w 7ri9 yinfasher",
    "latitude": "36.8065", "longitude": "10.1815"
})
data = r.json()
print_gravity(data)
check("gravity = CRITIQUE",            data.get("gravity") == "CRITIQUE", data.get("gravity"))
check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
check("gravity_confidence >= 0.6",     (data.get("gravity_confidence") or 0) >= 0.6, data.get("gravity_confidence"))

# ══════════════════════════════════════════════════════════════════
# TUNISIEN + IMAGE
# ══════════════════════════════════════════════════════════════════
separator("TEST 16 -- TUNISIEN + IMAGE : coherence texte/image")
if IMAGE_PATH:
    print(f"  Image: {IMAGE_PATH}")
    with open(IMAGE_PATH, "rb") as img:
        r = requests.post(f"{BASE_URL}/analyze",
            files={"image1": img},
            data={
                "description": "fi treek 7ofra kbira w khatra lel 3arbiyyet w 3arabiyyat twa9a3 fiha kol youm",
                "latitude": "36.8065", "longitude": "10.1815"
            }
        )
    data = r.json()
    print_gravity(data)
    check("has_image = True",              data.get("has_image") == True)
    check("gravity presente",              data.get("gravity") in ["FAIBLE","MOYENNE","ELEVEE","CRITIQUE"])
    check("gravity_confidence presente",   data.get("gravity_confidence") is not None)
    check("gravity >= MOYENNE",            data.get("gravity") in ["MOYENNE","ELEVEE","CRITIQUE"], data.get("gravity"))
else:
    print("  [SKIP] Aucune image dans data/raw/")
    check("test image TN ignore (pas d'image)", True)

# ══════════════════════════════════════════════════════════════════
# TEST 17 — Detection de doublons
# ══════════════════════════════════════════════════════════════════
separator("TEST 17 -- Detection de doublons")
existing = [{
    "id": 42,
    "description": "Nid de poule dangereux avenue Habib Bourguiba",
    "lat": 36.8065, "lon": 10.1815, "vote_count": 5
}]
r    = requests.post(f"{BASE_URL}/check-duplicate", json={
    "description": "Gros trou dans la route avenue Bourguiba",
    "latitude": 36.8067, "longitude": 10.1816,
    "existing_reports": existing
})
data = r.json()
print(f"  Meme zone     : is_duplicate={data.get('is_duplicate')}")
check("doublon detecte (meme zone)",   data.get("is_duplicate") == True,  data.get("is_duplicate"))

r    = requests.post(f"{BASE_URL}/check-duplicate", json={
    "description": "Nid de poule dangereux",
    "latitude": 36.9000, "longitude": 10.3000,
    "existing_reports": existing
})
data = r.json()
print(f"  Zone eloignee : is_duplicate={data.get('is_duplicate')}")
check("non-doublon (zone differente)", data.get("is_duplicate") == False, data.get("is_duplicate"))

# ══════════════════════════════════════════════════════════════════
# TEST 18 — Update priorite apres votes
# ══════════════════════════════════════════════════════════════════
separator("TEST 18 -- Mise a jour priorite apres votes")
r    = requests.post(f"{BASE_URL}/update-priority", json={
    "gravity": "ELEVEE", "vote_count": 25,
    "domain": "ROUTE", "latitude": 36.8065, "longitude": 10.1815
})
data = r.json()
print(f"  -> priority={data.get('priority')} | score={data.get('score')}")
check("priority presente",  data.get("priority") in ["BASSE","NORMALE","HAUTE","URGENTE"])
check("score present",      data.get("score") is not None)
check("address present",    isinstance(data.get("address"), dict), data.get("address"))

# ══════════════════════════════════════════════════════════════════
# TEST 19 — Endpoint /explain
# ══════════════════════════════════════════════════════════════════
separator("TEST 19 -- Endpoint /explain")
r    = requests.post(f"{BASE_URL}/explain", json={
    "gravity":      "ELEVEE",
    "priority":     "URGENTE",
    "score":        72.0,
    "vote_count":   8,
    "domain":       "ROUTE",
    "zone_type":    "CRITIQUE",
    "zone_score":   2.0,
    "zone_details": ["Ecole : Ibn Khaldoun", "Axe routier principal"],
    "has_image":    True,
    "images_count": 1,
    "manipulation_flags": {
        "exaggeration_detected": False,
        "exaggeration_level": "none",
        "exaggeration_words": [],
        "image_text_coherent": True
    }
})
data = r.json()
print(f"  -> {data.get('explanation')}")
print(f"  -> Recommandation : {data.get('recommendation')}")
check("explanation presente",    "explanation" in data)
check("recommendation presente", "recommendation" in data)
check("reasons non vide",        len(data.get("reasons", [])) > 0)

# ══════════════════════════════════════════════════════════════════
# RESUME FINAL
# ══════════════════════════════════════════════════════════════════
print(f"\n{'='*60}")
print(f"  RESUME -- {passed}/{total} tests reussis")
if failed == 0:
    print(f"  Tous les tests passent !")
else:
    print(f"  {failed} test(s) echoue(s)")
print(f"{'='*60}\n")

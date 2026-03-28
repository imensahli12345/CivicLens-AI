# training/train_gravity.py
# Modèle multilingue : intfloat/multilingual-e5-base
# Supporte : français + arabe standard + tunisien dialectal
# Dataset  : 504 exemples équilibrés (126 par classe)
import pandas as pd
import pickle
import os
import numpy as np
from sentence_transformers import SentenceTransformer
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.metrics import classification_report, accuracy_score

MODEL_DIR = "models"
os.makedirs(MODEL_DIR, exist_ok=True)

GRAVITY_LABELS = {0: "FAIBLE", 1: "MOYENNE", 2: "ELEVEE", 3: "CRITIQUE"}

print("=" * 60)
print("  CivicLens AI — Entraînement gravité multilingue")
print("  Modèle : intfloat/multilingual-e5-base")
print("  Langues : français + arabe standard + tunisien dialectal")
print("=" * 60)

# ── Charger le dataset ────────────────────────────────────────────
df = pd.read_csv("data/gravity_dataset.csv")
print(f"\nDataset chargé : {len(df)} exemples")
print("\nDistribution des classes :")
for k, v in GRAVITY_LABELS.items():
    count = (df['gravite'] == k).sum()
    pct   = count / len(df) * 100
    bar   = "█" * int(pct / 2)
    print(f"  {v:10s} : {count:3d} exemples ({pct:.0f}%) {bar}")

# Vérification
min_count = df['gravite'].value_counts().min()
if min_count < 50:
    print(f"\n[ATTENTION] Classe sous-représentée : {min_count} exemples minimum")
    print("  Recommandé : au moins 80 exemples par classe\n")
else:
    print(f"\n[OK] Toutes les classes ont au moins {min_count} exemples\n")

# ── Charger multilingual-e5-base ──────────────────────────────────
# Premier lancement : téléchargement ~278MB depuis HuggingFace
# Lancements suivants : chargé depuis le cache local (~5 sec)
print("[1/3] Chargement du modèle intfloat/multilingual-e5-base...")
print("      (téléchargement ~278MB au premier lancement)")
embedder = SentenceTransformer("intfloat/multilingual-e5-base")
print("[OK]  Modèle chargé\n")

# ── Générer les embeddings ────────────────────────────────────────
# multilingual-e5 requiert le préfixe "query: " pour la classification
print(f"[2/3] Génération des embeddings ({len(df)} phrases × 768 dimensions)...")
descriptions_prefixed = ["query: " + d for d in df['description'].tolist()]
X = embedder.encode(
    descriptions_prefixed,
    batch_size=32,
    show_progress_bar=True,
    convert_to_numpy=True
)
y = df['gravite'].values
print(f"[OK]  Embeddings générés : shape={X.shape}\n")

# ── Split train / test ────────────────────────────────────────────
X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42, stratify=y
)
print(f"Train : {len(X_train)} | Test : {len(X_test)}")

# ── LogisticRegression ────────────────────────────────────────────
print("\n[3/3] Entraînement LogisticRegression...")
model = LogisticRegression(
    max_iter=1000,
    C=2.0,
    random_state=42,
    solver='lbfgs',
    class_weight='balanced'
)
model.fit(X_train, y_train)
print("[OK]  Modèle entraîné\n")

# ── Évaluation ────────────────────────────────────────────────────
y_pred    = model.predict(X_test)
acc       = accuracy_score(y_test, y_pred)
cv_scores = cross_val_score(model, X, y, cv=5, scoring='accuracy')

print("=" * 60)
print("  RÉSULTATS")
print("=" * 60)
print(f"  Accuracy test  : {acc*100:.1f}%")
print(f"  Cross-val (5k) : {cv_scores.mean()*100:.1f}% ± {cv_scores.std()*100:.1f}%")
print()
print(classification_report(y_test, y_pred, target_names=list(GRAVITY_LABELS.values())))

# ── Tests multilingues ────────────────────────────────────────────
print("=" * 60)
print("  TESTS MULTILINGUES")
print("=" * 60)

test_cases = [
    # Français
    ("FR", "petite fissure sur le trottoir"),
    ("FR", "nid de poule profond sur route principale"),
    ("FR", "effondrement partiel d'une route principale"),
    ("FR", "explosion de gaz dans un immeuble habité"),
    # Arabe standard
    ("AR", "شقوق صغيرة في الرصيف"),
    ("AR", "حفرة عميقة في وسط الطريق الرئيسي"),
    ("AR", "انهيار جزئي للطريق الرئيسي"),
    ("AR", "انفجار غاز في عمارة سكنية"),
    # Tunisien dialectal
    ("TN", "ghar sghir fil trotor"),
    ("TN", "fi treek 7ofra kbira w khatra lel 3arbiyyet"),
    ("TN", "treek 5sar w il 7ouma maqtou3a"),
    ("TN", "il gaz infajar fil 3imara w fi blessa"),
]

for lang, text in test_cases:
    vec  = embedder.encode(["query: " + text], convert_to_numpy=True)
    pred = model.predict(vec)[0]
    prob = model.predict_proba(vec)[0]
    bar  = "█" * int(max(prob) * 15)
    print(f"  [{lang}] [{GRAVITY_LABELS[pred]:8s}] {bar:15s} {max(prob)*100:.0f}%")
    print(f"         \"{text[:55]}\"")

# ── Sauvegarder ──────────────────────────────────────────────────
print("\n" + "=" * 60)
pickle.dump(model, open(f"{MODEL_DIR}/gravity_model.pkl", "wb"))
pickle.dump("intfloat/multilingual-e5-base",
            open(f"{MODEL_DIR}/gravity_embedder_name.pkl", "wb"))

# Supprimer l'ancien vectorizer TF-IDF s'il existe (plus utilisé)
old_vec = f"{MODEL_DIR}/gravity_vectorizer.pkl"
if os.path.exists(old_vec):
    os.remove(old_vec)
    print(f"  [OK] Ancien gravity_vectorizer.pkl supprimé (remplacé par multilingual-e5)")

print(f"  [OK] gravity_model.pkl sauvegardé")
print(f"  [OK] gravity_embedder_name.pkl sauvegardé")
print(f"\n  Relancer : uvicorn api.main:app --reload --port 8001")
print("=" * 60)
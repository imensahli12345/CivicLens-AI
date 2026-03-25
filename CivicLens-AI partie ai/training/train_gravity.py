# training/train_gravity.py
import pandas as pd
import pickle
import os
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import train_test_split
from sklearn.metrics import classification_report, confusion_matrix, accuracy_score

MODEL_DIR = "models"
os.makedirs(MODEL_DIR, exist_ok=True)

GRAVITY_LABELS = {0: "FAIBLE", 1: "MOYENNE", 2: "ELEVEE", 3: "CRITIQUE"}

print("=== Entraînement modèle gravité ===\n")

# ── Charger le dataset ────────────────────────────────────────────
df = pd.read_csv("data/gravity_dataset.csv")
print(f"Dataset chargé : {len(df)} exemples")
print("\nDistribution des classes :")
for k, v in GRAVITY_LABELS.items():
    count = (df['gravite'] == k).sum()
    print(f"  {v:10s} (={k}) : {count} exemples")

# ── Split train / test ────────────────────────────────────────────
X_train, X_test, y_train, y_test = train_test_split(
    df['description'], df['gravite'],
    test_size=0.2, random_state=42, stratify=df['gravite']
)
print(f"\nTrain : {len(X_train)} | Test : {len(X_test)}")

# ── TF-IDF ────────────────────────────────────────────────────────
# ngram_range=(1,2) : unigrammes + bigrammes (ex: "eau potable")
vectorizer = TfidfVectorizer(
    max_features=2000,
    ngram_range=(1, 2),
    min_df=1,
    analyzer='word',
    strip_accents='unicode'    # gère les accents français
)
X_train_vec = vectorizer.fit_transform(X_train)
X_test_vec  = vectorizer.transform(X_test)

# ── Modèle Logistic Regression ────────────────────────────────────
model = LogisticRegression(
    max_iter=1000,
    C=1.0,
    random_state=42,
    solver='lbfgs'
)
model.fit(X_train_vec, y_train)

# ── Évaluation ────────────────────────────────────────────────────
y_pred = model.predict(X_test_vec)
acc    = accuracy_score(y_test, y_pred)

print(f"\n=== Résultats ===")
print(f"Accuracy : {acc*100:.1f}%\n")
print("Rapport détaillé :")
print(classification_report(
    y_test, y_pred,
    target_names=list(GRAVITY_LABELS.values())
))

# ── Test sur des exemples concrets ───────────────────────────────
print("=== Tests manuels ===")
test_examples = [
    "petit trou sur le côté de la route",
    "fuite d'eau importante, rue inondée",
    "pas d'eau potable depuis 5 jours, 500 familles",
    "nid de poule géant, accidents signalés",
    "lampadaire éteint depuis hier",
    "quartier entier sans électricité depuis 2 jours",
    "manque de bus depuis une semaine, population isolée",
]
for text in test_examples:
    vec  = vectorizer.transform([text])
    pred = model.predict(vec)[0]
    prob = model.predict_proba(vec)[0]
    print(f"  '{text[:55]}...' → {GRAVITY_LABELS[pred]} ({max(prob)*100:.0f}%)")

# ── Sauvegarder ──────────────────────────────────────────────────
pickle.dump(vectorizer, open(f"{MODEL_DIR}/gravity_vectorizer.pkl", "wb"))
pickle.dump(model,      open(f"{MODEL_DIR}/gravity_model.pkl",      "wb"))

print(f"\n✅ Modèles sauvegardés dans {MODEL_DIR}/")
print(f"   gravity_vectorizer.pkl")
print(f"   gravity_model.pkl")
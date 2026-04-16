# CivicLens AI — Guide d'exécution

## Ordre exact à suivre

### 1. Installer les dépendances
```bash
pip install -r requirements.txt
```

### 2. Préparer les images (redimensionner à 224x224)
```bash
python utils/preprocessing.py
```

### 3. Entraîner le classificateur de domaine (image)
```bash
python training/train_classifier.py
```
→ Génère : models/classifier_best.pth
→ Durée : 10-30 min selon GPU

### 4. Entraîner le modèle de gravité (texte)
```bash
python training/train_gravity.py
```
→ Génère : models/gravity_model.pkl + models/gravity_vectorizer.pkl
→ Durée : quelques secondes

### 5. Évaluer le modèle (optionnel)
```bash
python training/evaluate.py
```
→ Génère : models/evaluation/confusion_matrix.png

### 6. Lancer le microservice FastAPI
```bash
uvicorn api.main:app --reload --port 8001
```
→ Swagger UI : http://localhost:8001/docs

### 7. Tester (dans un 2ème terminal)
```bash
python test_api.py
```

## Structure du projet
```
civiclens-ai/
├── api/
│   └── main.py                  ← FastAPI endpoints
├── data/
│   ├── raw/                     ← images brutes par classe
│   │   ├── ROUTE/
│   │   ├── EAU/
│   │   ├── ECLAIRAGE/
│   │   ├── DECHETS/
│   │   ├── INFRASTRUCTURE/
│   │   └── AUTRE/
│   ├── processed/               ← images redimensionnées (généré)
│   └── gravity_dataset.csv      ← dataset gravité (texte)
├── models/                      ← modèles sauvegardés (généré)
├── training/
│   ├── train_classifier.py      ← entraînement MobileNetV2
│   ├── train_gravity.py         ← entraînement TF-IDF
│   └── evaluate.py              ← évaluation + matrice confusion
├── utils/
│   ├── preprocessing.py         ← préparation images
│   ├── duplicate_detector.py    ← détection doublons
│   ├── zone_analyzer.py         ← analyse zone géographique
│   └── priority_calculator.py   ← calcul priorité
├── test_api.py                  ← tests complets
└── requirements.txt
```

## Notes importantes
- TRANSPORT et EAU potable : pas d'images → classification par texte automatique
- GPU NVIDIA détecté automatiquement par PyTorch
- Swagger disponible sur http://localhost:8001/docs pour tester sans code
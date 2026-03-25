# training/evaluate.py
import torch
import torch.nn as nn
from torchvision import datasets, transforms, models
from torch.utils.data import DataLoader
from sklearn.metrics import classification_report, confusion_matrix
import matplotlib.pyplot as plt
import matplotlib
matplotlib.use('Agg')
import numpy as np
import os

MODEL_PATH = "models/classifier_best.pth"
DATA_DIR   = "data/processed"
OUTPUT_DIR = "models/evaluation"
os.makedirs(OUTPUT_DIR, exist_ok=True)

print("=== Évaluation du classificateur de domaine ===\n")

# ── Charger le modèle ─────────────────────────────────────────────
checkpoint  = torch.load(MODEL_PATH, map_location="cpu")
CLASS_NAMES = checkpoint['class_names']
NUM_CLASSES = len(CLASS_NAMES)
print(f"Modèle chargé : epoch {checkpoint['epoch']}, val_acc {checkpoint['val_acc']:.1f}%")
print(f"Classes : {CLASS_NAMES}")

model = models.mobilenet_v2(weights=None)
model.classifier = nn.Sequential(
    nn.Dropout(p=0.3),
    nn.Linear(model.last_channel, 256),
    nn.ReLU(),
    nn.Dropout(p=0.2),
    nn.Linear(256, NUM_CLASSES)
)
model.load_state_dict(checkpoint['model_state'])
model.eval()

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
model  = model.to(device)

# ── Dataset complet ───────────────────────────────────────────────
transform = transforms.Compose([
    transforms.Resize((224, 224)),
    transforms.ToTensor(),
    transforms.Normalize([0.485, 0.456, 0.406],
                         [0.229, 0.224, 0.225])
])
full_dataset = datasets.ImageFolder(DATA_DIR, transform=transform)
loader       = DataLoader(full_dataset, batch_size=32, shuffle=False)

# ── Prédictions ───────────────────────────────────────────────────
all_preds, all_labels = [], []
with torch.no_grad():
    for images, labels in loader:
        images = images.to(device)
        outputs = model(images)
        _, predicted = outputs.max(1)
        all_preds.extend(predicted.cpu().numpy())
        all_labels.extend(labels.numpy())

# ── Rapport ───────────────────────────────────────────────────────
print("\n=== Rapport de classification ===")
print(classification_report(all_labels, all_preds, target_names=CLASS_NAMES))

# ── Matrice de confusion ──────────────────────────────────────────
cm = confusion_matrix(all_labels, all_preds)
fig, ax = plt.subplots(figsize=(10, 8))
im = ax.imshow(cm, interpolation='nearest', cmap=plt.cm.Blues)
plt.colorbar(im)
ax.set(xticks=np.arange(NUM_CLASSES),
       yticks=np.arange(NUM_CLASSES),
       xticklabels=CLASS_NAMES,
       yticklabels=CLASS_NAMES,
       title='Matrice de Confusion — CivicLens AI',
       ylabel='Vraie classe',
       xlabel='Classe prédite')
plt.setp(ax.get_xticklabels(), rotation=45, ha="right")
thresh = cm.max() / 2.
for i in range(NUM_CLASSES):
    for j in range(NUM_CLASSES):
        ax.text(j, i, format(cm[i, j], 'd'),
                ha="center", va="center",
                color="white" if cm[i, j] > thresh else "black")
plt.tight_layout()
plt.savefig(f"{OUTPUT_DIR}/confusion_matrix.png", dpi=150)
print(f"\n✅ Matrice de confusion sauvegardée : {OUTPUT_DIR}/confusion_matrix.png")
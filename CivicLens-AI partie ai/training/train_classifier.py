# training/train_classifier.py
import torch
import torch.nn as nn
from torchvision import datasets, transforms, models
from torch.utils.data import DataLoader, random_split
import os

# ── Configuration ─────────────────────────────────────────────────
DATA_DIR    = "data/processed"
MODEL_DIR   = "models"
BATCH_SIZE  = 32
EPOCHS      = 20
LR          = 0.001
IMAGE_SIZE  = 224
os.makedirs(MODEL_DIR, exist_ok=True)

# ── Transformations ───────────────────────────────────────────────
# Train : augmentation pour enrichir le dataset
train_transform = transforms.Compose([
    transforms.Resize((IMAGE_SIZE, IMAGE_SIZE)),
    transforms.RandomHorizontalFlip(),
    transforms.RandomVerticalFlip(p=0.1),
    transforms.RandomRotation(20),
    transforms.ColorJitter(brightness=0.3, contrast=0.3, saturation=0.2),
    transforms.RandomGrayscale(p=0.05),
    transforms.ToTensor(),
    transforms.Normalize([0.485, 0.456, 0.406],
                         [0.229, 0.224, 0.225])
])

# Validation : pas d'augmentation
val_transform = transforms.Compose([
    transforms.Resize((IMAGE_SIZE, IMAGE_SIZE)),
    transforms.ToTensor(),
    transforms.Normalize([0.485, 0.456, 0.406],
                         [0.229, 0.224, 0.225])
])

# ── Charger le dataset ────────────────────────────────────────────
print("=== Chargement du dataset ===")
full_dataset = datasets.ImageFolder(DATA_DIR, transform=train_transform)
CLASS_NAMES  = full_dataset.classes
NUM_CLASSES  = len(CLASS_NAMES)

print(f"Classes détectées : {CLASS_NAMES}")
print(f"Nombre de classes : {NUM_CLASSES}")
print(f"Total images      : {len(full_dataset)}")

# Afficher la distribution par classe
print("\nDistribution par classe :")
for i, cls in enumerate(CLASS_NAMES):
    count = sum(1 for _, label in full_dataset.samples if label == i)
    print(f"  {cls:20s} : {count} images")

# Split 80% train / 20% validation
train_size = int(0.8 * len(full_dataset))
val_size   = len(full_dataset) - train_size
train_data, val_data = random_split(full_dataset, [train_size, val_size])

# Appliquer val_transform sur le set de validation
val_data.dataset.transform = val_transform

train_loader = DataLoader(train_data, batch_size=BATCH_SIZE, shuffle=True,  num_workers=0)
val_loader   = DataLoader(val_data,   batch_size=BATCH_SIZE, shuffle=False, num_workers=0)

print(f"\nTrain : {train_size} images | Validation : {val_size} images")

# ── Modèle MobileNetV2 avec Transfer Learning ─────────────────────
print("\n=== Chargement MobileNetV2 pré-entraîné ===")
model = models.mobilenet_v2(weights=models.MobileNet_V2_Weights.DEFAULT)

# Phase 1 : geler toutes les couches sauf la dernière
for param in model.parameters():
    param.requires_grad = False

# Remplacer le classificateur final
model.classifier = nn.Sequential(
    nn.Dropout(p=0.3),
    nn.Linear(model.last_channel, 256),
    nn.ReLU(),
    nn.Dropout(p=0.2),
    nn.Linear(256, NUM_CLASSES)
)

# ── GPU si disponible ─────────────────────────────────────────────
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
print(f"Entraînement sur : {device}")
if device.type == "cuda":
    print(f"GPU : {torch.cuda.get_device_name(0)}")
model = model.to(device)

# ── Optimiseur et scheduler ───────────────────────────────────────
criterion = nn.CrossEntropyLoss()
optimizer = torch.optim.Adam(model.classifier.parameters(), lr=LR, weight_decay=1e-4)
scheduler = torch.optim.lr_scheduler.ReduceLROnPlateau(
    optimizer, mode='max', patience=3, factor=0.5
)

# ── Boucle d'entraînement ─────────────────────────────────────────
print(f"\n=== Entraînement ({EPOCHS} epochs) ===\n")
best_val_acc  = 0.0
patience_count = 0
EARLY_STOP    = 7  # arrêt si pas d'amélioration après 7 epochs

for epoch in range(EPOCHS):

    # ── Phase Train ───────────────────────────────────────────────
    model.train()
    train_loss, correct, total = 0.0, 0, 0

    for images, labels in train_loader:
        images, labels = images.to(device), labels.to(device)
        optimizer.zero_grad()
        outputs = model(images)
        loss    = criterion(outputs, labels)
        loss.backward()
        optimizer.step()

        train_loss += loss.item()
        _, predicted = outputs.max(1)
        correct += predicted.eq(labels).sum().item()
        total   += labels.size(0)

    train_acc = 100. * correct / total

    # ── Phase Validation ──────────────────────────────────────────
    model.eval()
    val_loss, val_correct, val_total = 0.0, 0, 0

    with torch.no_grad():
        for images, labels in val_loader:
            images, labels = images.to(device), labels.to(device)
            outputs = model(images)
            loss    = criterion(outputs, labels)
            val_loss    += loss.item()
            _, predicted = outputs.max(1)
            val_correct += predicted.eq(labels).sum().item()
            val_total   += labels.size(0)

    val_acc = 100. * val_correct / val_total
    scheduler.step(val_acc)

    print(f"Epoch [{epoch+1:02d}/{EPOCHS}] | "
          f"Train Loss: {train_loss/len(train_loader):.3f} | "
          f"Train Acc: {train_acc:.1f}% | "
          f"Val Acc: {val_acc:.1f}%", end="")

    # Sauvegarder le meilleur modèle
    if val_acc > best_val_acc:
        best_val_acc   = val_acc
        patience_count = 0
        torch.save({
            'epoch':       epoch + 1,
            'model_state': model.state_dict(),
            'class_names': CLASS_NAMES,
            'val_acc':     val_acc,
            'num_classes': NUM_CLASSES
        }, f"{MODEL_DIR}/classifier_best.pth")
        print(f"  ✅ Sauvegardé (meilleur : {val_acc:.1f}%)")
    else:
        patience_count += 1
        print(f"  (patience: {patience_count}/{EARLY_STOP})")
        if patience_count >= EARLY_STOP:
            print(f"\n⏹ Early stopping à l'epoch {epoch+1}")
            break

print(f"\n✅ Entraînement terminé !")
print(f"   Meilleure précision validation : {best_val_acc:.1f}%")
print(f"   Modèle sauvegardé : {MODEL_DIR}/classifier_best.pth")
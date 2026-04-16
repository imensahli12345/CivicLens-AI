# utils/preprocessing.py
from PIL import Image, ImageFilter
import os
import hashlib
import numpy as np

# Classes avec images
IMAGE_CLASSES = ["ROUTE", "EAU", "ECLAIRAGE", "DECHETS", "INFRASTRUCTURE", "AUTRE"]

# ── Seuils de nettoyage ───────────────────────────────────────────
BLUR_THRESHOLD = 100.0  # en dessous = image trop floue
MIN_SIZE       = 50     # ignorer images plus petites que 50x50 pixels
BRIGHTNESS_MIN = 30     # en dessous = image trop sombre (0 = noir total)
BRIGHTNESS_MAX = 230    # au dessus  = image trop claire (255 = blanc total)


def compute_hash(img: Image.Image) -> str:
    """
    Hash MD5 sur miniature 16x16 pour détecter les doublons exacts.
    """
    img_small = img.resize((16, 16)).convert("L")
    return hashlib.md5(img_small.tobytes()).hexdigest()


def compute_blur_score(img: Image.Image) -> float:
    """
    Variance du Laplacien — score bas = image floue.
    """
    img_gray = img.convert("L")
    img_lap  = img_gray.filter(ImageFilter.FIND_EDGES)
    arr      = np.array(img_lap, dtype=np.float32)
    return float(np.var(arr))


def compute_brightness(img: Image.Image) -> float:
    """
    Luminosité moyenne de l'image (0 = noir total, 255 = blanc total).
    Calcul : moyenne des pixels en niveaux de gris.
    """
    img_gray = img.convert("L")
    arr      = np.array(img_gray, dtype=np.float32)
    return float(np.mean(arr))


def prepare_dataset(raw_dir="data/raw", output_dir="data/processed", size=(224, 224)):
    """
    Prépare le dataset :
    1. Filtre les images trop petites        (< MIN_SIZE)
    2. Supprime les doublons exacts          (même hash)
    3. Filtre les images trop floues         (score < BLUR_THRESHOLD)
    4. Filtre les images trop sombres/claires(brightness hors [MIN, MAX])
    5. Redimensionne à 224x224
    6. Sauvegarde dans data/processed/
    """
    print("=" * 55)
    print("  CivicLens AI — Préparation & Nettoyage Dataset")
    print("=" * 55)
    print(f"  Seuil flou        : {BLUR_THRESHOLD}")
    print(f"  Taille minimum    : {MIN_SIZE}x{MIN_SIZE} px")
    print(f"  Luminosité min    : {BRIGHTNESS_MIN}  (trop sombre en dessous)")
    print(f"  Luminosité max    : {BRIGHTNESS_MAX}  (trop claire au dessus)")
    print()

    total_ok        = 0
    total_dup       = 0
    total_blur      = 0
    total_small     = 0
    total_dark      = 0
    total_bright    = 0
    total_corrupt   = 0

    for class_name in IMAGE_CLASSES:
        class_path = os.path.join(raw_dir, class_name)
        out_path   = os.path.join(output_dir, class_name)

        if not os.path.exists(class_path):
            print(f"[ATTENTION] Dossier manquant : {class_path}")
            continue

        os.makedirs(out_path, exist_ok=True)

        seen_hashes   = set()
        class_ok      = 0
        class_dup     = 0
        class_blur    = 0
        class_small   = 0
        class_dark    = 0
        class_bright  = 0
        class_corrupt = 0

        files = [f for f in os.listdir(class_path)
                 if f.lower().endswith((".jpg", ".jpeg", ".png", ".bmp", ".webp"))]

        print(f"  [{class_name}] — {len(files)} images trouvées")

        for img_name in files:
            img_path = os.path.join(class_path, img_name)
            out_file = os.path.join(out_path, os.path.splitext(img_name)[0] + ".jpg")

            try:
                img = Image.open(img_path).convert("RGB")

                # 1. Taille minimale
                w, h = img.size
                if w < MIN_SIZE or h < MIN_SIZE:
                    class_small += 1
                    continue

                # 2. Doublons
                img_hash = compute_hash(img)
                if img_hash in seen_hashes:
                    class_dup += 1
                    continue
                seen_hashes.add(img_hash)

                # 3. Flou
                blur_score = compute_blur_score(img)
                if blur_score < BLUR_THRESHOLD:
                    class_blur += 1
                    continue

                # 4. Luminosité
                brightness = compute_brightness(img)
                if brightness < BRIGHTNESS_MIN:
                    class_dark += 1
                    continue
                if brightness > BRIGHTNESS_MAX:
                    class_bright += 1
                    continue

                # 5. Resize et sauvegarde
                img = img.resize(size, Image.LANCZOS)
                img.save(out_file, "JPEG", quality=90)
                class_ok += 1

            except Exception as e:
                print(f"    [SKIP corrompu] {img_name} — {e}")
                class_corrupt += 1

        print(f"    OK              : {class_ok}")
        print(f"    Doublons        : {class_dup}")
        print(f"    Trop floues     : {class_blur}")
        print(f"    Trop sombres    : {class_dark}")
        print(f"    Trop claires    : {class_bright}")
        print(f"    Trop petites    : {class_small}")
        if class_corrupt > 0:
            print(f"    Corrompues      : {class_corrupt}")
        print()

        total_ok      += class_ok
        total_dup     += class_dup
        total_blur    += class_blur
        total_small   += class_small
        total_dark    += class_dark
        total_bright  += class_bright
        total_corrupt += class_corrupt

    print("=" * 55)
    print("  RÉSUMÉ GLOBAL")
    print("=" * 55)
    print(f"  Images gardées   : {total_ok}")
    print(f"  Doublons supprim : {total_dup}")
    print(f"  Trop floues      : {total_blur}")
    print(f"  Trop sombres     : {total_dark}")
    print(f"  Trop claires     : {total_bright}")
    print(f"  Trop petites     : {total_small}")
    print(f"  Corrompues       : {total_corrupt}")
    print(f"\n  Dataset prêt dans : {output_dir}/")
    print("=" * 55)

    avg = total_ok / len(IMAGE_CLASSES) if IMAGE_CLASSES else 0
    if avg < 150:
        print(f"\nATTENTION : moyenne {avg:.0f} images/classe — insuffisant.")
        print("Recommandé : minimum 200 images par classe.")
    elif avg < 300:
        print(f"\nMoyenne : {avg:.0f} images/classe — correct pour un PFE.")
    else:
        print(f"\nMoyenne : {avg:.0f} images/classe — excellent !")


if __name__ == "__main__":
    prepare_dataset()
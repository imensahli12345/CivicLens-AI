# utils/zone_analyzer.py
import requests
import time
import json
import os
import hashlib

# ── Cache géographique ────────────────────────────────────────────
# Evite de rappeler Overpass API pour des coordonnées déjà analysées
# Fichier JSON : models/zone_cache.json
CACHE_FILE      = "models/zone_cache.json"
CACHE_PRECISION = 3    # arrondir à 3 décimales ≈ 111m de précision
                       # deux points dans le même carré de 111m partagent le même cache

def _pick_first(address: dict, keys: list) -> str:
    for k in keys:
        v = address.get(k)
        if v:
            return str(v)
    return ""

def _extract_region(address: dict) -> dict:
    suburb = _pick_first(address, [
        "suburb", "neighbourhood", "quarter", "city_district",
        "hamlet", "village", "town", "city"
    ])
    county = _pick_first(address, ["county", "municipality", "district"])
    state = _pick_first(address, ["state", "state_district", "region"])
    country = _pick_first(address, ["country"])
    if country and county:
        country_lower = country.lower()
        if country_lower in ("tunisie", "tunisia"):
            # Correction spécifique pour Nabeul (Nominatim peut renvoyer 'El Asouak')
            if county.lower() == "el asouak":
                county = "Nabeul"
                
            if not county.lower().startswith("délégation"):
                county = f"Délégation de {county}"
    return {
        "suburb": suburb or None,
        "county": county or None,
        "state": state or None,
        "country": country or None
    }

def _load_cache() -> dict:
    """Charge le cache depuis le fichier JSON."""
    if os.path.exists(CACHE_FILE):
        try:
            with open(CACHE_FILE, "r", encoding="utf-8") as f:
                return json.load(f)
        except Exception:
            return {}
    return {}

def _save_cache(cache: dict):
    """Sauvegarde le cache dans le fichier JSON."""
    os.makedirs(os.path.dirname(CACHE_FILE), exist_ok=True)
    try:
        with open(CACHE_FILE, "w", encoding="utf-8") as f:
            json.dump(cache, f, ensure_ascii=False, indent=2)
    except Exception as e:
        print(f"[ZoneCache] Erreur sauvegarde : {e}")

def _cache_key(lat: float, lon: float) -> str:
    """
    Clé de cache basée sur les coordonnées arrondies.
    CACHE_PRECISION=3 → carré de ~111m × ~111m
    Deux points dans ce carré retournent le même résultat.
    """
    lat_r = round(lat, CACHE_PRECISION)
    lon_r = round(lon, CACHE_PRECISION)
    return f"{lat_r},{lon_r}"


def get_zone_score(lat: float, lon: float) -> dict:
    """
    Analyse la zone géographique autour d'un point via Overpass API.
    Retourne un score d'importance (0.5 → 2.0) et le type de zone.

    Cache activé : si la zone a déjà été analysée dans un rayon de ~111m,
    retourne le résultat en cache sans appeler Overpass API.

    Score :
      < 1.0  → zone isolée / peu fréquentée  (BASSE)
      1.0    → zone normale                  (NORMALE)
      1.4+   → zone fréquentée               (HAUTE)
      1.8+   → zone très critique            (CRITIQUE)
    """
    # ── Vérifier le cache ─────────────────────────────────────────
    cache     = _load_cache()
    cache_key = _cache_key(lat, lon)

    if cache_key in cache:
        cached = cache[cache_key]
        print(f"[ZoneCache] Cache hit pour {cache_key} → {cached['zone_type']} (score={cached['zone_score']})")
        cached["from_cache"] = True
        cached.setdefault("address", {})
        cached.setdefault("region", _extract_region(cached.get("address", {})))
        return cached

    # ── Résultat par défaut ───────────────────────────────────────
    zone_info = {
        "zone_score":   1.0,
        "zone_type":    "NORMALE",
        "zone_details": [],
        "road_type":    "unknown",
        "address":      {},
        "region":       {"suburb": None, "county": None, "state": None, "country": None},
        "from_cache":   False
    }

    headers = {"User-Agent": "CivicLens-AI/1.0 (PFE project)", "Accept-Language": "fr"}

    try:
        # ── 1. Reverse geocoding ──────────────────────────────────
        reverse_url = (
            f"https://nominatim.openstreetmap.org/reverse"
            f"?lat={lat}&lon={lon}&format=json&addressdetails=1&zoom=18&accept-language=fr"
        )
        r    = requests.get(reverse_url, headers=headers, timeout=6)
        data = r.json()
        raw_address = data.get("address", {})
        road    = raw_address.get("road", "")
        zone_info["road_type"] = road
        
        filtered_addr = {}
        city_val = _pick_first(raw_address, ["city", "town", "village", "municipality", "suburb"])
        if city_val: filtered_addr["city"] = str(city_val)
        if "county" in raw_address: filtered_addr["county"] = str(raw_address["county"])
        if "postcode" in raw_address: filtered_addr["postcode"] = str(raw_address["postcode"])
        if "country" in raw_address: filtered_addr["country"] = str(raw_address["country"])
        if "state" in raw_address: filtered_addr["state"] = str(raw_address["state"])
        
        zone_info["address"] = filtered_addr
        zone_info["region"] = _extract_region(raw_address)

        # ── 2. Overpass : POIs dans un rayon de 500m ─────────────
        overpass_url = "https://overpass-api.de/api/interpreter"
        query = f"""
        [out:json][timeout:12];
        (
          node["amenity"="school"](around:500,{lat},{lon});
          node["amenity"="hospital"](around:500,{lat},{lon});
          node["amenity"="clinic"](around:500,{lat},{lon});
          node["amenity"="university"](around:500,{lat},{lon});
          node["amenity"="kindergarten"](around:500,{lat},{lon});
          node["amenity"="marketplace"](around:300,{lat},{lon});
          node["amenity"="bus_station"](around:300,{lat},{lon});
          node["highway"="bus_stop"](around:300,{lat},{lon});
          node["shop"](around:300,{lat},{lon});
          node["office"](around:300,{lat},{lon});
        );
        out body;
        """
        rp       = requests.post(overpass_url, data=query, headers=headers, timeout=12)
        elements = rp.json().get("elements", [])

        score   = 1.0
        details = []
        seen    = set()

        for el in elements:
            tags    = el.get("tags", {})
            amenity = tags.get("amenity", "")
            shop    = tags.get("shop", "")
            name    = tags.get("name", amenity or shop or "")

            if name in seen:
                continue
            seen.add(name)

            if amenity == "school":
                score += 0.4
                details.append(f"École : {name}")
            elif amenity == "hospital":
                score += 0.5
                details.append(f"Hôpital : {name}")
            elif amenity == "clinic":
                score += 0.3
                details.append(f"Clinique : {name}")
            elif amenity == "university":
                score += 0.3
                details.append(f"Université : {name}")
            elif amenity == "kindergarten":
                score += 0.4
                details.append(f"Maternelle : {name}")
            elif amenity == "marketplace":
                score += 0.2
                details.append(f"Marché : {name}")
            elif amenity in ("bus_station", "bus_stop") or tags.get("highway") == "bus_stop":
                score += 0.15
                details.append(f"Arrêt bus : {name}")
            elif shop:
                score += 0.05

        # ── 3. Bonus selon type de route ──────────────────────────
        road_lower = road.lower()
        if any(k in road_lower for k in ["primary", "trunk", "motorway", "principale"]):
            score += 0.4
            details.append("Axe routier principal")
        elif any(k in road_lower for k in ["secondary", "secondaire"]):
            score += 0.2
            details.append("Route secondaire")
        elif any(k in road_lower for k in ["residential", "résidentiel"]):
            score += 0.1
            details.append("Zone résidentielle")
        elif any(k in road_lower for k in ["track", "path", "piste"]):
            score -= 0.3
            details.append("Route isolée / piste")

        score = max(0.5, min(score, 2.0))

        if score >= 1.8:
            zone_type = "CRITIQUE"
        elif score >= 1.4:
            zone_type = "HAUTE"
        elif score >= 1.0:
            zone_type = "NORMALE"
        else:
            zone_type = "BASSE"

        zone_info["zone_score"]   = round(score, 2)
        zone_info["zone_type"]    = zone_type
        zone_info["zone_details"] = details[:8]

        time.sleep(1)  # respecter limite Nominatim

        # ── Sauvegarder dans le cache ─────────────────────────────
        cache_entry = {
            "zone_score":   zone_info["zone_score"],
            "zone_type":    zone_info["zone_type"],
            "zone_details": zone_info["zone_details"],
            "road_type":    zone_info["road_type"],
            "address":      zone_info["address"],
            "region":       zone_info["region"],
            "from_cache":   False
        }
        cache[cache_key] = cache_entry
        _save_cache(cache)
        print(f"[ZoneCache] Sauvegardé : {cache_key} → {zone_type}")

    except Exception as e:
        print(f"[ZoneAnalyzer] Erreur réseau (score par défaut 1.0) : {e}")

    return zone_info

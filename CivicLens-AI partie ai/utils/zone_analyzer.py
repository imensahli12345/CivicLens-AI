# utils/zone_analyzer.py
import requests
import time

def get_zone_score(lat: float, lon: float) -> dict:
    """
    Analyse la zone géographique autour d'un point via Overpass API (OpenStreetMap).
    Retourne un score d'importance (0.5 → 2.0) et le type de zone.

    Score :
      < 1.0  → zone isolée / peu fréquentée  (BASSE)
      1.0    → zone normale                  (NORMALE)
      1.4+   → zone fréquentée               (HAUTE)
      1.8+   → zone très critique            (CRITIQUE)
    """
    zone_info = {
        "zone_score":   1.0,
        "zone_type":    "NORMALE",
        "zone_details": [],
        "road_type":    "unknown"
    }

    headers = {"User-Agent": "CivicLens-AI/1.0 (PFE project)"}

    try:
        # ── 1. Reverse geocoding : type de route du point ─────────
        reverse_url = (
            f"https://nominatim.openstreetmap.org/reverse"
            f"?lat={lat}&lon={lon}&format=json"
        )
        r    = requests.get(reverse_url, headers=headers, timeout=6)
        data = r.json()
        address = data.get("address", {})
        road    = address.get("road", "")
        zone_info["road_type"] = road

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
        seen    = set()   # éviter les doublons de nom

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
                score += 0.05   # boutique = légère fréquentation

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
            score -= 0.3   # route isolée → score réduit
            details.append("Route isolée / piste")

        # Plafonner entre 0.5 et 2.0
        score = max(0.5, min(score, 2.0))

        # ── 4. Classification de la zone ──────────────────────────
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
        zone_info["zone_details"] = details[:8]   # max 8 détails

        # Respecter la limite Nominatim (1 req/sec)
        time.sleep(1)

    except Exception as e:
        print(f"[ZoneAnalyzer] Erreur réseau (score par défaut 1.0) : {e}")

    return zone_info
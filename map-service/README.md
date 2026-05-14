# CivicLens Map Service — Microservice Indépendant

Microservice carte urbaine pour CivicLens AI.  
**Totalement indépendant du backend .NET** — lit la même base PostgreSQL en lecture seule.

## Architecture

```
map-service/
├── backend/              ← FastAPI Python
│   ├── app/
│   │   ├── main.py           # Entry point
│   │   ├── config.py         # Config (.env)
│   │   ├── db/database.py    # Connexion async PostgreSQL
│   │   ├── models/
│   │   │   ├── report.py     # SQLAlchemy → table Signalements
│   │   │   └── schemas.py    # Pydantic DTOs
│   │   ├── routers/
│   │   │   ├── map.py        # REST endpoints
│   │   │   └── ws.py         # WebSocket temps réel
│   │   └── services/
│   │       └── report_service.py  # Logique métier
│   ├── requirements.txt
│   ├── Dockerfile
│   └── .env              ← CONFIGURER ICI
├── frontend/
│   └── index.html        # Page de test (Leaflet.js CDN)
└── docker-compose.yml
```

## Setup rapide (sans Docker)

### 1. Configurer la base de données

Éditer `backend/.env` :
```env
DATABASE_URL=postgresql+asyncpg://USER:PASSWORD@HOST:5432/NOM_BDD
```

### 2. Installer les dépendances Python

```bash
cd backend
pip install -r requirements.txt
```

### 3. Lancer le serveur

```bash
cd backend
uvicorn app.main:app --host 0.0.0.0 --port 8002 --reload
```

### 4. Ouvrir le frontend de test

Ouvrir `frontend/index.html` directement dans le navigateur,  
ou via Live Server (VS Code), ou :
```bash
cd frontend
python -m http.server 8080
# → http://localhost:8080
```

## Setup avec Docker

```bash
# Éditer DATABASE_URL dans docker-compose.yml
docker-compose up --build
```

- Backend API : http://localhost:8002
- Frontend test : http://localhost:8080
- Swagger docs : http://localhost:8002/docs

## Endpoints API

| Méthode | Endpoint | Description |
|---------|----------|-------------|
| GET | `/map/reports` | Markers carte (filtrables) |
| GET | `/map/reports/{id}` | Détail complet |
| GET | `/map/geojson` | Export GeoJSON |
| GET | `/map/heatmap` | Données heatmap |
| GET | `/map/stats` | Statistiques agrégées |
| GET | `/health` | Health check |
| WS  | `/ws/map-updates` | Temps réel |

### Filtres disponibles (query params)

```
statut     = EN_ATTENTE | OUVERTE | ENVOYEE | EN_COURS_DETUDE | RESOLU | REJETE
priorite   = FAIBLE | MOYENNE | HAUTE | CRITIQUE
gravite    = FAIBLE | MODEREE | GRAVE | CRITIQUE
domaine    = ROUTES | ECLAIRAGE | DECHETS | TRANSPORT | EAU | ...
min_score  = 0.0 - 100.0
date_from  = ISO 8601
date_to    = ISO 8601
bbox       = lat_min,lon_min,lat_max,lon_max
limit      = max 2000 (défaut 500)
offset     = pagination
```

### Exemple d'appel

```bash
# Tous les signalements critiques
curl http://localhost:8002/map/reports?priorite=CRITIQUE

# GeoJSON des signalements non résolus en Tunisie
curl "http://localhost:8002/map/geojson?bbox=30.2,7.5,37.5,11.6"

# Stats globales
curl http://localhost:8002/map/stats
```

## Notes importantes

- Le microservice est **READ-ONLY** — il ne modifie jamais la base de données
- Les noms de tables utilisent la casse EF Core (ex: `"Signalements"`, `"Citoyens"`)
- Si tes noms de colonnes diffèrent, adapter `backend/app/models/report.py`
- Le WebSocket poll la DB toutes les 5 secondes pour les nouveaux signalements

## Intégration future dans le projet Angular

Remplacer `API_BASE` dans `index.html` par `environment.mapServiceUrl` dans Angular,  
et utiliser `MapService` (décrit dans le rapport PDF) pour encapsuler les appels HTTP.

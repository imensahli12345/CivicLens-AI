import asyncio
import sys
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

from app.config import settings
from app.routers import map, ws
from app.routers.ws import poll_new_signalements

if sys.platform.startswith("win") and hasattr(asyncio, "WindowsSelectorEventLoopPolicy"):
    asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())


# ── Lifespan (startup / shutdown) ────────────────────────────────────────────

@asynccontextmanager
async def lifespan(app: FastAPI):
    # Start background WS polling task
    poller = asyncio.create_task(poll_new_signalements())
    print("[OK] CivicLens Map Service started")
    print(f"   - API docs: http://{settings.APP_HOST}:{settings.APP_PORT}/docs")
    print(f"   - WebSocket: ws://{settings.APP_HOST}:{settings.APP_PORT}/ws/map-updates")
    yield
    poller.cancel()
    print("[BYE] CivicLens Map Service stopped")


# ── App ───────────────────────────────────────────────────────────────────────

app = FastAPI(
    title="CivicLens Map Service",
    description="""
## 🗺️ CivicLens AI — Microservice Carte Urbaine

Microservice indépendant exposant les signalements géolocalisés pour la carte interactive.

### Endpoints principaux
- **GET /map/reports** — Markers pour Leaflet.js (filtrable)
- **GET /map/reports/{id}** — Détail complet d'un signalement
- **GET /map/geojson** — Export GeoJSON filtré
- **GET /map/heatmap** — Données heatmap
- **GET /map/stats** — Statistiques agrégées
- **WS /ws/map-updates** — Flux temps réel
    """,
    version="1.0.0",
    lifespan=lifespan,
)

# ── CORS ──────────────────────────────────────────────────────────────────────

app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.get_cors_origins() + ["*"],  # Permissive for dev
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ── Routers ───────────────────────────────────────────────────────────────────

app.include_router(map.router)
app.include_router(ws.router)


# ── Health ────────────────────────────────────────────────────────────────────

@app.get("/health", tags=["Health"])
async def health():
    return JSONResponse({"status": "ok", "service": "CivicLens Map Service", "version": "1.0.0"})


@app.get("/", tags=["Health"])
async def root():
    return JSONResponse({
        "service": "CivicLens Map Service",
        "docs": "/docs",
        "health": "/health",
        "endpoints": [
            "GET /map/reports",
            "GET /map/reports/{id}",
            "GET /map/geojson",
            "GET /map/heatmap",
            "GET /map/stats",
            "WS  /ws/map-updates",
        ]
    })

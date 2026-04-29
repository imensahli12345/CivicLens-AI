from pydantic import BaseModel, Field
from typing import Optional, List, Any
from datetime import datetime
from enum import Enum


# ── Enums matching StatutSignalement.cs ──────────────────────────────────────

class StatutSignalement(str, Enum):
    EN_ATTENTE = "EN_ATTENTE"
    OUVERTE = "OUVERTE"
    ENVOYEE = "ENVOYEE"
    EN_COURS_DETUDE = "EN_COURS_DETUDE"
    RESOLU = "RESOLU"
    REJETE = "REJETE"


class Priorite(str, Enum):
    FAIBLE = "FAIBLE"
    MOYENNE = "MOYENNE"
    HAUTE = "HAUTE"
    CRITIQUE = "CRITIQUE"


class Gravite(str, Enum):
    FAIBLE = "FAIBLE"
    MODEREE = "MODEREE"
    GRAVE = "GRAVE"
    CRITIQUE = "CRITIQUE"


# ── Map Marker DTO (lightweight — for listing many markers) ──────────────────

class ReportMarkerDTO(BaseModel):
    id: int
    latitude: float
    longitude: float
    statut: Optional[str] = None
    priorite: Optional[str] = None
    gravite: Optional[str] = None
    domaine: Optional[str] = None
    score_ai: Optional[float] = None
    date_creation: datetime
    vote_count: int = 0
    has_images: bool = False
    description_short: str = ""  # First 80 chars

    model_config = {"from_attributes": True}


# ── Full Report DTO (for detail panel) ───────────────────────────────────────

class ImageDTO(BaseModel):
    id: str
    url: Optional[str] = None

    model_config = {"from_attributes": True}


class ReportDetailDTO(BaseModel):
    id: int
    description: str
    statut: Optional[str] = None
    priorite: Optional[str] = None
    gravite: Optional[str] = None
    domaine: Optional[str] = None
    score_ai: Optional[float] = None
    adresse: Optional[Any] = None
    latitude: Optional[float] = None
    longitude: Optional[float] = None
    date_creation: datetime
    citoyen_id: str
    organisation_id: Optional[str] = None
    vote_count: int = 0
    images: List[ImageDTO] = []

    model_config = {"from_attributes": True}


# ── GeoJSON ──────────────────────────────────────────────────────────────────

class GeoJSONFeature(BaseModel):
    type: str = "Feature"
    geometry: dict
    properties: dict


class GeoJSONCollection(BaseModel):
    type: str = "FeatureCollection"
    features: List[GeoJSONFeature]
    total: int


# ── Filters ──────────────────────────────────────────────────────────────────

class MapFilters(BaseModel):
    statut: Optional[str] = None
    priorite: Optional[str] = None
    gravite: Optional[str] = None
    domaine: Optional[str] = None
    min_score: Optional[float] = None
    date_from: Optional[datetime] = None
    date_to: Optional[datetime] = None
    bbox: Optional[str] = None   # "lat_min,lon_min,lat_max,lon_max"
    limit: int = Field(default=500, le=2000)
    offset: int = 0


# ── Stats ─────────────────────────────────────────────────────────────────────

class MapStatsDTO(BaseModel):
    total: int
    by_statut: dict
    by_priorite: dict
    by_domaine: dict
    avg_score_ai: Optional[float] = None

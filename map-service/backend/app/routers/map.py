from datetime import datetime
import base64
import json
import uuid
from typing import Optional

from fastapi import APIRouter, Depends, HTTPException, Query, Request
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.db.database import get_db
from app.models.report import Organisation
from app.services.report_service import report_service

router = APIRouter(prefix="/map", tags=["Map"])


def _base64url_decode(value: str) -> bytes:
    padded = value + "=" * (-len(value) % 4)
    return base64.urlsafe_b64decode(padded.encode("utf-8"))


def _try_extract_user_id_from_jwt(token: str) -> Optional[str]:
    try:
        parts = token.split(".")
        if len(parts) < 2:
            return None
        payload = json.loads(_base64url_decode(parts[1]).decode("utf-8"))
        return (
            payload.get("nameid")
            or payload.get("sub")
            or payload.get("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
        )
    except Exception:
        return None


async def _resolve_org_domaine(request: Request, db: AsyncSession) -> Optional[str]:
    auth = request.headers.get("Authorization") or request.headers.get("authorization")
    if not auth:
        return None
    token = auth.replace("Bearer ", "").strip()
    if not token:
        return None

    user_id = _try_extract_user_id_from_jwt(token)
    if not user_id:
        return None

    try:
        org_id = uuid.UUID(str(user_id))
        q = select(Organisation.DomaineOrganisation).where(Organisation.Id == org_id)
        res = await db.execute(q)
        domaine = res.scalar_one_or_none()
        return domaine
    except Exception:
        return None


@router.get("/reports")
async def get_reports(
    request: Request,
    statut: Optional[str] = Query(None),
    priorite: Optional[str] = Query(None),
    gravite: Optional[str] = Query(None),
    domaine: Optional[str] = Query(None),
    min_score: Optional[float] = Query(None),
    date_from: Optional[datetime] = Query(None),
    date_to: Optional[datetime] = Query(None),
    bbox: Optional[str] = Query(None),
    limit: int = Query(500, ge=1, le=2000),
    offset: int = Query(0, ge=0),
    db: AsyncSession = Depends(get_db),
):
    org_domaine = await _resolve_org_domaine(request, db)
    if org_domaine:
        domaine = org_domaine
    markers, total = await report_service.get_markers(
        db=db,
        statut=statut,
        priorite=priorite,
        gravite=gravite,
        domaine=domaine,
        min_score=min_score,
        date_from=date_from,
        date_to=date_to,
        bbox=bbox,
        limit=limit,
        offset=offset,
    )
    return {
        "total": total,
        "returned": len(markers),
        "offset": offset,
        "data": [m.model_dump() for m in markers],
    }


@router.get("/reports/{report_id}")
async def get_report_detail(report_id: int, request: Request, db: AsyncSession = Depends(get_db)):
    org_domaine = await _resolve_org_domaine(request, db)
    detail = await report_service.get_detail(db, report_id, domaine=org_domaine)
    if not detail:
        raise HTTPException(status_code=404, detail="Report not found")
    return detail.model_dump()


@router.get("/stats")
async def get_stats(request: Request, db: AsyncSession = Depends(get_db)):
    org_domaine = await _resolve_org_domaine(request, db)
    stats = await report_service.get_stats(db, domaine=org_domaine)
    return stats.model_dump()


@router.get("/geojson")
async def get_geojson(
    request: Request,
    statut: Optional[str] = Query(None),
    priorite: Optional[str] = Query(None),
    gravite: Optional[str] = Query(None),
    domaine: Optional[str] = Query(None),
    bbox: Optional[str] = Query(None),
    limit: int = Query(1000, ge=1, le=5000),
    db: AsyncSession = Depends(get_db),
):
    org_domaine = await _resolve_org_domaine(request, db)
    if org_domaine:
        domaine = org_domaine
    geojson = await report_service.get_geojson(
        db=db,
        statut=statut,
        priorite=priorite,
        gravite=gravite,
        domaine=domaine,
        bbox=bbox,
        limit=limit,
    )
    return geojson.model_dump()


@router.get("/heatmap")
async def get_heatmap(
    request: Request,
    priorite: Optional[str] = Query(None),
    statut: Optional[str] = Query(None),
    db: AsyncSession = Depends(get_db),
):
    org_domaine = await _resolve_org_domaine(request, db)
    points = await report_service.get_heatmap_data(db=db, priorite=priorite, statut=statut, domaine=org_domaine)
    return {"count": len(points), "points": points}

from datetime import datetime
from typing import Optional

from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.ext.asyncio import AsyncSession

from app.db.database import get_db
from app.services.report_service import report_service

router = APIRouter(prefix="/map", tags=["Map"])


@router.get("/reports")
async def get_reports(
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
async def get_report_detail(report_id: int, db: AsyncSession = Depends(get_db)):
    detail = await report_service.get_detail(db, report_id)
    if not detail:
        raise HTTPException(status_code=404, detail="Report not found")
    return detail.model_dump()


@router.get("/stats")
async def get_stats(db: AsyncSession = Depends(get_db)):
    stats = await report_service.get_stats(db)
    return stats.model_dump()


@router.get("/geojson")
async def get_geojson(
    statut: Optional[str] = Query(None),
    priorite: Optional[str] = Query(None),
    gravite: Optional[str] = Query(None),
    domaine: Optional[str] = Query(None),
    bbox: Optional[str] = Query(None),
    limit: int = Query(1000, ge=1, le=5000),
    db: AsyncSession = Depends(get_db),
):
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
    priorite: Optional[str] = Query(None),
    statut: Optional[str] = Query(None),
    db: AsyncSession = Depends(get_db),
):
    points = await report_service.get_heatmap_data(db=db, priorite=priorite, statut=statut)
    return {"count": len(points), "points": points}

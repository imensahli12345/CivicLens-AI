from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, func, and_, cast, Float
from sqlalchemy.orm import selectinload
from typing import List, Optional, Tuple
from datetime import datetime

from app.models.report import Signalement, ImageSignalement, Vote
from app.models.schemas import (
    ReportMarkerDTO, ReportDetailDTO, ImageDTO,
    GeoJSONCollection, GeoJSONFeature, MapStatsDTO
)


def _build_filters(query, statut, priorite, gravite, domaine, min_score, date_from, date_to, bbox):
    """Build dynamic WHERE clauses."""
    conditions = [
        Signalement.Latitude.isnot(None),
        Signalement.Longitude.isnot(None),
    ]

    if statut:
        conditions.append(Signalement.Statut == statut)
    if priorite:
        conditions.append(Signalement.Priorite == priorite)
    if gravite:
        conditions.append(Signalement.Gravite == gravite)
    if domaine:
        conditions.append(Signalement.Domaine == domaine)
    if min_score is not None:
        conditions.append(Signalement.ScoreAI >= min_score)
    if date_from:
        conditions.append(Signalement.DateCreation >= date_from)
    if date_to:
        conditions.append(Signalement.DateCreation <= date_to)

    # Bounding box filter: "lat_min,lon_min,lat_max,lon_max"
    if bbox:
        try:
            lat_min, lon_min, lat_max, lon_max = [float(x) for x in bbox.split(",")]
            conditions.append(Signalement.Latitude >= lat_min)
            conditions.append(Signalement.Latitude <= lat_max)
            conditions.append(Signalement.Longitude >= lon_min)
            conditions.append(Signalement.Longitude <= lon_max)
        except Exception:
            pass  # ignore invalid bbox

    return query.where(and_(*conditions))


def _to_marker(row: Signalement) -> ReportMarkerDTO:
    vote_count = len(row.Votes) if row.Votes else 0
    has_images = len(row.Images) > 0 if row.Images else False
    desc_short = row.Description[:80] + "..." if len(row.Description) > 80 else row.Description

    return ReportMarkerDTO(
        id=row.Id,
        latitude=row.Latitude,
        longitude=row.Longitude,
        statut=row.Statut,
        priorite=row.Priorite,
        gravite=row.Gravite,
        domaine=row.Domaine,
        score_ai=row.ScoreAI,
        date_creation=row.DateCreation,
        vote_count=vote_count,
        has_images=has_images,
        description_short=desc_short,
    )


def _to_detail(row: Signalement) -> ReportDetailDTO:
    return ReportDetailDTO(
        id=row.Id,
        description=row.Description,
        statut=row.Statut,
        priorite=row.Priorite,
        gravite=row.Gravite,
        domaine=row.Domaine,
        score_ai=row.ScoreAI,
        adresse=row.Adresse,
        latitude=row.Latitude,
        longitude=row.Longitude,
        date_creation=row.DateCreation,
        citoyen_id=str(row.CitoyenId),
        organisation_id=str(row.OrganisationId) if row.OrganisationId else None,
        vote_count=len(row.Votes) if row.Votes else 0,
        images=[ImageDTO(id=str(img.Id), url=img.UrlImage) for img in (row.Images or [])],
    )


class ReportService:

    async def get_markers(
        self,
        db: AsyncSession,
        statut: Optional[str] = None,
        priorite: Optional[str] = None,
        gravite: Optional[str] = None,
        domaine: Optional[str] = None,
        min_score: Optional[float] = None,
        date_from: Optional[datetime] = None,
        date_to: Optional[datetime] = None,
        bbox: Optional[str] = None,
        limit: int = 500,
        offset: int = 0,
    ) -> Tuple[List[ReportMarkerDTO], int]:

        base_q = (
            select(Signalement)
            .options(
                selectinload(Signalement.Votes),
                selectinload(Signalement.Images),
            )
            .order_by(Signalement.DateCreation.desc())
        )

        base_q = _build_filters(base_q, statut, priorite, gravite, domaine, min_score, date_from, date_to, bbox)

        # Count total
        count_q = select(func.count()).select_from(
            _build_filters(
                select(Signalement),
                statut, priorite, gravite, domaine, min_score, date_from, date_to, bbox
            ).subquery()
        )
        total_result = await db.execute(count_q)
        total = total_result.scalar_one()

        # Paginated results
        result = await db.execute(base_q.limit(limit).offset(offset))
        rows = result.scalars().all()

        return [_to_marker(r) for r in rows], total

    async def get_detail(self, db: AsyncSession, report_id: int) -> Optional[ReportDetailDTO]:
        q = (
            select(Signalement)
            .options(
                selectinload(Signalement.Votes),
                selectinload(Signalement.Images),
            )
            .where(Signalement.Id == report_id)
        )
        result = await db.execute(q)
        row = result.scalar_one_or_none()
        if not row:
            return None
        return _to_detail(row)

    async def get_geojson(
        self,
        db: AsyncSession,
        statut: Optional[str] = None,
        priorite: Optional[str] = None,
        gravite: Optional[str] = None,
        domaine: Optional[str] = None,
        bbox: Optional[str] = None,
        limit: int = 1000,
    ) -> GeoJSONCollection:

        base_q = (
            select(Signalement)
            .options(selectinload(Signalement.Votes))
            .order_by(Signalement.DateCreation.desc())
            .limit(limit)
        )
        base_q = _build_filters(base_q, statut, priorite, gravite, domaine, None, None, None, bbox)

        result = await db.execute(base_q)
        rows = result.scalars().all()

        features = []
        for row in rows:
            features.append(GeoJSONFeature(
                geometry={
                    "type": "Point",
                    "coordinates": [row.Longitude, row.Latitude],
                },
                properties={
                    "id": row.Id,
                    "statut": row.Statut,
                    "priorite": row.Priorite,
                    "gravite": row.Gravite,
                    "domaine": row.Domaine,
                    "score_ai": row.ScoreAI,
                    "vote_count": len(row.Votes) if row.Votes else 0,
                    "date_creation": row.DateCreation.isoformat(),
                    "description_short": row.Description[:80],
                }
            ))

        return GeoJSONCollection(features=features, total=len(features))

    async def get_stats(self, db: AsyncSession) -> MapStatsDTO:
        # Total with coords
        total_q = select(func.count()).where(
            Signalement.Latitude.isnot(None),
            Signalement.Longitude.isnot(None),
        )
        total = (await db.execute(total_q)).scalar_one()

        # By statut
        statut_q = (
            select(Signalement.Statut, func.count().label("cnt"))
            .where(Signalement.Latitude.isnot(None))
            .group_by(Signalement.Statut)
        )
        by_statut = {row[0]: row[1] for row in (await db.execute(statut_q)).all()}

        # By priorité
        priorite_q = (
            select(Signalement.Priorite, func.count().label("cnt"))
            .where(Signalement.Latitude.isnot(None))
            .group_by(Signalement.Priorite)
        )
        by_priorite = {str(row[0]): row[1] for row in (await db.execute(priorite_q)).all()}

        # By domaine
        domaine_q = (
            select(Signalement.Domaine, func.count().label("cnt"))
            .where(Signalement.Latitude.isnot(None))
            .group_by(Signalement.Domaine)
        )
        by_domaine = {str(row[0]): row[1] for row in (await db.execute(domaine_q)).all()}

        # Avg score AI
        avg_q = select(func.avg(Signalement.ScoreAI)).where(
            Signalement.Latitude.isnot(None),
            Signalement.ScoreAI.isnot(None),
        )
        avg_score = (await db.execute(avg_q)).scalar_one_or_none()

        return MapStatsDTO(
            total=total,
            by_statut=by_statut,
            by_priorite=by_priorite,
            by_domaine=by_domaine,
            avg_score_ai=round(avg_score, 3) if avg_score else None,
        )

    async def get_heatmap_data(
        self,
        db: AsyncSession,
        priorite: Optional[str] = None,
        statut: Optional[str] = None,
    ) -> List[dict]:
        """Returns lightweight lat/lon/intensity for heatmap layer."""
        base_q = select(
            Signalement.Latitude,
            Signalement.Longitude,
            Signalement.ScoreAI,
            Signalement.Priorite,
        ).where(
            Signalement.Latitude.isnot(None),
            Signalement.Longitude.isnot(None),
        )

        if priorite:
            base_q = base_q.where(Signalement.Priorite == priorite)
        if statut:
            base_q = base_q.where(Signalement.Statut == statut)

        result = await db.execute(base_q)
        rows = result.all()

        # Map priority to intensity weight
        priority_weight = {"CRITIQUE": 1.0, "HAUTE": 0.75, "MOYENNE": 0.5, "FAIBLE": 0.25}

        return [
            {
                "lat": row[0],
                "lon": row[1],
                "intensity": row[2] / 100 if row[2] else priority_weight.get(row[3], 0.3),
            }
            for row in rows
        ]


report_service = ReportService()

"""
WebSocket endpoint for real-time map updates.
The backend polls the DB every 5 seconds and pushes new signalements to connected clients.
No Redis dependency required for basic operation.
"""
import asyncio
import json
from datetime import datetime, timezone
from typing import Set

from fastapi import APIRouter, WebSocket, WebSocketDisconnect
from sqlalchemy import select

from app.db.database import AsyncSessionLocal
from app.models.report import Signalement

router = APIRouter(tags=["WebSocket"])

# Connected clients registry
active_connections: Set[WebSocket] = set()

# Track the latest signalement ID seen to detect new ones
_last_max_id: int = 0


class ConnectionManager:
    def __init__(self):
        self.connections: Set[WebSocket] = set()

    async def connect(self, ws: WebSocket):
        await ws.accept()
        self.connections.add(ws)

    def disconnect(self, ws: WebSocket):
        self.connections.discard(ws)

    async def broadcast(self, message: dict):
        dead = set()
        for ws in self.connections:
            try:
                await ws.send_json(message)
            except Exception:
                dead.add(ws)
        for ws in dead:
            self.connections.discard(ws)


manager = ConnectionManager()


async def poll_new_signalements():
    """Background task: polls DB every 5s, pushes new signalements to WS clients."""
    global _last_max_id

    # Initialize with current max ID
    async with AsyncSessionLocal() as db:
        result = await db.execute(select(Signalement.Id).order_by(Signalement.Id.desc()).limit(1))
        row = result.scalar_one_or_none()
        _last_max_id = row or 0

    while True:
        await asyncio.sleep(5)

        if not manager.connections:
            continue

        try:
            async with AsyncSessionLocal() as db:
                q = (
                    select(Signalement)
                    .where(
                        Signalement.Id > _last_max_id,
                        Signalement.Latitude.isnot(None),
                        Signalement.Longitude.isnot(None),
                    )
                    .order_by(Signalement.Id.asc())
                )
                result = await db.execute(q)
                new_rows = result.scalars().all()

                if new_rows:
                    _last_max_id = max(r.Id for r in new_rows)

                    for row in new_rows:
                        payload = {
                            "event": "new_signalement",
                            "data": {
                                "id": row.Id,
                                "latitude": row.Latitude,
                                "longitude": row.Longitude,
                                "statut": row.Statut,
                                "priorite": row.Priorite,
                                "gravite": row.Gravite,
                                "domaine": row.Domaine,
                                "score_ai": row.ScoreAI,
                                "date_creation": row.DateCreation.isoformat(),
                                "description_short": row.Description[:80],
                            }
                        }
                        await manager.broadcast(payload)

        except Exception as e:
            print(f"[WS Poller] Error: {e}")


@router.websocket("/ws/map-updates")
async def websocket_map_updates(websocket: WebSocket):
    """
    Connect to receive real-time push notifications when new signalements are added.
    The client receives JSON messages of shape:
      { "event": "new_signalement", "data": { ...ReportMarkerDTO... } }
    Also sends a heartbeat ping every 30s to keep the connection alive.
    """
    await manager.connect(websocket)
    try:
        # Send connection confirmation
        await websocket.send_json({
            "event": "connected",
            "message": "CivicLens Map Service — real-time updates active",
            "timestamp": datetime.now(timezone.utc).isoformat(),
        })

        # Keep connection alive — wait for client messages or disconnect
        while True:
            try:
                # Timeout every 30s to send heartbeat
                data = await asyncio.wait_for(websocket.receive_text(), timeout=30.0)
                # Handle client ping
                if data == "ping":
                    await websocket.send_json({"event": "pong"})
            except asyncio.TimeoutError:
                # Send server-side heartbeat
                await websocket.send_json({
                    "event": "heartbeat",
                    "timestamp": datetime.now(timezone.utc).isoformat(),
                })

    except WebSocketDisconnect:
        manager.disconnect(websocket)
    except Exception as e:
        print(f"[WS] Client error: {e}")
        manager.disconnect(websocket)

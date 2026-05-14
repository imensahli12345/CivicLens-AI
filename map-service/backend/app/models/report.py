"""
SQLAlchemy model that maps READ-ONLY to the existing .NET 'Signalements' table.
We do NOT modify the schema — this microservice only reads data.
"""
from sqlalchemy import Column, BigInteger, String, DateTime, Float, Text, ForeignKey
from sqlalchemy.dialects.postgresql import UUID as PG_UUID, JSONB
from sqlalchemy.orm import relationship
from app.db.database import Base
import uuid


class Signalement(Base):
    """
    Maps to the existing 'Signalements' table created by .NET EF Core.
    Column names match EF Core conventions (PascalCase → same in PG).
    """
    __tablename__ = "Signalements"

    Id = Column(BigInteger, primary_key=True, autoincrement=True)
    Description = Column(Text, nullable=False)
    DateCreation = Column(DateTime(timezone=True), nullable=False)

    # Enums stored as strings (HasConversion<string>() in EF Core)
    Statut = Column(String, nullable=False, default="EN_ATTENTE")
    Gravite = Column(String, nullable=True)
    Priorite = Column(String, nullable=True)
    Domaine = Column(String, nullable=True)

    ScoreAI = Column(Float, nullable=True)

    # JSONB address field
    Adresse = Column(JSONB, nullable=True)

    Latitude = Column(Float, nullable=True)
    Longitude = Column(Float, nullable=True)

    # Foreign Keys
    CitoyenId = Column(PG_UUID(as_uuid=True), ForeignKey("Citoyens.\"Id\""), nullable=False)
    OrganisationId = Column(PG_UUID(as_uuid=True), ForeignKey("Organisations.\"Id\""), nullable=True)

    # Relationships (read-only, lazy loaded)
    Images = relationship("ImageSignalement", back_populates="Signalement", lazy="selectin")
    Votes = relationship("Vote", back_populates="Signalement", lazy="selectin")


class ImageSignalement(Base):
    __tablename__ = "ImagesSignalement"

    Id = Column(PG_UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    UrlImage = Column(Text, nullable=True)
    SignalementId = Column(BigInteger, ForeignKey("Signalements.Id"), nullable=False)

    Signalement = relationship("Signalement", back_populates="Images")


class Vote(Base):
    __tablename__ = "Votes"

    Id = Column(PG_UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    SignalementId = Column(BigInteger, ForeignKey("Signalements.Id"), nullable=False)
    CitoyenId = Column(PG_UUID(as_uuid=True), nullable=False)

    Signalement = relationship("Signalement", back_populates="Votes")


class Organisation(Base):
    __tablename__ = "Organisations"

    Id = Column(PG_UUID(as_uuid=True), primary_key=True)
    DomaineOrganisation = Column(String, nullable=False)

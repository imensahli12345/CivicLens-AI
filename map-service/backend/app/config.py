from pydantic_settings import BaseSettings
from typing import List
import json


class Settings(BaseSettings):
    DATABASE_URL: str = "postgresql+asyncpg://postgres:postgres@localhost:5432/civiclens"
    APP_HOST: str = "0.0.0.0"
    APP_PORT: int = 8001
    CORS_ORIGINS: str = '["http://localhost:3000","http://localhost:4200","http://localhost:8080","http://127.0.0.1:5500"]'
    USE_REDIS: bool = False
    REDIS_URL: str = "redis://localhost:6379"

    def get_cors_origins(self) -> List[str]:
        try:
            return json.loads(self.CORS_ORIGINS)
        except Exception:
            return ["*"]

    class Config:
        env_file = ".env"
        extra = "ignore"


settings = Settings()

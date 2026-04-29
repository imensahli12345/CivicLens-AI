import argparse
import asyncio
import os
import sys

import uvicorn


def main() -> None:
    parser = argparse.ArgumentParser(description="Run CivicLens backend with Windows-safe loop")
    parser.add_argument("--host", default=os.getenv("APP_HOST", "0.0.0.0"))
    parser.add_argument("--port", type=int, default=int(os.getenv("APP_PORT", "8001")))
    parser.add_argument("--reload", action="store_true")
    args = parser.parse_args()

    if sys.platform.startswith("win") and hasattr(asyncio, "WindowsSelectorEventLoopPolicy"):
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())

    uvicorn.run("app.main:app", host=args.host, port=args.port, reload=args.reload)


if __name__ == "__main__":
    main()

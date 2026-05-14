@echo off
echo ==========================================
echo Starting CivicLens Map Service
echo ==========================================

cd backend
if not exist venv (
    echo [1/3] Creating Python virtual environment...
    python -m venv venv
)
echo [2/3] Activating venv and checking dependencies...
call venv\Scripts\activate.bat
pip install -r requirements.txt

echo [3/3] Starting backend (port 8002)...
start "CivicLens Map Backend" cmd /k "cd /d %~dp0backend & call venv\Scripts\activate.bat & python run_server.py --host 0.0.0.0 --port 8002 --reload"

cd ../frontend
echo [4/4] Starting frontend server (port 8085)...
start "CivicLens Map Frontend" cmd /k "cd /d %~dp0frontend & python -m http.server 8085"

echo.
echo All services started! Opening browser...
timeout 3 > nul
start "" "http://127.0.0.1:8085"
exit

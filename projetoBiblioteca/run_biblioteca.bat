@echo off
cd /d "%~dp0"
echo Iniciando o projeto projetoBiblioteca (perfil http, porta 5190)...
dotnet run --launch-profile http
pause

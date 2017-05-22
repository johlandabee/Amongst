@echo off
set CI=True
set CONFIGURATION=Release
set LABEL=Test

dotnet restore -v Minimal
dotnet build -c %CONFIGURATION% --no-dependencies --version-suffix adssada
dotnet test "sln\Amongst.Test\Amongst.Test.csproj" --no-build -c %CONFIGURATION%
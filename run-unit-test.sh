#!/bin/bash

dotnet test --logger:"junit;LogFilePath=/app/testout/{assembly}.xml" tests/Application.UnitTests/Application.UnitTests.csproj
dotnet test --logger:"junit;LogFilePath=/app/testout/{assembly}.xml" tests/Domain.UnitTests/Domain.UnitTests.csproj
dotnet test --logger:"junit;LogFilePath=/app/testout/{assembly}.xml" tests/Infrastructure.UnitTests/Infrastructure.UnitTests.csproj

chown -R $1 /app
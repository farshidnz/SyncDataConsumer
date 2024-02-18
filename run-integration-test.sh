#!/bin/bash

dotnet test --logger:"junit;LogFilePath=/app/inttestout/{assembly}.xml" tests/Application.IntegrationTests/Application.IntegrationTests.csproj

chown -R $1 /app
#!/bin/sh
set -e

./EfCoreMigrationsBundle

dotnet TodoApp.Web.dll

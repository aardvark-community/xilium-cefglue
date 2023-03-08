#!/bin/sh

dotnet tool restore
dotnet paket restore
dotnet build Xilium.CefGlue.slnf
@echo off

SET Config=Release

SET MSBuild=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe

%MSBuild% Carbonator.sln  /property:Configuration=%Config%

copy LICENSE.md %CD%\Carbonator\bin\Release\LICENSE.txt
copy README.md %CD%\Carbonator\bin\Release\README.txt
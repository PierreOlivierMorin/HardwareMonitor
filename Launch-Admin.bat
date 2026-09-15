@echo off
powershell -Command "Start-Process '%~dp0publish\HardwareMonitor.exe' -Verb RunAs"

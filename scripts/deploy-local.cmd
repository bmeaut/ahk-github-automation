@echo off
rem Convenience launcher for deploy-local.sh from a Windows terminal (or double-click), regardless of
rem the current working directory. Any arguments (e.g. --force-full) are passed through.
pushd "%~dp0\.."
wsl bash scripts/deploy-local.sh %*
popd

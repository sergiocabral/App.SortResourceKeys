cd /d "%~dp0"
%windir%\Microsoft.NET\Framework\v4.0.30319\csc.exe /out:SortResourceKeys.exe /target:exe Program.cs
pause

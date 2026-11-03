@echo off
setlocal
pushd "%~dp0"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:anycpu /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /win32manifest:app.manifest /win32icon:TH.ico /out:..\WH40K-Darktide-Thai-Mod.exe Manager.cs CompactTests.cs CategoryTests.cs FreshTests.cs RestoreEnglish.cs RestoreTests.cs
if errorlevel 1 goto failure
copy /y app.config ..\WH40K-Darktide-Thai-Mod.exe.config >nul
popd
exit /b 0
:failure
popd
exit /b 1

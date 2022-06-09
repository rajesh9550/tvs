:: Set application identifier - the same as in CMSApplicationName key in web.config (in case you don't have the key in web config, please use CMSApplicationGuid instead)
:: Admin password is used in this script only
:: Change path to InstallUtil.exe if necessary
SET _applicationIdentifier=<ApplicationName>
SET _adminPassword=<YourPassword>
SET _installUtilPath=D:\Windows\Microsoft.NET\Framework\v4.0.30319
:: Do not change the rest of the script

net user ServiceAdmin >nul && echo User Exists || (net user ServiceAdmin %_adminPassword% /add
net localgroup administrators ServiceAdmin /add
echo User Created)

:: Replace all '\' and '/' for '_' and add parentheses 
SET _application_service=%_applicationIdentifier:/=_%
SET _application_service=(%_application_service:\=_%)

%_installUtilPath%\installUtil /unattended /username=%COMPUTERNAME%\ServiceAdmin /password=%_adminPassword% /webpath="%RdRoleRoot%approot" HealthMonitoringService.exe > InstallHealthMonitoringService.txt
SET _service_name=KenticoCMSHealthMonitor%_application_service%
net start "%_service_name:~0,80%"

"HealthMonitoringService.exe" /createcounters /webpath="%cd%" > InstallHealthMonitoringService.txt

exit /B 0
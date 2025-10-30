dir bin\x86\Setup\en-us
rem pause

set release=Chem4Word-Setup.3.3.16.Release.14.msi

del setup.log
del remove.log

msiexec /i bin\x86\Setup\en-us\%release% /l*v setup.log

pause

rem msiexec /uninstall bin\x86\Setup\%release% /l*v remove.log

pause

rem find "Property(" setup.log > properties.log
rem search logs for "Calling custom action WiX.CustomAction"

dir bin\x86\Setup\
rem pause

set release=Chem4Word-Setup.3.3.12.Release.10.msi

del setup.log
del remove.log

msiexec /i bin\x86\Setup\%release% /l*v setup.log

pause

msiexec /uninstall bin\x86\Setup\%release% /l*v remove.log

pause

rem find "Property(" setup.log > properties.log
rem search logs for "Calling custom action WiX.CustomAction"

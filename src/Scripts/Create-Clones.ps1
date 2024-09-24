# Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted

$pwd = Split-Path -Path $MyInvocation.MyCommand.Path

$source = "$($pwd)\Chem4Word-Setup.3.3.8.Release.6.msi";

$targets = @();

# V2 Clones
$targets += "Chem4Word.Setup.2.0.1.0.Beta.2.msi"
$targets += "Chem4Word2.0.1.0 Beta 4 Setup.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.5.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.6.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.8.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.7.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.9.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.10.msi"
$targets += "Chem4Word.Setup.2.0.1.0.Beta.11.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2016.07.06.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2016.07.15.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2016.07.16.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2016.07.29.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2016.08.03.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2016.09.01.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2017.03.31.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2017.10.18.msi"
$targets += "Chem4Word.Setup.2.0.1.0.2017.10.25.msi"

# V3 Clones
$targets += "Chem4Word-Setup.3.0.14.Release.1.msi"
$targets += "Chem4Word-Setup.3.0.15.Release.2.msi"
$targets += "Chem4Word-Setup.3.0.16.Release.3.msi"
$targets += "Chem4Word-Setup.3.0.17.Release.4.msi"
$targets += "Chem4Word-Setup.3.0.18.Release.5.msi"
$targets += "Chem4Word-Setup.3.0.19.Release.6.msi"
$targets += "Chem4Word-Setup.3.0.20.Release.7.msi"
$targets += "Chem4Word-Setup.3.0.21.Release.8.msi"
$targets += "Chem4Word-Setup.3.0.22.Release.9.msi"
$targets += "Chem4Word-Setup.3.0.23.Release.10.msi"
$targets += "Chem4Word-Setup.3.0.24.Release.11.msi"
$targets += "Chem4Word-Setup.3.0.25.Release.12.msi"
$targets += "Chem4Word-Setup.3.0.26.Release.14.msi"
$targets += "Chem4Word-Setup.3.0.27.Release.15.msi"
$targets += "Chem4Word-Setup.3.0.28.Release.16.msi"
$targets += "Chem4Word-Setup.3.0.29.Release.17.msi"
$targets += "Chem4Word-Setup.3.0.30.Release.18.msi"
$targets += "Chem4Word-Setup.3.0.31.Release.19.msi"
$targets += "Chem4Word-Setup.3.0.32.Release.20.msi"
$targets += "Chem4Word-Setup.3.0.33.Release.21.msi"
$targets += "Chem4Word-Setup.3.0.34.Release.22.msi"
$targets += "Chem4Word-Setup.3.0.35.Release.23.msi"
$targets += "Chem4Word-Setup.3.0.36.Release.24.msi"
$targets += "Chem4Word-Setup.3.0.37.Release.25.msi"
$targets += "Chem4Word-Setup.3.0.38.Release.26.msi"
$targets += "Chem4Word-Setup.3.0.39.Release.27.msi"
$targets += "Chem4Word-Setup.3.0.40.Release.28.msi"
$targets += "Chem4Word-Setup.3.0.41.Release.29.msi"

# V3.1 Clones
$targets += "Chem4Word.Setup.3.1.0.Alpha.1.msi"
$targets += "Chem4Word.Setup.3.1.1.Beta.1.msi"
$targets += "Chem4Word.Setup.3.1.2.Beta.2.msi"
$targets += "Chem4Word.Setup.3.1.3.Beta.3.msi"
$targets += "Chem4Word.Setup.3.1.4.Beta.4.msi"
$targets += "Chem4Word.Setup.3.1.5.Beta.5.msi"
$targets += "Chem4Word.Setup.3.1.6.Beta.6.msi"
$targets += "Chem4Word.Setup.3.1.7.Beta.7.msi"
$targets += "Chem4Word.Setup.3.1.8.Beta.8.msi"
$targets += "Chem4Word.Setup.3.1.9.Beta.9.msi"
$targets += "Chem4Word.Setup.3.1.10.Beta.10.msi"
$targets += "Chem4Word.Setup.3.1.11.Release.1.msi"
$targets += "Chem4Word.Setup.3.1.12.Release.2.msi"
$targets += "Chem4Word.Setup.3.1.13.Release.3.msi"
$targets += "Chem4Word.Setup.3.1.14.Release.4.msi"
$targets += "Chem4Word.Setup.3.1.15.Release.5.msi"
$targets += "Chem4Word.Setup.3.1.16.Release.6.msi"
$targets += "Chem4Word.Setup.3.1.17.Release.7.msi"
$targets += "Chem4Word.Setup.3.1.18.Release.8.msi"
$targets += "Chem4Word.Setup.3.1.19.Release.9.msi"
$targets += "Chem4Word.Setup.3.1.20.Release.10.msi"
$targets += "Chem4Word.Setup.3.1.21.Release.11.msi"
$targets += "Chem4Word.Setup.3.1.22.Release.12.msi"
$targets += "Chem4Word.Setup.3.1.23.Release.13.msi"

$targets += "Chem4Word-Setup.3.1.Latest.msi"


foreach ($target in $targets)
{
    Copy-Item $source -Destination "$($pwd)\$($target)"; 
}
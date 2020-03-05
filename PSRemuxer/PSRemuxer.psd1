@{

RootModule = "PSRemuxer.psm1"

ModuleVersion = '1.0.0'

Description = "This package contains video and audio metadata editor PowerShell cmdlets."

PowerShellVersion = '5.0.0'

GUID = 'd58f1843-b331-480a-9b22-648aabe08119'

Author = 'Aoba xu'

CompanyName = 'Aoba-raws'

Copyright = 'Copyright (c) 2020 Aoba-raws'

CmdletsToExport = @(
    'New-SubRemux'
)

AliasesToExport = @(
    'muxsub'
)

PrivateData = @{
    PSData = @{
        LicenseUri = "https://raw.githubusercontent.com/Aoba-raws/PSRemuxer/master/LICENSE"

        ProjectUri = "https://github.com/Aoba-raws/PSRemuxer"
    }
}

HelpInfoUri="https://github.com/Aoba-raws/PSRemuxer"

}

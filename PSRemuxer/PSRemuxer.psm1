﻿Set-StrictMode -Version Latest

$PSModule = $ExecutionContext.SessionState.Module
$PSModuleRoot = $PSModule.ModuleBase

# Import the appropriate nested binary module based on the current PowerShell version
$binaryModuleRoot = $PSModuleRoot
if ($PSVersionTable["PSEdition"] -eq 'Core')
{
    $binaryModuleRoot = Join-Path -Path $PSModuleRoot -ChildPath 'coreclr'
}
else
{
    $binaryModuleRoot = Join-Path -Path $PSModuleRoot -ChildPath 'clr' 
}

$binaryModulePath = Join-Path -Path $binaryModuleRoot -ChildPath 'PSRemuxer.dll'
Import-Module -Name $binaryModulePath -PassThru
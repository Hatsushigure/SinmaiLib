<#
.SYNOPSIS
Initializes or verifies the lock file for the required managed assemblies.

.EXAMPLE
.\scripts\Manage-AssemblyLock.ps1 -Mode Initialize -ManagedDirectory "D:\Game\Package\Sinmai_Data\Managed"

.EXAMPLE
.\scripts\Manage-AssemblyLock.ps1 -Mode Verify -ManagedDirectory "D:\Game\Package\Sinmai_Data\Managed"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateSet("Initialize", "Verify")]
    [string] $Mode,

    [Parameter(Mandatory, Position = 1)]
    [ValidateNotNullOrEmpty()]
    [string] $ManagedDirectory
)

$ErrorActionPreference = "Stop"

$assemblyNames = @(
    "Assembly-CSharp.dll"
    "AMDaemon.NET.dll"
    "ChimeLib.NET.dll"
)
$lockFilePath = Join-Path (Split-Path $PSScriptRoot -Parent) "managed-assemblies.lock.json"

function Get-AssemblyRecord {
    param(
        [Parameter(Mandatory)]
        [string] $Directory,

        [Parameter(Mandatory)]
        [string] $Name
    )

    $file = Get-Item -LiteralPath (Join-Path $Directory $Name) -ErrorAction Stop
    [ordered]@{
        Name   = $Name
        Size   = $file.Length
        Sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    }
}

if ($Mode -eq "Initialize") {
    $records = foreach ($assemblyName in $assemblyNames) {
        Get-AssemblyRecord -Directory $ManagedDirectory -Name $assemblyName
    }

    [ordered]@{ Assemblies = $records } |
        ConvertTo-Json -Depth 3 |
        Set-Content -LiteralPath $lockFilePath -Encoding utf8

    Write-Host "Initialized assembly lock file: $lockFilePath"
    return
}

$lock = Get-Content -LiteralPath $lockFilePath -Raw -ErrorAction Stop | ConvertFrom-Json
foreach ($assemblyName in $assemblyNames) {
    $expected = $lock.Assemblies | Where-Object Name -CEQ $assemblyName
    $actual = Get-AssemblyRecord -Directory $ManagedDirectory -Name $assemblyName

    if ($null -eq $expected -or
        [long] $expected.Size -ne $actual.Size -or
        [string] $expected.Sha256 -cne $actual.Sha256) {
        throw "Assembly does not match the lock file: $assemblyName"
    }
}

Write-Host "All managed assemblies match the lock file."

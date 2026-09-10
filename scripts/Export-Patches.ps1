<#
.SYNOPSIS
Exports development commits for the current version as patch files.

.EXAMPLE
.\scripts\Export-Patches.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
if (Test-Path Variable:PSNativeCommandUseErrorActionPreference) {
    $PSNativeCommandUseErrorActionPreference = $false
}

. (Join-Path $PSScriptRoot "Git-Version.ps1")

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)]
        [string] $FilePath,

        [Parameter()]
        [string[]] $ArgumentList = @()
    )

    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($ArgumentList -join ' ')"
    }
}

$versionContext = Get-GitVersionContext -RepositoryPath $PSScriptRoot
$repositoryRoot = $versionContext.RepositoryRoot
$version = $versionContext.Version

$devBranchName = "dev/$version"
$decompBranchName = "decomp/$version"
foreach ($branchName in @($devBranchName, $decompBranchName)) {
    & git -C $repositoryRoot show-ref --verify --quiet "refs/heads/$branchName"
    if ($LASTEXITCODE -ne 0) {
        throw "Required local branch does not exist: $branchName"
    }
}

$patchDirectory = Join-Path $repositoryRoot "patches"
$null = New-Item -ItemType Directory -Path $patchDirectory -Force
$revisionRange = "$decompBranchName..$devBranchName"

Invoke-CheckedCommand git @(
    "-C", $repositoryRoot,
    "format-patch",
    "-p", "-U0", "-N", "-D", "-B",
    "--zero-commit",
    "--output-directory", $patchDirectory,
    $revisionRange
)

Write-Host "Exported patches for version '$version' to: $patchDirectory"

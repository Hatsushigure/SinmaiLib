<#
.SYNOPSIS
Applies exported patches to a version branch and updates its development branch.

.DESCRIPTION
Run this script while checked out on a version-number branch (for example,
`1.56`). The matching `dev/<version>` and `decomp/<version>` branches must
point at the same commit before the first patch is applied.

.EXAMPLE
.\scripts\Apply-Patches.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
if (Test-Path Variable:PSNativeCommandUseErrorActionPreference) {
    $PSNativeCommandUseErrorActionPreference = $false
}
. (Join-Path $PSScriptRoot "Script-Helpers.ps1")

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

$devCommit = ([string](& git -C $repositoryRoot rev-parse $devBranchName)).Trim()
$decompCommit = ([string](& git -C $repositoryRoot rev-parse $decompBranchName)).Trim()
if ($LASTEXITCODE -ne 0 -or $devCommit -ne $decompCommit) {
    throw "Branches '$devBranchName' and '$decompBranchName' must point to the same commit."
}

$patchDirectory = Join-Path $repositoryRoot "patches"
$orderPath = Join-Path $patchDirectory "order.txt"
if (-not (Test-Path -LiteralPath $orderPath)) {
    throw "Patch order file does not exist: $orderPath"
}
$patchNumbers = @(Get-Content -LiteralPath $orderPath | ForEach-Object {
    $line = $_.Trim()
    if ($line.Length -gt 0) {
        if ($line -cnotmatch '^\d+$') { throw "Invalid patch number in order.txt: $line" }
        $line
    }
})

$worktreePath = Join-Path ([System.IO.Path]::GetTempPath()) "SinmaiLib-apply-$version-$([guid]::NewGuid().ToString('N'))"
$worktreeCreated = $false
try {
    Invoke-CheckedCommand git @("-C", $repositoryRoot, "worktree", "add", $worktreePath, $devBranchName)
    $worktreeCreated = $true
    $devStatus = @(& git -C $worktreePath status --porcelain)
    if ($LASTEXITCODE -ne 0) { throw "Unable to inspect the dev worktree." }
    if ($devStatus.Count -gt 0) {
        throw "The dev branch worktree must be clean before applying patches."
    }

    foreach ($patchNumber in $patchNumbers) {
        $patchPath = Join-Path $patchDirectory $patchNumber
        $mainPatchPath = Join-Path $patchPath "main.patch"
        $deletePath = Join-Path $patchPath "delete.txt"
        $addListPath = Join-Path $patchPath "add.txt"
        $addPath = Join-Path $patchPath "add"
        foreach ($requiredPath in @($mainPatchPath, $deletePath, $addListPath)) {
            if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
                throw "Missing patch file for '$patchNumber': $requiredPath"
            }
        }

        Write-Host "Applying patch $patchNumber..."
        Invoke-CheckedCommand git @("-C", $worktreePath, "am", $mainPatchPath)

        $deleted = @(Get-Content -LiteralPath $deletePath | ForEach-Object {
            if ($_.Trim().Length -gt 0) { $_ }
        })
        if ($deleted.Count -gt 0) {
            Invoke-CheckedCommand git (@("-C", $worktreePath, "rm", "--") + $deleted)
        }

        $added = @(Get-Content -LiteralPath $addListPath | ForEach-Object {
            if ($_.Trim().Length -gt 0) { $_ }
        })
        foreach ($relativePath in $added) {
            $source = Join-Path $addPath $relativePath
            $destination = Join-Path $worktreePath $relativePath
            if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
                throw "Added file is missing from '$patchNumber/add': $relativePath"
            }
            $destinationParent = Split-Path -Parent $destination
            $null = New-Item -ItemType Directory -Path $destinationParent -Force
            Copy-Item -LiteralPath $source -Destination $destination -Force
        }

        if ($added.Count -gt 0) {
            Invoke-CheckedCommand git (@("-C", $worktreePath, "add", "--") + $added)
        }
        Invoke-CheckedCommand git @("-C", $worktreePath, "commit", "--amend", "--no-edit", "--allow-empty")
    }
}
finally {
    if ($worktreeCreated) {
        Invoke-CheckedCommand git @("-C", $repositoryRoot, "worktree", "remove", "--force", $worktreePath)
    }
}

Write-Host "Applied $($patchNumbers.Count) patch(es) for version '$version'."

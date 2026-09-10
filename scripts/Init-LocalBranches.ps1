<#
.SYNOPSIS
Creates the local decomp and development branches for an SDGB version.

.EXAMPLE
.\scripts\Init-LocalBranches.ps1 -ManagedDirectory "D:\Game\Package\Sinmai_Data\Managed"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateNotNullOrEmpty()]
    [string] $ManagedDirectory
)

$ErrorActionPreference = "Stop"
if (Test-Path Variable:PSNativeCommandUseErrorActionPreference) {
    $PSNativeCommandUseErrorActionPreference = $false
}

. (Join-Path $PSScriptRoot "Script-Helpers.ps1")

$versionContext = Get-GitVersionContext -RepositoryPath $PSScriptRoot
$repositoryRoot = $versionContext.RepositoryRoot
$version = $versionContext.Version

$decompBranchName = "decomp/$version"
$devBranchName = "dev/$version"
foreach ($branchName in @($decompBranchName, $devBranchName)) {
    & git -C $repositoryRoot show-ref --verify --quiet "refs/heads/$branchName"
    if ($LASTEXITCODE -eq 0) {
        throw "Branch already exists: $branchName"
    }
}

$managedPath = (Resolve-Path -LiteralPath $ManagedDirectory -ErrorAction Stop).Path
& (Join-Path $PSScriptRoot "Manage-AssemblyLock.ps1") -Mode Verify -ManagedDirectory $managedPath

$assemblyNames = @(
    "Assembly-CSharp.dll"
    "AMDaemon.NET.dll"
    "ChimeLib.NET.dll"
)

$assemblyPaths = foreach ($assemblyName in $assemblyNames) {
    Join-Path $managedPath $assemblyName
}

$worktreePath = Join-Path ([System.IO.Path]::GetTempPath()) "SinmaiLib-decomp-$version-$([guid]::NewGuid().ToString('N'))"
$worktreeCreated = $false
$completed = $false

try {
    Invoke-CheckedCommand dotnet @("tool", "restore")
    Invoke-CheckedCommand git @("-C", $repositoryRoot, "worktree", "add", "--orphan", "-b", $decompBranchName, $worktreePath)
    $worktreeCreated = $true

    $sourcePath = Join-Path $worktreePath "src"
    $null = New-Item -ItemType Directory -Path $sourcePath -Force

    foreach ($assemblyPath in $assemblyPaths) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($assemblyPath)
        $outputPath = Join-Path $sourcePath $projectName
        Invoke-CheckedCommand dotnet @(
            "tool", "run", "ilspycmd",
            "--",
            "--nested-directories",
            "--project",
            "--outputdir", $outputPath,
            "--referencepath", $managedPath,
            $assemblyPath
        ) $repositoryRoot
    }

    Invoke-CheckedCommand dotnet @(
        "tool", "run", "csharpier",
        "--",
        "format", $sourcePath
    ) $repositoryRoot

    Get-ChildItem -LiteralPath $sourcePath -Recurse -File |
        Where-Object { $_.Extension -in @(".sln", ".csproj") } |
        Remove-Item -Force

    Invoke-CheckedCommand git @("-C", $worktreePath, "add", "--all", "--", "src")
    & git -C $worktreePath diff --cached --quiet --exit-code
    if ($LASTEXITCODE -eq 0) {
        throw "Decompilation produced no changes to commit."
    }
    if ($LASTEXITCODE -ne 1) {
        throw "Unable to inspect staged changes (git diff exit code $LASTEXITCODE)."
    }

    Invoke-CheckedCommand git @(
        "-C", $worktreePath,
        "commit", "-m", "init: Add SDGB $version decompile code"
    )
    Invoke-CheckedCommand git @(
        "-C", $repositoryRoot,
        "branch", $devBranchName, $decompBranchName
    )

    $completed = $true
}
finally {
    if ($worktreeCreated -and $completed) {
        Invoke-CheckedCommand git @("-C", $repositoryRoot, "worktree", "remove", $worktreePath)
    }
    elseif ($worktreeCreated) {
        Write-Warning "The operation failed. The worktree was kept for inspection: $worktreePath"
    }
}

Write-Host "Created branches '$decompBranchName' and '$devBranchName'."

<#
.SYNOPSIS
Exports development commits for the current version as patch files.

.EXAMPLE
.\scripts\Export-Patches.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
if (Test-Path Variable:PSNativeCommandUseErrorActionPreference)
{
    $PSNativeCommandUseErrorActionPreference = $false 
}
. (Join-Path $PSScriptRoot "Script-Helpers.ps1")

$versionContext = Get-GitVersionContext -RepositoryPath $PSScriptRoot
$repositoryRoot = $versionContext.RepositoryRoot
$version = $versionContext.Version
$devBranchName = "dev/$version"
$decompBranchName = "decomp/$version"
foreach ($branchName in @($devBranchName, $decompBranchName))
{
    & git -C $repositoryRoot show-ref --verify --quiet "refs/heads/$branchName"
    if ($LASTEXITCODE -ne 0)
    {
        throw "Required local branch does not exist: $branchName" 
    }
}

$patchDirectory = Join-Path $repositoryRoot "patches"
$null = New-Item -ItemType Directory -Path $patchDirectory -Force
$orderPath = Join-Path $patchDirectory "order.txt"
$orderLines = if (Test-Path -LiteralPath $orderPath)
{
    @(Get-Content -LiteralPath $orderPath) 
}
else
{
    @() 
}
$lastOrderLine = $orderLines | Where-Object { $_.Trim().Length -gt 0 } | Select-Object -Last 1
if ($null -eq $lastOrderLine)
{
    $lastNumber = 0
}
elseif ($lastOrderLine.Trim() -cmatch '^\d+$')
{
    $lastNumber = [int]$lastOrderLine.Trim()
}
else
{
    throw "The last non-empty line in order.txt is not a patch number: $lastOrderLine"
}
$nextNumber = $lastNumber + 1
$commits = @(& git -C $repositoryRoot rev-list --reverse "$decompBranchName..$devBranchName")
if ($LASTEXITCODE -ne 0)
{
    throw "Unable to enumerate commits between branches." 
}
$startIndex = $nextNumber - 1
if ($startIndex -ge $commits.Count)
{
    Write-Host "No new commits to export for version '$version'."; return 
}

$worktreePath = Join-Path ([System.IO.Path]::GetTempPath()) "SinmaiLib-export-$version-$([guid]::NewGuid().ToString('N'))"
$worktreeCreated = $false

try
{
    Invoke-CheckedCommand git @("-C", $repositoryRoot, "worktree", "add", "--detach", $worktreePath, $devBranchName)
    $worktreeCreated = $true

    for ($index = $startIndex; $index -lt $commits.Count; $index++)
    {
        $commit = ([string]$commits[$index]).Trim()
        $parent = ([string](& git -C $repositoryRoot rev-parse "$commit^" )).Trim()
        if ($LASTEXITCODE -ne 0 -or -not $parent)
        {
            throw "Unable to resolve parent commit for $commit." 
        }
        $number = $nextNumber + ($index - $startIndex)
        $patchPath = Join-Path $patchDirectory ('{0:D4}' -f $number)
        $addPath = Join-Path $patchPath 'add'
        $null = New-Item -ItemType Directory -Path $addPath -Force

        $deleted = @(& git -C $repositoryRoot diff -B '--diff-filter=D' --name-only $parent $commit)
        if ($LASTEXITCODE -ne 0)
        {
            throw "Unable to enumerate deleted files for $commit." 
        }
        [System.IO.File]::WriteAllText(
            (Join-Path $patchPath 'delete.txt'), 
            ([string]::Join("`n", $deleted) + "`n"),
            [System.Text.UTF8Encoding]($false)
        )

        $added = @(& git -C $repositoryRoot diff -B '--diff-filter=A' --name-only $parent $commit)
        if ($LASTEXITCODE -ne 0)
        {
            throw "Unable to enumerate added files for $commit." 
        }
        [System.IO.File]::WriteAllText(
            (Join-Path $patchPath 'add.txt'), 
            ([string]::Join("`n", $added) + "`n"),
            [System.Text.UTF8Encoding]($false)
        )

        Invoke-CheckedCommand git @("-C", $worktreePath, "checkout", "--detach", $commit)
        foreach ($relativePath in $added)
        {
            $source = Join-Path $worktreePath $relativePath
            $destination = Join-Path $addPath $relativePath
            if (-not (Test-Path -LiteralPath $source -PathType Leaf))
            {
                throw "Added file is missing from commit ${commit}: $relativePath"
            }
            $destinationParent = Split-Path -Parent $destination
            $null = New-Item -ItemType Directory -Path $destinationParent -Force
            Copy-Item -LiteralPath $source -Destination $destination -Force
        }

        $patchFilePath = Join-Path $patchPath "main.patch"
        Invoke-CheckedCommand git @(
            '-C', $repositoryRoot, 
            'format-patch', 
            '-p', '-B', 
            '--diff-filter=ad', 
            '--zero-commit', 
            '--no-signature', 
            "--output=$patchFilePath", 
            "$parent..$commit"
        )
        if (-not (Test-Path -LiteralPath $patchFilePath))
        {
            throw "No patch was generated for commit $commit." 
        }

        [System.IO.File]::AppendAllText(
            $orderPath, 
            ("{0:D4}`n" -f $number), 
            [System.Text.UTF8Encoding]($false)
        )
    }
}
finally
{
    if ($worktreeCreated)
    {
        Invoke-CheckedCommand git @("-C", $repositoryRoot, "worktree", "remove", "--force", $worktreePath)
    }
}

Write-Host "Exported patches for version '$version' to: $patchDirectory"

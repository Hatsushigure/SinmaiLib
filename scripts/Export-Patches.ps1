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

$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("sinmailib-patches-" + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $temporaryRoot -Force
try
{
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
        Set-Content -LiteralPath (Join-Path $patchPath 'delete.txt') -Value $deleted -Encoding utf8

        $added = @(& git -C $repositoryRoot diff -B '--diff-filter=A' --name-only $parent $commit)
        if ($LASTEXITCODE -ne 0)
        {
            throw "Unable to enumerate added files for $commit." 
        }
        Set-Content -LiteralPath (Join-Path $patchPath 'add.txt') -Value $added -Encoding utf8

        if ($added.Count -gt 0)
        {
            $archivePath = Join-Path $temporaryRoot ("$number.tar")
            $archiveArguments = @(
                '-C', $repositoryRoot, 
                'archive', 
                "--output=$archivePath", 
                $commit, 
                '--'
            ) + $added
            Invoke-CheckedCommand git $archiveArguments
            Invoke-CheckedCommand tar @('-xf', $archivePath, '-C', $addPath)
            Remove-Item -LiteralPath $archivePath -Force
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
        Add-Content -LiteralPath $orderPath -Value ('{0:D4}' -f $number) -Encoding utf8
    }
}
finally
{
    if (Test-Path -LiteralPath $temporaryRoot)
    {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force 
    }
}
Write-Host "Exported patches for version '$version' to: $patchDirectory"

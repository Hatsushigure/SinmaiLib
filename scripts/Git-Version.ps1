function Get-GitVersionContext {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $RepositoryPath
    )

    $repositoryRoot = [string] (& git -C $RepositoryPath rev-parse --show-toplevel)
    if ($LASTEXITCODE -ne 0) {
        throw "The script must be run from a Git repository."
    }
    $repositoryRoot = [System.IO.Path]::GetFullPath($repositoryRoot.Trim())

    $version = [string] (& git -C $repositoryRoot branch --show-current)
    if ($LASTEXITCODE -ne 0 -or -not $version) {
        throw "The repository must be on a branch; detached HEAD is not supported."
    }
    $version = $version.Trim()

    if ($version.StartsWith("dev", [System.StringComparison]::OrdinalIgnoreCase) -or
        $version.StartsWith("decomp", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "The current branch must not start with 'dev' or 'decomp': $version"
    }
    if ($version -cnotmatch '^[A-Za-z0-9._-]+$') {
        throw "The current branch is not a valid version: $version"
    }

    [pscustomobject]@{
        RepositoryRoot = $repositoryRoot
        Version        = $version
    }
}

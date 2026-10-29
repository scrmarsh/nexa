$ErrorActionPreference = 'Stop'
$env:GIT_TERMINAL_PROMPT = '0'
$env:GCM_INTERACTIVE = 'Never'
$credentialLines = "protocol=https`nhost=github.com`n`n" | git credential fill 2>$null
if ($LASTEXITCODE -ne 0) { throw 'Configured Git credential unavailable' }
$fields = @{}
foreach ($line in $credentialLines) {
    if ($line -match '^([^=]+)=(.*)$') { $fields[$matches[1]] = $matches[2] }
}
if (!$fields['password']) { throw 'No configured Git token returned' }
$headers = @{ Authorization = "Bearer $($fields['password'])"; Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28' }
$base = 'https://api.github.com/repos/scrmarsh/nexa'
$plan = Get-Content "$PSScriptRoot/../docs/NEXA_031_REMOTE_CHANGE_PLAN.json" -Raw | ConvertFrom-Json
function Get-RefOrNull($tag) {
    try { return Invoke-RestMethod "$base/git/ref/tags/$tag" -Headers $headers }
    catch { if ([int]$_.Exception.Response.StatusCode -eq 404) { return $null }; throw }
}
function Assert-Preserved($live, $change) {
    if ($live.id -ne $change.releaseId -or $live.name -cne $change.expectedBefore.name -or $live.tag_name -cne $change.legacyTag) { throw "Release identity changed: $($change.legacyTag)" }
    if ([datetime]$live.published_at -ne [datetime]$change.expectedBefore.published_at -or !$live.prerelease -or $live.draft) { throw 'Release publication state changed' }
    if ($live.assets.Count -ne $change.expectedBefore.assets.Count) { throw 'Asset count changed' }
    foreach ($expected in $change.expectedBefore.assets) {
        $asset = @($live.assets | Where-Object id -eq $expected.id)
        if ($asset.Count -ne 1) { throw 'Asset ID changed' }
        foreach ($property in @('name','size','digest','browser_download_url')) {
            if ($asset[0].$property -cne $expected.$property) { throw "Asset $property changed" }
        }
    }
}
# Validate every baseline before the first remote write. Accept exact target bodies
# on rerun so an interrupted operation can finish without duplicating anything.
foreach ($change in $plan.changes) {
    $live = Invoke-RestMethod "$base/releases/$($change.releaseId)" -Headers $headers
    Assert-Preserved $live $change
    if ($live.body -cne $change.expectedBefore.body -and $live.body -cne $change.releasePatch.body) { throw 'Release description changed since approval' }
    $legacy = Get-RefOrNull $change.legacyTag
    if ($legacy.object.type -ne 'commit' -or $legacy.object.sha -cne $change.commit) { throw 'Legacy tag target changed' }
    $canonical = Get-RefOrNull $change.canonicalTag
    if ($canonical -and ($canonical.object.type -ne 'commit' -or $canonical.object.sha -cne $change.commit)) { throw 'Canonical tag already targets a different object' }
}
$results = @()
foreach ($change in $plan.changes) {
    if (!(Get-RefOrNull $change.canonicalTag)) {
        $null = Invoke-RestMethod "$base/git/refs" -Method Post -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes(($change.createRef | ConvertTo-Json -Compress)))
    }
    $live = Invoke-RestMethod "$base/releases/$($change.releaseId)" -Headers $headers
    if ($live.body -cne $change.releasePatch.body) {
        $null = Invoke-RestMethod "$base/releases/$($change.releaseId)" -Method Patch -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes(($change.releasePatch | ConvertTo-Json -Compress)))
    }
    $after = Invoke-RestMethod "$base/releases/$($change.releaseId)" -Headers $headers
    Assert-Preserved $after $change
    if ($after.body -cne $change.releasePatch.body) { throw 'Description verification failed' }
    foreach ($tag in @($change.legacyTag, $change.canonicalTag)) {
        $ref = Get-RefOrNull $tag
        if ($ref.object.sha -cne $change.commit) { throw 'Tag verification failed' }
    }
    $results += [ordered]@{ releaseId = $after.id; url = $after.html_url; canonicalTag = $change.canonicalTag; legacyTag = $after.tag_name; commit = $change.commit; published_at = $after.published_at; assets = $change.expectedBefore.assets; descriptionVerified = $true; assetsPreserved = $true }
    Write-Output "Verified correction: $($change.canonicalTag); original release and assets preserved"
}
[ordered]@{ appliedAtUtc = [datetime]::UtcNow.ToString('o'); authorization = 'User: ok publish, approving the prepared canonical tag aliases and release description corrections'; newApplicationReleasePublished = $false; results = $results } | ConvertTo-Json -Depth 10 | Set-Content "$PSScriptRoot/../docs/verification/nexa031/github-corrections-applied.json" -Encoding utf8

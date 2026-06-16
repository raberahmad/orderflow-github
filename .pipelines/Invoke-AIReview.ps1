param(
    [Parameter(Mandatory=$true)] [string]$OrganizationUrl,
    [Parameter(Mandatory=$true)] [string]$Project,
    [Parameter(Mandatory=$true)] [string]$RepositoryId,
    [Parameter(Mandatory=$true)] [string]$PullRequestId,
    [Parameter(Mandatory=$true)] [string]$AoaiEndpoint,
    [Parameter(Mandatory=$true)] [string]$AoaiKey,
    [Parameter(Mandatory=$true)] [string]$AoaiDeployment,
    [Parameter(Mandatory=$true)] [int]$MaxTokens
)

$ErrorActionPreference = "Stop"

# Base URLs used throughout the script
$adoBase   = "$OrganizationUrl/$Project/_apis/git/repositories/$RepositoryId"
$prBase    = "$adoBase/pullRequests/$PullRequestId"
$threadUrl = "$prBase/threads?api-version=7.1"

# ------------------------------------------------
# Helper: Azure DevOps authentication header
# Uses the pipeline built-in token - no PAT needed
# ------------------------------------------------
function Get-AdoHeaders {
    $token   = $env:SYSTEM_ACCESSTOKEN
    $encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$token"))
    return @{ Authorization = "Basic $encoded"; "Content-Type" = "application/json" }
}

# ------------------------------------------------
# Helper: Claude on Azure Foundry authentication header
# ------------------------------------------------
function Get-ClaudeHeaders {
    return @{
        "Authorization"     = "Bearer $AoaiKey"
        "Content-Type"      = "application/json"
        "anthropic-version" = "2023-06-01"
    }
}

# ------------------------------------------------
# Helper: Post a general comment to the PR
# ------------------------------------------------
function Post-PrComment($content) {
    $body = @{
        comments = @(@{ parentCommentId = 0; content = $content; commentType = 1 })
        status   = "active"
    } | ConvertTo-Json -Depth 10
    Invoke-RestMethod -Uri $threadUrl -Headers (Get-AdoHeaders) -Method Post -Body $body | Out-Null
}

# ------------------------------------------------
# Helper: Post an inline comment on a specific file and line
# Falls back to a general comment if the line is not in the diff
# ------------------------------------------------
function Post-InlineComment($content, $filePath, $line) {
    $body = @{
        comments = @(@{ parentCommentId = 0; content = $content; commentType = 1 })
        threadContext = @{
            filePath       = $filePath
            rightFileStart = @{ line = $line; offset = 1 }
            rightFileEnd   = @{ line = $line; offset = 1 }
        }
        status = "active"
    } | ConvertTo-Json -Depth 10

    try {
        Invoke-RestMethod -Uri $threadUrl -Headers (Get-AdoHeaders) -Method Post -Body $body | Out-Null
        Write-Host "  Posted inline on $filePath line $line"
    }
    catch {
        Write-Warning "  Could not post inline on line $line - posting as general comment"
        Post-PrComment "$content`n_(file: $filePath line: $line)_"
    }
}

# ================================================
# STEP 1: Get the list of changed files in the PR
# ================================================
Write-Host "Step 1: Fetching changed files..."

$adoHeaders  = Get-AdoHeaders
$iterations  = Invoke-RestMethod -Uri "$prBase/iterations?api-version=7.1" -Headers $adoHeaders -Method Get
$iterationId = ($iterations.value | Select-Object -Last 1).id
Write-Host "  PR iteration: $iterationId"

$changes        = Invoke-RestMethod -Uri "$prBase/iterations/$iterationId/changes?api-version=7.1" -Headers $adoHeaders -Method Get
$changedEntries = $changes.changeEntries | Where-Object { $_.changeType -ne "delete" -and $_.item.path -ne $null }

if ($changedEntries.Count -eq 0) {
    Write-Host "No changed files found. Skipping review."
    exit 0
}

Write-Host "  Changed files:"
$changedEntries | ForEach-Object { Write-Host "    - $($_.item.path)" }

# ================================================
# STEP 2: Fetch the content of each changed file
# Line numbers are added so Claude can reference exact locations
# ================================================
Write-Host "Step 2: Fetching file contents..."

$skipExtensions = '\.(png|jpg|gif|ico|pdf|zip|dll|exe|pdb|nupkg)$'
$fileContents   = @()

foreach ($entry in $changedEntries) {
    $filePath = $entry.item.path
    $objectId = $entry.item.objectId

    if ($filePath -match $skipExtensions) {
        Write-Host "  Skipping binary: $filePath"
        continue
    }

    try {
        $content = Invoke-RestMethod -Uri "$adoBase/blobs/$objectId`?api-version=7.1" -Headers $adoHeaders -Method Get
        $lines   = $content -split "`n"

        # Prefix each line with its number: "  42: code here"
        $numbered = ($lines | ForEach-Object -Begin { $i = 1 } -Process { "{0,4}: {1}" -f $i++, $_ }) -join "`n"
        $fileContents += "### File: $filePath`n``````csharp`n$numbered`n```````n"
        Write-Host "  Fetched: $filePath ($($lines.Count) lines)"
    }
    catch {
        Write-Warning "  Could not fetch $filePath"
    }
}

if ($fileContents.Count -eq 0) {
    Post-PrComment "**AI Code Review**`n`nNo source files could be fetched for this PR. Check pipeline logs."
    exit 0
}

# ================================================
# STEP 3: Send files to Claude for review
# ================================================
Write-Host "Step 3: Calling Claude ($AoaiDeployment)..."

$systemPrompt = @"
You are a senior C# code reviewer for an ASP.NET Web API project on .NET 10.

Project: REST API using the Repository pattern and ServiceResult<T> return type.
This pipeline uses Claude on Azure AI Foundry which uses the Anthropic API format
(/anthropic/v1/messages with Bearer token and anthropic-version header). Do not flag
this as an Azure OpenAI format mismatch - it is intentional and correct.

Review the ENTIRE file content provided - not just the changed lines.
Flag both pre-existing issues and new issues introduced by this PR.

Focus on:
1. Bugs and logic errors (race conditions, null refs)
2. Security issues (SQL injection, missing auth, exposed secrets)
3. Error handling and null safety
4. Missing input validation
5. GDPR/PII concerns in logging
6. Async/await correctness

Severity levels:
- Critical: crashes, data loss, security breach, or race conditions
- Major: wrong behaviour, missing validation, or PII leak
- Minor: code quality or style improvements

IMPORTANT: Respond ONLY with a valid JSON object.
No markdown, no code blocks, no backticks, no non-ASCII characters.
Use a regular hyphen (-) instead of an em dash.

JSON structure:
{
  "summary": "Brief assessment of the PR",
  "issues": [
    {
      "severity": "Critical|Major|Minor",
      "file": "/path/to/file.cs",
      "line": 42,
      "isNewIssue": true,
      "description": "What the issue is and exactly how to fix it"
    }
  ],
  "suggestions": ["Optional non-critical improvement"]
}

Line number rules:
- Use the exact number shown before the colon in the file content (e.g. "  42: code" means line 42)
- isNewIssue = true if introduced in this PR, false if pre-existing
"@

$requestParams = @{
    model      = $AoaiDeployment
    system     = $systemPrompt
    messages   = @(@{ role = "user"; content = "Review this PR:`n`n$($fileContents -join "`n---`n")" })
    max_tokens = [int]4000
}

# Opus 4.7 and 4.8 use adaptive thinking - temperature is deprecated
if ($AoaiDeployment -notmatch "opus-4-[78]") {
    $requestParams["temperature"] = [float]0.2
}

$requestBody = $requestParams | ConvertTo-Json -Depth 10

try {
    $response  = Invoke-RestMethod -Uri "$($AoaiEndpoint.TrimEnd('/'))/anthropic/v1/messages" -Headers (Get-ClaudeHeaders) -Method Post -Body $requestBody
    $rawReview = $response.content[0].text
    Write-Host "  Review received."
}
catch {
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        Write-Host "  Claude API error: $($reader.ReadToEnd())"
    } else {
        Write-Host "  Claude API error: $($_.Exception.Message)"
    }
    throw
}

# ================================================
# STEP 4: Parse Claude's JSON response
# ================================================
Write-Host "Step 4: Parsing response..."

try {
    $review = ($rawReview -replace '(?s)^```json\s*' -replace '(?s)^```\s*' -replace '```\s*$').Trim() | ConvertFrom-Json
    Write-Host "  Parsed OK - $($review.issues.Count) issue(s) found"
}
catch {
    Write-Warning "  Could not parse JSON - posting raw response as comment"
    Post-PrComment "**AI Code Review**`n`n$rawReview"
    exit 0
}

# ================================================
# STEP 5: Post summary comment to the PR
# ================================================
Write-Host "Step 5: Posting summary..."

$critical = ($review.issues | Where-Object severity -eq "Critical").Count
$major    = ($review.issues | Where-Object severity -eq "Major").Count
$minor    = ($review.issues | Where-Object severity -eq "Minor").Count
$newCount = ($review.issues | Where-Object isNewIssue -eq $true).Count
$oldCount = ($review.issues | Where-Object isNewIssue -eq $false).Count

$minorLines = ($review.issues | Where-Object severity -eq "Minor" | ForEach-Object {
    $tag = if ($_.isNewIssue) { "[NEW]" } else { "[PRE-EXISTING]" }
    "- $tag $($_.file) line $($_.line): $($_.description)"
}) -join "`n"

$suggestions = ($review.suggestions | ForEach-Object { "- $_" }) -join "`n"

$summary  = "**AI Code Review - $AoaiDeployment**`n`n"
$summary += "**Summary**`n$($review.summary)`n`n"
$summary += "**Issues Found:** $($review.issues.Count) total - $critical Critical, $major Major, $minor Minor`n"
$summary += "$newCount new in this PR | $oldCount pre-existing`n`n"

if ($minorLines) {
    $summary += "**Minor Issues**`n$minorLines`n`n"
}

if ($suggestions) {
    $summary += "**Suggestions**`n$suggestions"
}

Post-PrComment $summary
Write-Host "  Summary posted."

# ================================================
# STEP 6: Post inline comments for Critical and Major
# Minor issues are listed in the summary above
# ================================================
Write-Host "Step 6: Posting inline comments..."

$review.issues | Where-Object { $_.severity -in @("Critical", "Major") } | ForEach-Object {
    $tag     = if ($_.isNewIssue) { "[NEW]" } else { "[PRE-EXISTING]" }
    $content = "**[$($_.severity)]** $tag - $($_.description)"
    Post-InlineComment $content $_.file ([int]$_.line)
}

Write-Host "Review completed for PR #$PullRequestId"
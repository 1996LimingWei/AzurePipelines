$retryCount = 3
$retryDelaySeconds = 5

$summaryFilePath = "$(System.DefaultWorkingDirectory)/_GenderDebias-PullRequest/drop_linux_stage_linux_job/PerformanceEvaluation/Output/Summary.txt"

$retryAttempts = 0

while ($retryAttempts -lt $retryCount) {
    # Check if Summary.txt file exists
    if (-not (Test-Path $summaryFilePath)) {
        # Run the dotnet command
        dotnet GenderDebias.PerformanceEvaluation.dll prod Output "$(System.DefaultWorkingDirectory)/_GenderDebias-PullRequest/drop_linux_stage_linux_job/test_sets" 1
        Start-Sleep -Seconds $retryDelaySeconds
        $retryAttempts++
    }
    else {
        # Summary.txt file exists, break the loop
        break
    }
}

# If all retry attempts failed or Summary.txt file still doesn't exist, throw an error
if (($retryAttempts -eq $retryCount -and $retryException) -or (-not (Test-Path $summaryFilePath))) {
    throw "Failed to generate Summary.txt file."
}

$filePath = "$(System.DefaultWorkingDirectory)/_GenderDebias-PullRequest/drop_linux_stage_linux_job/PerformanceEvaluation/Output/Summary.txt"
$fileContent = Get-Content -Path $filePath -Raw
Write-Host $fileContent

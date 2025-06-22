$retryCount = 5
$retryDelaySeconds = 5

$retryAttempts = 0
$retryException = $null

while ($retryAttempts -lt $retryCount) {
    try {
        dotnet GenderDebias.PerformanceEvaluation.dll prod Output "$(System.DefaultWorkingDirectory)/_GenderDebias-PullRequest/drop_linux_stage_linux_job/test_sets" -1

        # If the script execution is successful, break the loop
        break
    }
    catch [Azure.Identity.AuthenticationFailedException] {
        if ($_.Exception.Message -eq "Azure CLI authentication timed out.") {
            $retryAttempts++
            $retryException = $_.Exception

            Write-Host "Retry attempt $retryAttempts for exception: $($retryException.Message)"
            Start-Sleep -Seconds $retryDelaySeconds
        }
        else {
            # If it's a different exception, rethrow it to fail the task
            throw
        }
    }
}

# If all retry attempts failed, throw the last exception encountered
if ($retryAttempts -eq $retryCount -and $retryException) {
    throw $retryException
}

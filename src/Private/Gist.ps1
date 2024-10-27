class Gist {
    [string]$Id
    [string]$FileName

    Gist([string]$Id, [string]$FileName) {
        $this.Id = $Id
        $this.FileName = $FileName
    }

    static [Gist] Provide() {
        $gistId = [System.Environment]::GetEnvironmentVariable('GIST_GET_GIST_ID', [System.EnvironmentVariableTarget]::User)
        $fistFileName = [System.Environment]::GetEnvironmentVariable('GIST_GET_GIST_FILE_NAME', [System.EnvironmentVariableTarget]::User)
        if($gistId -and $fistFileName) {
            return [Gist]::new($gistId, $fistFileName)
        }
        else {
            throw "Please specify a GistId or Path. Alternatively, you need to register the GistId in advance using Set-GistGetGistId."
        }

    }
}
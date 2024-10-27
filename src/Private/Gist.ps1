class Gist {
    [string]$Id
    [string]$FileName

    Gist([string]$Id, [string]$FileName) {
        $this.Id = $Id
        $this.FileName = $FileName
    }

    static [Gist] Provide() {
        $id = [System.Environment]::GetEnvironmentVariable('GIST_GET_GIST_ID', [System.EnvironmentVariableTarget]::User)
        $fileName = [System.Environment]::GetEnvironmentVariable('GIST_GET_GIST_FILE_NAME', [System.EnvironmentVariableTarget]::User)
        if($id -and $fileName) {
            return [Gist]::new($id, $fileName)
        }
        else {
            throw "Please specify a GistId or Path. Alternatively, you need to register the GistId in advance using Set-GistGetGistId."
        }

    }
}
class GistFile {
    [string]$Id
    [string]$FileName

    GistFile([string]$Id, [string]$FileName) {
        $this.Id = $Id
        $this.FileName = $FileName
    }
}

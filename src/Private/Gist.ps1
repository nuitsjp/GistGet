class Gist {
    [string]$Id
    [string]$FileName

    Gist([string]$Id, [string]$FileName) {
        $this.Id = $Id
        $this.FileName = $FileName
    }
}
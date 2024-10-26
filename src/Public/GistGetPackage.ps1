class GistGetPackage {
    # インスタンスプロパティ
    [string]$Id = $null
    [bool]$AllowHashMismatch = $false
    [string]$Architecture = $null
    [string]$Custom = $null
    [bool]$Force = $false
    [string]$InstallerType = $null
    [string]$Locale = $null
    [string]$Log = $null
    [string]$Mode = $null
    [string]$Override = $null
    [string]$Scope = $null
    [string]$Version = $null
    [bool]$Confirm = $false
    [bool]$WhatIf = $false
    [bool]$Uninstall = $false
    
    static [string[]] $Parameters = @(
        'Id', 
        'AllowHashMismatch',
        'Architecture',
        'Custom',
        'Force',
        'InstallerType',
        'Locale',
        'Log',
        'Mode',
        'Override',
        'Scope',
        'Version',
        'Confirm',
        'WhatIf',
        'Uninstall'
    )

    # デフォルトコンストラクタ
    GistGetPackage() {
    }

    # パラメータ付きコンストラクタ
    GistGetPackage([string]$id) {
        $this.Id = $id
    }

    [hashtable] ToHashtable() {
        $hash = @{}
        foreach ($param in [GistGetPackage]::Parameters) {
            if ($this.$param) {
                $hash[$param] = $this.$param
            }
        }
        return $hash
    }

    static [hashtable] ToHashtable([GistGetPackage]$package) {
        return $package.ToHashtable()
    }


    # 静的ファクトリーメソッド
    static [GistGetPackage[]] ParseYaml([string]$yaml) {
        $packages = @()
        $packageList = $yaml | ConvertFrom-Yaml
        foreach ($package in $packageList) {
            $packages += [GistGetPackage]::CreateFromHashtable($package)
        }
        return $packages
    }

    static [GistGetPackage] CreateFromHashtable([hashtable]$hash) {
        $package = [GistGetPackage]::new()
        foreach ($param in [GistGetPackage]::Parameters) {
            if ($hash.ContainsKey($param)) {
                $package.$param = $hash[$param]
            }
        }
        return $package
    }
}

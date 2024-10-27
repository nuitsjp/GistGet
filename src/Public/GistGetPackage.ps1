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
        'id', 
        'allowHashMismatch',
        'architecture',
        'custom',
        'force',
        'installerType',
        'locale',
        'log',
        'mode',
        'override',
        'scope',
        'version',
        'confirm',
        'whatIf',
        'uninstall'
    )

    # デフォルトコンストラクタ
    GistGetPackage() {
    }

    # パラメータ付きコンストラクタ
    GistGetPackage([string]$id) {
        $this.Id = $id
    }

    [hashtable] ToHashtable() {
        $hash = [ordered]@{}
        foreach ($param in [GistGetPackage]::Parameters) {
            if ($this.$param) {
                $hash[$param] = $this.$param
            }
        }
        return $hash
    }

    static [string] ToYaml([GistGetPackage[]]$packages) {
        $values = [ordered]@{}
        foreach ($package in $packages) {
            $properties = [ordered]@{}
            foreach ($param in [GistGetPackage]::Parameters) {
                if ($package.$param) {
                    $properties[$param] = $package.$param
                }
            }
            $values[$package.Id] = $properties
        }

        return $values | ConvertTo-Yaml
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

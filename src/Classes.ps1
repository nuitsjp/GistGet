class GistFile {
    [string]$Id
    [string]$FileName

    GistFile([string]$Id, [string]$FileName) {
        $this.Id = $Id
        $this.FileName = $FileName
    }
}

class GistGetPackage {
    # インスタンスプロパティ
    [string]$Id = $null
    [bool]$AllowHashMismatch = $false
    [string]$Architecture = $null
    [string]$Custom = $null
    [bool]$Force = $false
    [string]$Header = $null
    [string]$InstallerType = $null
    [string]$Locale = $null
    [string]$Location = $null
    [string]$Log = $null
    [string]$Mode = $null
    [string]$Override = $null
    [string]$Scope = $null
    [bool]$SkipDependencies = $false
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
        'header',
        'installerType',
        'locale',
        'location',
        'log',
        'mode',
        'override',
        'scope',
        'skipDependencies',
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

        # $Packages を Id の昇順でソートしてyamlに変換
        foreach ($package in ($packages | Sort-Object Id)) {
            $properties = [ordered]@{}
            foreach ($param in [GistGetPackage]::Parameters) {
                # id 以外の有効なプロパティを取得
                if ($package.$param -and $param -ne "id") {
                    $properties[$param] = $package.$param
                }
            }
            # プロパティが空の場合は明示的に$nullを設定
            $values[$package.Id] = $properties.Count -eq 0 ? $null : $properties
        }
    
        # ConvertTo-Yaml の出力から余分なスペースを削除
        return ConvertTo-Yaml $values
    }

    # 静的ファクトリーメソッド
    static [GistGetPackage[]] ParseYaml([string]$yaml) {
        $packages = @()
        $hashtable = $yaml | ConvertFrom-Yaml
        $keys = $hashtable.Keys | Sort-Object
        foreach ($key in $keys) {
            $properties = $hashtable[$key]
            $package = [GistGetPackage]::new($key)
            if ($properties) {
                foreach ($param in [GistGetPackage]::Parameters) {
                    if ($properties.ContainsKey($param)) {
                        $package.$param = $properties[$param]
                    }
                }
            }
            $packages += $package
        }
        return $packages
    }

    static [GistGetPackage] FromHashtable([hashtable]$hash) {
        $package = [GistGetPackage]::new()
        foreach ($param in [GistGetPackage]::Parameters) {
            if ($hash.ContainsKey($param)) {
                $package.$param = $hash[$param]
            }
        }
        return $package
    }
}

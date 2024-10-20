# Module Pesterがインストールされていなかった場合は、インストールするin
if (-not (Get-Module -Name Pester -ListAvailable)) {
    Install-Module -Name Pester -Force -Scope CurrentUser
}
Import-Module -Name Pester -Force

Invoke-Pester
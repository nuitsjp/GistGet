# Get-GistGetPackage

Retrieves the list of packages defined in Gist.

```pwsh
Get-GistGetPackage
```

Retrieves definition information from Gist in the following priority order:

1. Gist ID set by Set-GistGetGistId
2. Gist with only "GistGet" set in "Gist description..."

The default is 2.

However, since 1. operates faster, it is recommended to explicitly register the ID using Set-GistGetGistId if you use it frequently.
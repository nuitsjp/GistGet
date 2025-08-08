# Set-GistFile

Sets the Gist ID and filename for using GistGet.

```pwsh
Set-GistFile -GistId 49990de4389f126d1f6d57c10c408a0c -GistFileName GistGet.yml
```

By default, GistGet uses the first file from a Gist that has only "GistGet" written in its "Gist description...".

This is designed for ease of use since Gist IDs are not easily recognizable. Therefore, internally it operates in the following steps:

1. Search all Gists for one that has only "GistGet" written in its "Gist description..."
2. Load packages from the Gist

As a result, two network communications occur.

By setting the ID and filename using Set-GistFile, you can use it more efficiently by reducing this overhead.
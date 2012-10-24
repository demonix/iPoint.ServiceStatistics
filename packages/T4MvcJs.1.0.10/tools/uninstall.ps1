param($installPath, $toolsPath, $package, $project)


$projectDir = [io.path]::GetDirectoryName($project.FullName)
$T4path = [io.path]::Combine($projectDir, "Scripts\T4MvcJs\")
Remove-Item -recurse $T4path 

$Scripts = $project.ProjectItems | where {$_.Name -eq 'Scripts'}
$T4MvcJsFolder = $Scripts.ProjectItems | where {$_.Name -eq 'T4MvcJs'}
$T4MvcJsFolder.Remove()


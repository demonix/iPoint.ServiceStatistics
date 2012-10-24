param($installPath, $toolsPath, $package, $project)
function FindRegisterRouteFunction($project)
{
	$asax = $project.ProjectItems | where {$_.Name -eq 'Global.asax'}
	$asax = $asax.ProjectItems | where {$_.Name -eq 'Global.asax.cs'}
	
	$namespace = $asax.FileCodeModel.CodeElements | where {$_.Kind -eq 5}

	$methods = $namespace.Children | % {$_.Children}
	$registerRoutesFunction = $methods | where {$_.Name -eq "RegisterRoutes"};
	
	$registerRoutesFunction.FullName;
}

function ReplaceFileContents($project, $filename, $toReplace, $replaceWith) {
	$projectDir = [io.path]::GetDirectoryName($project.FullName)
	$filePath = [io.path]::Combine($projectDir, "Scripts\T4MvcJs\" + $filename)
	$contents = [io.file]::ReadAllText($filePath)
	$contents = $contents.Replace($toReplace, $replaceWith)

	[io.file]::WriteAllText($filePath, $contents)
}

$registerRouteFunction = FindRegisterRouteFunction($project)
ReplaceFileContents $project "T4Proxy.cs" "//#MvcApplication.RegisterRoutes#" $registerRouteFunction

$packageFolder = $package.ToString().Replace(' ', '.')


$dllPath = '$(SolutionDir)packages\' + $packageFolder + '\tools\T4MvcJs.RoutesHandler.dll'

ReplaceFileContents $project 'T4MvcJs.tt' '$(ProjectDir)$(OutDir)T4MvcJs.RoutesHandler.dll' $dllPath
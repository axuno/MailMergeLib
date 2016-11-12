REM Pack .Net Framework Libraries
nuget.exe pack MailMergeLib.nuspec
REM Pack for .Net Core
"C:\Program Files\dotnet\dotnet.exe" pack ~/project.json --no-build --configuration release


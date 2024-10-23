find . -name "*.nupkg"|xargs rm -rf
dotnet pack --configuration Debug
find . -name "*.nupkg"|xargs -I {} dotnet nuget push "{}" -k $NUGET_TOKEN -s https://api.nuget.org/v3/index.json --skip-duplicate

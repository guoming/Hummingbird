find . -name "*.nupkg"|xargs rm -rf
dotnet pack --configuration Debug
find . -name "*.nupkg"|xargs -I {} dotnet nuget push "{}" -k $k -s https://api.nuget.org/v3/index.json --skip-duplicate

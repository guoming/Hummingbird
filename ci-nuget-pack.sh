find . -name "*.nupkg"|xargs rm -rf
dotnet pack --configuration Debug
find . -name "*.nupkg"|xargs -I {} dotnet nuget push "{}"  --api-key oy2pni2ildbzv7a6oo7aq73fc5eqqlouzkcb7yync4cn5i -s https://api.nuget.org/v3/index.json

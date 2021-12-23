find . -name "*.nupkg"|xargs rm -rf
dotnet pack --configuration Debug
find . -name "*.nupkg"|xargs -I {} dotnet nuget push "{}"  --api-key oy2lf5bairpuqwucpxsa3w2f7hoj7txupek3prt4a2bi5u -s https://api.nuget.org/v3/index.json

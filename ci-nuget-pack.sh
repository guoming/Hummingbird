find . -name "*.nupkg"|xargs rm -rf
dotnet pack --configuration Debug
find . -name "*.nupkg"|xargs -I {} dotnet nuget push "{}"  --api-key  oy2l53fxd7xm5jndnyrqewssedgopshuticofclpespbyi  -s https://api.nuget.org/v3/index.json --skip-duplicate

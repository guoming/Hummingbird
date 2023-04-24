find . -name "*.nupkg"|xargs rm -rf
dotnet pack --configuration Debug
find . -name "*.nupkg"|xargs -I {} dotnet nuget push "{}" --apikey oy2f46ijfbgeob43rk4qggky5q7m4jxehftjy5bwq6etni -s https://api.nuget.org/v3/index.json --skip-duplicate

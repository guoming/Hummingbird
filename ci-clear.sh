find . -name "*Release"|xargs -I {} rm -rf "{}"
find . -name "*Debug"|xargs -I {} rm -rf "{}"

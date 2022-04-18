# dotnet build -c Release

dotnet publish ff.csproj --configuration Release -r osx.12-arm64 -f net6.0 -p:UseAppHost=true --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:PublishTrimmed=false -o ./ -nologo
cp ff ~/bin/ff
# have to re-sign in the target folder or it won't be trusted by MacOS
codesign -f -s - ~/bin/ff


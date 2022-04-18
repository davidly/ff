# dotnet build -c Release

dotnet publish ff.csproj --configuration Release -r linux-x64 -f net6.0 -p:UseAppHost=true --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:PublishTrimmed=false -o ./ -nologo


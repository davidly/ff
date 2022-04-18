dotnet publish ff.csproj --configuration Release -r win10-x64 -f net6.0 -p:PlatformTarget=x64 -p:UseAppHost=true --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:PublishTrimmed=false -o ./ -nologo


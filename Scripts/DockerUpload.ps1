docker build -f ..\src\IK.Imager.Api\Dockerfile ..\src -t ilyakhorev/ik-imager-api:1.0
docker build -f ..\src\IK.Imager.BackgroundService\Dockerfile ..\src -t ilyakhorev/ik-imager-backgroundservice:1.0

docker push ilyakhorev/ik-imager-api:1.0
docker push ilyakhorev/ik-imager-backgroundservice:1.0

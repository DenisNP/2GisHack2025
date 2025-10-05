# build client
FROM node:24 as BUILD_CLIENT
COPY ./frontend /app
WORKDIR /app
RUN npm install -g yarn
RUN yarn install
RUN yarn build

# build server
FROM mcr.microsoft.com/dotnet/sdk:9.0 as BUILD_SERVER
WORKDIR /app
COPY ./backend .
WORKDIR ./2GisHack2025/AntAlgorightm.WebApi
RUN dotnet restore
RUN dotnet publish -c Release -o out

# copy
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=BUILD_SERVER /app/2GisHack2025/AntAlgorightm.WebApi/out .
COPY --from=BUILD_CLIENT /app/build /app/wwwroot

# run
EXPOSE 5070
ENTRYPOINT ["dotnet", "AntAlgorightm.WebApi.dll"]
# build client
FROM node:24 as BUILD_CLIENT
COPY ./frontend /app
WORKDIR /app

# Объявляем ARG с значениями из переменных окружения хоста
ARG REACT_APP_MAPGL_API_KEY
ARG REACT_APP_MAPGL_STYLE_ID
ARG REACT_APP_2GIS_PAGE_SIZE

# Устанавливаем переменные окружения для сборки React
ENV REACT_APP_MAPGL_API_KEY=${REACT_APP_MAPGL_API_KEY}
ENV REACT_APP_MAPGL_STYLE_ID=${REACT_APP_MAPGL_STYLE_ID}
ENV REACT_APP_2GIS_PAGE_SIZE=${REACT_APP_2GIS_PAGE_SIZE}

RUN yarn install
RUN yarn build

# build server
FROM mcr.microsoft.com/dotnet/sdk:9.0 as BUILD_SERVER
WORKDIR /app
COPY ./backend .
WORKDIR ./PathScape.WebApi
RUN dotnet restore
RUN dotnet publish -c Release -o out

# copy
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=BUILD_SERVER /app/PathScape.WebApi/out .
COPY --from=BUILD_CLIENT /app/build /app/wwwroot

# run
EXPOSE 8080
ENTRYPOINT ["dotnet", "PathScape.WebApi.dll"]
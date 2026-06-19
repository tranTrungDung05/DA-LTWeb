# Giai đoạn 1: Sử dụng SDK để biên dịch code (Nặng cấu hình build)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Sao chép file csproj và restore các gói NuGet trước để tận dụng cache của Docker
COPY *.csproj ./
RUN dotnet restore

# Sao chép toàn bộ code còn lại vào và biên dịch ra thư mục 'out'
COPY . ./
RUN dotnet publish -c Release -o out

# Giai đoạn 2: Sử dụng Runtime để chạy ứng dụng (Cực kỳ nhẹ và bảo mật)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Cấu hình cổng chạy bên trong container (Mặc định .NET 8 dùng cổng 8080)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Mvc-entity-docker.dll"]
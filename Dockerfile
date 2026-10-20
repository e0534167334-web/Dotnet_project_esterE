# בנייה: רק פרויקט ה-API והשכבות שלו (פרויקט הבדיקות לא נכנס לקונטיינר)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Baby_Clinic/Baby_Clinic.csproj Baby_Clinic/
COPY Baby_Clinic.Core/Baby_Clinic.Core.csproj Baby_Clinic.Core/
COPY Baby_Clinic.Data/Baby_Clinic.Data.csproj Baby_Clinic.Data/
COPY Baby_Clinic.Service/Baby_Clinic.Service.csproj Baby_Clinic.Service/
RUN dotnet restore Baby_Clinic/Baby_Clinic.csproj

COPY Baby_Clinic/ Baby_Clinic/
COPY Baby_Clinic.Core/ Baby_Clinic.Core/
COPY Baby_Clinic.Data/ Baby_Clinic.Data/
COPY Baby_Clinic.Service/ Baby_Clinic.Service/
RUN dotnet publish Baby_Clinic/Baby_Clinic.csproj -c Release -o /app --no-restore

# הרצה
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# Render מפנה את התעבורה לפורט הזה (משתנה PORT בהגדרות השירות)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Baby_Clinic.dll"]

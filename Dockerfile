FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /app

COPY ./InvoiceGenerator ./

RUN dotnet restore

RUN dotnet publish --configuration release --output out

FROM mcr.microsoft.com/dotnet/runtime:7.0

WORKDIR /final

COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "InvoiceGenerator.dll"]
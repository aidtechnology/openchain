FROM microsoft/dotnet:1.1-sdk-projectjson

RUN mkdir -p /openchain

COPY src/ /openchain

WORKDIR /openchain/Openchain

RUN dotnet restore

# We will probably want to save the lock files directly in the repository, but for the time-being this should do...
RUN cd ../Openchain.Abstractions && dotnet restore
RUN cd ../Openchain.Infrastructure && dotnet restore
RUN cd ../Openchain.Server && dotnet restore
RUN cd ../Openchain.Anchoring.Blockchain && dotnet restore
RUN cd ../Openchain.Sqlite && dotnet restore
RUN cd ../Openchain.SqlServer && dotnet restore
RUN cd ../Openchain.Validation.PermissionBased && dotnet restore

EXPOSE 8080
ENTRYPOINT ["dotnet", "run"]

FROM mcr.microsoft.com/dotnet/sdk:9.0.303-alpine3.21@sha256:2fe880002c458a6e95a3f8bb38b63c0f2e21ffefcb01c0223c4408cc91ad7d9d
RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang=19.1.4-r0 \
        cmake=3.31.1-r0 \
        make=4.4.1-r2 \
        bash=5.2.37-r0 \
        alpine-sdk=1.1-r0 \
        protobuf=24.4-r4 \
        protobuf-dev=24.4-r4 \
        grpc=1.62.1-r2 \
        grpc-plugins=1.62.1-r2

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.412 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project

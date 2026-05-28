FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=${USER_UID}

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        bash-completion \
        build-essential \
        ca-certificates \
        clang \
        cmake \
        curl \
        gdb \
        git \
        iputils-ping \
        jq \
        less \
        libssl-dev \
        mosquitto-clients \
        netcat-openbsd \
        ninja-build \
        pkg-config \
        postgresql-client \
        sudo \
        unzip \
        wget \
        zip \
    && rm -rf /var/lib/apt/lists/*

RUN if ! getent group "${USERNAME}" >/dev/null; then groupadd --gid "${USER_GID}" "${USERNAME}"; fi \
    && if ! id -u "${USERNAME}" >/dev/null 2>&1; then useradd --uid "${USER_UID}" --gid "${USER_GID}" -m "${USERNAME}"; fi \
    && echo "${USERNAME} ALL=(root) NOPASSWD:ALL" >/etc/sudoers.d/${USERNAME} \
    && chmod 0440 /etc/sudoers.d/${USERNAME}

WORKDIR /workspaces/CoroMES

FROM i386/debian:stretch-slim AS converter-base

WORKDIR /

RUN \
	apt update \
	&& apt install -y --no-install-recommends curl apt-transport-https dirmngr gzip tar gnupg ca-certificates \
        tzdata less psmisc git curl libstdc++ imagemagick xvfb xauth \
	&& apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
	&& echo "deb https://download.mono-project.com/repo/debian stable-stretch main" | tee /etc/apt/sources.list.d/mono-official-stable.list \
	&& apt update \
	&& apt install -y --no-install-recommends mono-complete \
	&& ln -sf /usr/share/zoneinfo/America/Los_Angeles /etc/localtime \
	&& dpkg-reconfigure -f noninteractive tzdata \
	&& rm -rf /var/lib/apt/lists/* /tmp/*

COPY performer performer
# note: performer support is removed in OpenSceneGraph after version 3.4, so have to checkout the older 3.4 branch
RUN \
    for i in `ls /performer/*tgz`; do tar -C / -xzvf $i; done \
    && ldconfig \
    && echo "deb-src http://deb.debian.org/debian stretch main" | tee /etc/apt/sources.list.d/debian-sources.list \
    && apt update \
    && apt build-dep -y openscenegraph \
    && mkdir /osg \
    && cd /osg \
    && git clone https://github.com/openscenegraph/OpenSceneGraph.git \
    && cd OpenSceneGraph \
    && git checkout OpenSceneGraph-3.4 \
    && git pull \
    && mkdir ../build \
    && cd ../build \
    && cmake ../OpenSceneGraph \
    && make \
    && make install \
    && mkdir /usr/tmp \
    && chmod a+rwx /usr/tmp \
    && rm -rf /osg \
    && rm -rf /var/lib/apt/lists/* /tmp/*

FROM converter-base AS converter

WORKDIR /

COPY bin app

ENTRYPOINT [ "mono", "--debug", "/app/monoexe/MiplMeshToObj.exe" ]
CMD [ "/path/to/mipl_mesh_filename", "/directory/to/write/obj" ]

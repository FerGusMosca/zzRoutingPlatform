# Usa una imagen base de Windows con .NET Framework 4.7.2 instalado
FROM mcr.microsoft.com/dotnet/framework/runtime:4.7.2-windowsservercore-ltsc2019

# Establece el directorio de trabajo en el contenedor
WORKDIR /app

# Copia todos los archivos del proyecto al contenedor
COPY . /app

# Establece el punto de entrada para ejecutar la aplicación
ENTRYPOINT ["C:\\Projects\\Bias\\tph_platform\\hftplatform\\HFTPlatform\\DockerDummyTestApp\\bin\\Debug"]

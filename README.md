# Gestor_Archivos

# 🗂️ FileManagerApp

Un gestor de archivos moderno, rápido e intuitivo para Windows diseñado bajo la arquitectura MVVM, utilizando **.NET 8** y **WPF / WinUI 3**. Este proyecto ha sido desarrollado como un esfuerzo colaborativo siguiendo principios de código limpio, inyección de dependencias y pruebas unitarias automáticas.

---

## 🚀 Stack Tecnológico

| Componente              | Tecnología                                        |
| :---------------------- | :------------------------------------------------ |
| **Lenguaje**            | C# 12                                             |
| **Framework**           | .NET 8 (LTS)                                      |
| **Interfaz de Usuario** | WPF + Material Design in XAML Toolkit (o WinUI 3) |
| **Acceso a Archivos**   | `System.IO` & `FileSystemWatcher`                 |
| **Arquitectura**        | MVVM (Model-View-ViewModel)                       |
| **Inyección de Dep.**   | `Microsoft.Extensions.DependencyInjection`        |
| **Pruebas**             | xUnit + Moq                                       |
| **CI/CD**               | GitHub Actions                                    |

---

## 📂 Arquitectura y Estructura del Proyecto

La solución está dividida en proyectos independientes para garantizar la separación de responsabilidades y permitir el desarrollo en paralelo:

```text
FileManagerApp/
│
├── FileManager.Core/          # Lógica de negocio y acceso a System.IO (Persona 1)
│   ├── Models/                # Entidades (FileSystemItem, FileProperties)
│   └── Services/              # Servicios nativos (FileService, PropertiesService)
│
├── FileManager.ViewModels/    # Capa de presentación lógica y estados (Persona 3)
│   ├── Commands/              # Comandos reutilizables (RelayCommand)
│   └── ViewModels/            # MainViewModel, FileExplorerViewModel, etc.
│
├── FileManager.UI/            # Interfaz de usuario y recursos visuales (Persona 2)
│   ├── Views/                 # Ventanas y Páginas XAML
│   ├── Controls/              # Componentes personalizados (Breadcrumb, TreeView)
│   └── Themes/                # Estilos y diccionarios de recursos
│
└── FileManager.Tests/         # Pruebas automatizadas (Todo el equipo)
    ├── Core.Tests/
    └── ViewModels.Tests/

```

---

## 🛠️ Requisitos Previos e Instalación

Para compilar y ejecutar este proyecto localmente, asegúrate de tener instalado lo siguiente en tu entorno de desarrollo:

1. **Sistema Operativo:** Windows 10 (versión 1809 o superior) o Windows 11.
2. **IDE:** [Visual Studio 2022](https://visualstudio.microsoft.com/) (Se recomienda versión Community o superior).

- _Nota: Durante la instalación de Visual Studio, asegúrate de marcar la carga de trabajo **"Desarrollo de escritorio de .NET"**._

3. **SDK:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Viene incluido en Visual Studio 2022).
4. **Git:** Herramienta de control de versiones instalada y configurada.

### Clonar el repositorio

Abre tu terminal (PowerShell o Git Bash) y ejecuta:

```powershell
git clone [https://github.com/juanlealr/gestor_archivos.git](https://github.com/juanlealr/gestor_archivos.git)
cd "FileManagerApp"

```

---

## 💻 Cómo Compilar y Ejecutar

### Desde Visual Studio 2022

1. Abre el archivo de solución `FileManagerApp.sln`.
2. Espera a que Visual Studio restaure automáticamente los paquetes NuGet.
3. Haz clic derecho sobre el proyecto **`FileManager.UI`** en el Explorador de soluciones y selecciona **"Establecer como proyecto de inicio"** (_Set as StartUp Project_).
4. Presiona `F5` para compilar y ejecutar en modo depuración.

### Desde la Consola de Comandos (.NET CLI)

Si prefieres usar la terminal, sitúate en la raíz de la solución y ejecuta:

```powershell
# Restaurar dependencias
dotnet restore

# Compilar la solución completa
dotnet build

# Ejecutar el proyecto de interfaz de usuario
dotnet run --project .\FileManager.UI\FileManager.UI.csproj

```

### Ejecutar las Pruebas Unitarias

```powershell
dotnet test

```

---

## 📖 Guía de Uso del Aplicativo

Al iniciar la aplicación, te encontrarás con una interfaz moderna dividida en zonas clave:

- **Panel Izquierdo (Árbol de Navegación):** Muestra los discos locales y la jerarquía de carpetas. Haz clic en las flechas para expandir los directorios.
- **Panel Central (Vista de Contenido):** Muestra los archivos y carpetas del directorio actual con detalles de tamaño, tipo y última modificación.
- _Doble clic:_ Abre carpetas para navegar o ejecuta archivos con la aplicación nativa de Windows.
- _Clic derecho:_ Despliega el menú contextual con operaciones rápidas (Copiar, Mover, Cambiar nombre, Eliminar y Propiedades).

- **Barra Superior (Breadcrumb):** Muestra la ruta actual de manera interactiva. Puedes hacer clic en cualquier sección de la ruta para regresar rápidamente.
- **Panel Lateral Derecho (Vista Previa):** Al seleccionar un archivo compatible (Imágenes o Texto), se generará una previsualización automática.

---

## 🤝 Convenciones y Flujo de Trabajo del Equipo

Para mantener el orden en el desarrollo en paralelo, el equipo sigue estrictamente las siguientes reglas:

### Flujo de Ramas (Git Flow Simplificado)

- `main`: Solo contiene versiones estables y listas para producción (Releases).
- `develop`: Rama principal de integración. Todo el código nuevo se fusiona aquí primero.
- `feature/PX-descripcion`: Ramas de trabajo individuales. Donde `PX` representa el rol asignado (P1: Core, P2: UI, P3: ViewModels).

> ⚠️ **Regla de Oro:** Todo Pull Request (PR) hacia `develop` requiere la aprobación de al menos **un compañero de equipo** y debe pasar la build automática de GitHub Actions antes de ser mezclado.

### Estilo de Commits (Conventional Commits)

Los mensajes de commit deben ser claros y estructurados:

- `feat:` Nueva funcionalidad (ej: `feat: agregar copiado asíncrono de archivos`)
- `fix:` Corrección de un error (ej: `fix: resolver excepción de ruta no encontrada`)
- `docs:` Cambios en la documentación (ej: `docs: actualizar instrucciones del readme`)
- `test:` Añadir o modificar pruebas unitarias.

---

## 📅 Hitos del Proyecto

- [x] **Hito 1 (Semana 1):** Proyecto base configurado e interfaces de servicios definidas.
- [ ] **Hito 2 (Semana 4):** MVP Funcional (Navegación base y visualización de propiedades).
- [ ] **Hito 3 (Semana 8):** Operaciones CRUD completas del sistema de archivos y alertas en tiempo real.
- [ ] **Hito 4 (Semana 11):** UX pulida (Búsquedas, temas claro/oscuro y Drag & Drop).
- [ ] **Hito 5 (Semana 12):** Pruebas finales, empaquetado MSIX y lanzamiento de la versión 1.0.

```

```

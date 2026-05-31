# Guía de ViewModels - Gestor de Archivos

## Estructura Creada

Se han implementado los siguientes componentes en `FileManager.ViewModels`:

### 1. **RelayCommand.cs**

Implementación de `ICommand` para usar en los ViewModels:

- `RelayCommand`: Comando genérico no tipado
- `RelayCommand<T>`: Comando tipado genéricamente

**Uso:**

```csharp
OpenFileCommand = new RelayCommand<FileSystemItem>(item => { /* lógica */ });
```

### 2. **ViewModelBase.cs**

Clase base abstracta para todos los ViewModels:

- Implementa `INotifyPropertyChanged`
- Método `OnPropertyChanged()` para notificar cambios
- Método `SetProperty<T>()` para facilitar binding automático

**Uso:**

```csharp
public class MyViewModel : ViewModelBase
{
    private string _currentPath = "";

    public string CurrentPath
    {
        get => _currentPath;
        set => SetProperty(ref _currentPath, value);
    }
}
```

### 3. **MainViewModel.cs**

ViewModel principal de la aplicación con inyección de dependencias:

- Inyecta `IFileService` en el constructor
- Contiene referencia a `FileExplorerViewModel`
- Método `InitializeAsync()` para cargar datos iniciales

**Características:**

- Separación clara de responsabilidades
- Carga de unidades al iniciar

### 4. **FileExplorerViewModel.cs**

ViewModel para la exploración de archivos:

**Propiedades:**

- `CurrentPath`: Ruta actual del directorio
- `SelectedItem`: Archivo/carpeta seleccionado
- `IsLoading`: Indica si se está cargando
- `Items`: Colección de elementos del directorio
- `Drives`: Colección de unidades disponibles

**Comandos:**

- `OpenFileCommand`: Abre archivo o navega a carpeta
- `NavigateBackCommand`: Navega hacia atrás en el historial
- `NavigateForwardCommand`: Navega hacia adelante
- `NavigateToPathCommand`: Navega a una ruta específica

**Historial de Navegación:**

- Implementa pilas (stacks) para Back/Forward
- Se limpia automáticamente al navegar lateralmente

## Configuración en App.xaml.cs

La inyección de dependencias se configura en `App.xaml.cs`:

```csharp
private IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();

    // Registrar servicios
    services.AddSingleton<IFileService, FileService>();
    services.AddSingleton<MainViewModel>();
    services.AddSingleton<MainWindow>();

    return services.BuildServiceProvider();
}
```

## Binding en XAML

### Ejemplo de Binding a Propiedades

```xaml
<TextBox Text="{Binding FileExplorerViewModel.CurrentPath, Mode=OneWay}"/>
```

### Ejemplo de Binding a Comandos

```xaml
<Button Command="{Binding FileExplorerViewModel.NavigateBackCommand}" Content="Atrás"/>
```

### Ejemplo de Binding a Colecciones

```xaml
<ListBox ItemsSource="{Binding FileExplorerViewModel.Items}"
         SelectedItem="{Binding FileExplorerViewModel.SelectedItem}"/>
```

## Flujo de Ejecución

1. **App.xaml.cs - OnStartup()**
   - Configura servicios DI
   - Crea MainViewModel
   - Asigna como DataContext a MainWindow
   - Llama a `MainViewModel.InitializeAsync()`

2. **MainViewModel.InitializeAsync()**
   - Crea FileExplorerViewModel
   - Llama a `LoadDrivesAsync()`

3. **FileExplorerViewModel.LoadDrivesAsync()**
   - Obtiene unidades del servicio
   - Llena la colección Drives

4. **Usuario interactúa con UI**
   - Click en unidad → Ejecuta `NavigateToPathCommand`
   - Click en carpeta → Ejecuta `OpenFileCommand`
   - Click en atrás → Ejecuta `NavigateBackCommand`

## Próximos Pasos

1. **Implementar FileService** en `FileManager.Core/Services/FileService.cs`
2. **Registrar en App.xaml.cs:**

   ```csharp
   services.AddSingleton<IFileService, FileService>();
   ```

3. **Agregar más ViewModels** según sea necesario (búsqueda, propiedades, etc.)

## Notas Importantes

- Los ViewModels usan async/await para operaciones I/O
- La colección `Items` es `ObservableCollection<T>` para auto-actualizar UI
- El historial de navegación es local al ViewModel (no persistente)
- Los comandos solo se ejecutan si `CanExecute` retorna true

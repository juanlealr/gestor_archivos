using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using FileManager.Core.Services;
using FileManager.ViewModels;

namespace FileManager.UI;

/// <summary>
/// Interaction logic for App.xaml
/// Configura la inyección de dependencias para la aplicación.
/// </summary>
public partial class App : Application
{
    private IServiceProvider _serviceProvider;

    public App()
    {
        _serviceProvider = ConfigureServices();
    }

    /// <summary>
    /// Configura los servicios de inyección de dependencias.
    /// </summary>
    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Registrar servicios
        // TODO: Implementar FileService cuando esté listo
        // Por ahora, se puede usar un mock para pruebas
        // services.AddSingleton<IFileService, FileService>();
        
        // Registrar ViewModels
        services.AddSingleton<MainViewModel>();
        
        // Registrar ventanas
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Obtener la ventana principal del contenedor DI
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        
        // Obtener el ViewModel principal
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        
        // Establecer el DataContext
        mainWindow.DataContext = mainViewModel;
        
        // Inicializar el ViewModel (cargar datos iniciales)
        mainViewModel.InitializeAsync();
        
        // Mostrar la ventana
        mainWindow.Show();
    }
}


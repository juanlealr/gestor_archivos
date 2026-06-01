# Guía de Usuario

Bienvenido a Gestor de Archivos — una aplicación WPF para explorar y gestionar archivos en tu sistema.

Principales áreas y funcionamiento:

- Barra de direcciones
  - Campo de texto para escribir o ver la ruta actual (CurrentPath).
  - Botón "Ir" ejecuta la navegación a la ruta introducida.

- Panel izquierdo (Árbol de unidades)
  - Muestra las unidades del sistema (Drives).
  - Haz clic en una unidad para cargar su contenido en el panel derecho.

- Panel derecho (Lista de archivos)
  - Lista los elementos del directorio actual (archivos y carpetas).
  - Columnas: Nombre, Tipo, Tamaño, Fecha Modificación.
  - Doble clic en una carpeta para navegar dentro; doble clic en archivo intentará abrirlo.
  - Selecciona un elemento para operaciones (Copiar, Cortar, Renombrar, Eliminar).

- Barra de herramientas (centro superior)
  - ➕ Nuevo: crea un nuevo elemento en la ruta actual (si está implementado).
  - ✂️ Cortar: corta el elemento seleccionado (para pegarlo en otra carpeta).
  - 📋 Copiar: copia el elemento seleccionado.
  - 📥 Pegar: pega el elemento copiado/cortado en la ruta actual.
  - ✏️ Renombrar: renombra el elemento seleccionado.
  - ❌ Eliminar: borra el elemento seleccionado.

  Nota: si alguna acción no realiza nada, significa que el comando correspondiente aún no está implementado o no hay elemento seleccionado.

- Navegación y historial
  - La aplicación mantiene historial de navegación (Atrás/Adelante) en el ViewModel cuando se navega.

- Barra de estado
  - Indica el estado actual (por ejemplo "Listo") y mensajes de carga.

Consejos rápidos
- Si la vista no muestra elementos, comprueba permisos de acceso al directorio.
- Para navegar manualmente escribe la ruta completa en la barra de direcciones y pulsa "Ir".

Si encuentras errores o funcionalidades faltantes, contacta al desarrollador o revisa la guía del programador para detalles técnicos.

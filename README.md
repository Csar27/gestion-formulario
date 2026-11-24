# Gestión de Formularios

Aplicación full-stack para diseñar formularios dinámicos, catálogos dependientes, respuestas parciales y reportes con exportación.

## Stack

- **Backend:** .NET 8 Web API — Clean Architecture (Domain / Application / Infrastructure / Api), EF Core + PostgreSQL (Npgsql), ASP.NET Core Identity + JWT, FluentValidation, ClosedXML y QuestPDF.
- **Frontend:** Angular (standalone components, Reactive Forms, Angular Material).

## Estado del proyecto

- [x] **Etapa 1 — Base de datos y entidades**
- [x] **Etapa 2 - Seguridad** (Identity, JWT, roles, permisos y usuarios)
- [x] **Etapa 3 - API de formularios y respuestas**
- [x] **Etapa 4 - Catálogos**
- [x] **Etapa 5 — Diseñador Angular**
- [x] **Etapa 6 — Renderizador dinámico y stepper** - esta entrega
- [x] Etapa 7 - Reportes y exportaciones

## Puesta en marcha (Etapa 1)

Requisitos: .NET 8 SDK, Docker y (para la Etapa 5) Node 20+.

```bash
# 1. Base de datos
docker compose up -d db

# 2. Migraciones (la herramienta dotnet-ef 8 está en el manifest local)
dotnet tool restore
dotnet ef database update \
  --project src/GestionFormulario.Infrastructure \
  --startup-project src/GestionFormulario.Api

# 3. Compilación y tests
dotnet build GestionFormulario.sln
dotnet test GestionFormulario.sln

# 4. API (siembra automáticamente permisos, rol Admin y usuario admin)
dotnet run --project src/GestionFormulario.Api

# 5. Frontend Angular
cd frontend
npm install
npm start   # http://localhost:4200
```

- Si el API se ejecuta desde Visual Studio con IIS Express, el puerto HTTP (`36835`) redirige al HTTPS (`44366`) por `UseHttpsRedirection`, y ese salto rompe CORS desde el navegador. Para desarrollo con el frontend, levantar el API compilado en un puerto HTTP puro:
  ```bash
  cd src/GestionFormulario.Api
  dotnet bin/Debug/net8.0/GestionFormulario.Api.dll --urls http://localhost:5080
  ```
  y dejar `apiUrl: 'http://localhost:5080/api'` en `frontend/src/environments/environment.ts` (así no se necesita confiar en el certificado de IIS Express).
- Swagger: el puerto que indique la consola (perfil IIS Express: `http://localhost:36835/swagger` / `https://localhost:44366/swagger`).
- Health check: `GET /health`
- Usuario seed: `admin@gestionformulario.local` / `Admin123!` (`DebeCambiarPassword = true`)
  - En la base de datos local de desarrollo la contraseña se cambió a `Admin1234!` durante la verificación de la Etapa 2.

> El usuario y la contraseña del seed se leen de `Seed` en `appsettings.json`. Cámbialos antes de desplegar.

## Seguridad (Etapa 2)

Flujos disponibles:

- `POST /api/auth/login` → `accessToken` (JWT, 15 min) + `refreshToken` (7 días, rotado en cada uso).
- `POST /api/auth/refresh` → nuevo par de tokens; reutilizar un token ya rotado revoca todos los tokens del usuario.
- `POST /api/auth/cambiar-password` → obligatorio en el primer login; hasta que se ejecuta, el resto de la API responde `403` con `code: CAMBIO_PASSWORD_REQUERIDO`.
- `POST /api/auth/olvide-password` / `reset-password`, `POST /api/auth/logout`, `GET /api/auth/mis-permisos`.
- `GET/POST/PUT/DELETE /api/usuarios` y `/api/roles`, más `GET /api/roles/permisos-disponibles` y `PUT /api/roles/{id}/permisos`.

El código de permiso se declara con `[HasPermission("Catalogos.Editar")]` y se resuelve mediante una política dinámica `Permiso:{codigo}` a partir de las claims `permiso` del JWT. Los roles `Admin` y los usuarios administradores no se pueden editar/eliminar. Las respuestas de error usan `ProblemDetails` con un campo `code` estable (`VALIDACION`, `CREDENCIALES_INVALIDAS`, `ACCESO_DENEGADO`, etc.).

## Catálogos (Etapa 4)

- `GET /api/catalogos`, `GET /api/catalogos/{id}`, `POST /api/catalogos`, `PUT /api/catalogos/{id}` y `DELETE /api/catalogos/{id}` con permisos `Catalogos.Ver/Crear/Editar/Eliminar`.
- `GET /api/catalogos/{id}/items?padreItemId=&buscar=&soloActivos=true` devuelve opciones listas para `mat-select` con búsqueda. En catálogos dependientes exige `padreItemId` y valida que el item padre pertenezca al catálogo padre.
- CRUD de ítems: `POST /api/catalogos/{catalogoId}/items`, `PUT /api/catalogos/items/{itemId}` y `DELETE /api/catalogos/items/{itemId}`.
- Importación `POST /api/catalogos/{id}/importar` (multipart/form-data; campo `archivo` y opcional `reemplazar=true`) de `.csv` o `.xlsx` usando CsvHelper y ClosedXML. Columnas aceptadas: `Codigo`, `Texto`, `Orden` y `PadreItemCodigo` (este último obligatorio en catálogos dependientes).
- Jerarquía multinivel (País → Departamento → Ciudad) mediante `CatalogoPadreId` + `PadreItemId`. Se rechazan ciclos, padres inexistentes, padres inactivos y padres de otro catálogo.
- Borrado lógico: al eliminar un catálogo se desactivan sus descendientes e ítems; `RespuestaValor.ValorTexto` conserva la instantánea histórica aunque el item cambie o se desactive.
- Códigos de error estables: `CATALOGO_DUPLICADO`, `CATALOGO_NO_ENCONTRADO`, `CATALOGO_CICLICO`, `CATALOGO_PADRE_INVALIDO`, `PADRE_ITEM_REQUERIDO`, `PADRE_ITEM_INVALIDO`, `ITEM_DUPLICADO`, `ITEM_NO_ENCONTRADO`, `ARCHIVO_VACIO`.

## Formularios y respuestas (Etapa 3)

- `GET /api/formularios`, `GET /api/formularios/{id}`, `POST /api/formularios`, `PUT /api/formularios/{id}` y `DELETE /api/formularios/{id}` con permisos `Formularios.Ver/Crear/Editar/Eliminar`.
- `GET /api/formularios` solo devuelve los formularios sin restricción de roles o los permitidos al usuario; Admin ve todos.
- `EsPasoAPaso` habilita el stepper. Tipos soportados: `Texto`, `Entero`, `Decimal`, `Email`, `Fecha`, `Hora`, `Moneda`, `Porcentaje` y `Lista`. `Fecha` usa `AAAA-MM-DD` y `Hora` usa `HH:mm` (se normalizan en servidor; el texto mostrado en reportes usa el formato local `es-ES`).
- Opciones de lista: `FuenteOpciones = Manual` guarda `[{ "valor", "etiqueta" }]` en `jsonb`; `Catalogo` usa ítems activos y respeta `CampoPadreId` con validación dependiente en servidor.
- Normalización: `Valor` se guarda en formato invariante; `ValorTexto` conserva la presentación `es-ES` (`1.234,56 €`, `15,00 %`) para que el histórico no cambie.
- `POST /api/respuestas` con `{ id, formularioId, seccionId, valores, finalizar }`:
  - sin `id` crea un borrador; con `id` lo reanuda.
  - con `seccionId` valida solo los campos obligatorios de esa sección ("Guardar y continuar").
  - con `finalizar: true` valida todos los obligatorios, pasa de `Borrador` a `Completa` y persiste todos los valores.
  - una respuesta completada ya no admite guardados parciales (409 `RESPUESTA_COMPLETA`).
- `GET /api/respuestas?formularioId=&desde=&hasta=&estado=&page=&pageSize=`, `GET /api/respuestas/{id}` y `DELETE /api/respuestas/{id}` con permisos `Respuestas.*`.
- Los usuarios no administradores solo ven y modifican sus propias respuestas; Admin ve todas.

Códigos de error añadidos: `FORMULARIO_NO_ENCONTRADO`, `FORMULARIO_DUPLICADO`, `FORMULARIO_EN_USO`, `CAMPO_REQUERIDO`, `VALOR_INVALIDO`, `VALOR_FUERA_RANGO`, `OPCION_NO_VALIDA`, `RESPUESTA_COMPLETA`, `RESPUESTA_NO_ENCONTRADA`.


## Frontend Angular (Etapa 5)

La aplicación Angular 21 vive en `frontend/` y usa componentes standalone, Angular Material 21, Reactive Forms y signals.

- `npm install` y `npm start` → `http://localhost:4200`. La API se consume desde `http://localhost:5080/api` (configurable en `frontend/src/environments/environment.ts`).
- Autenticación: `AuthService` guarda la sesión en `localStorage` (`gestion-formulario-usuario`); `authInterceptor` añade el `Bearer` y reintenta una vez tras `POST /api/auth/refresh` cuando recibe un 401.
- `authGuard` y `permissionGuard` protegen las rutas. Si el usuario debe cambiar su contraseña, se le redirige a `/cambiar-password` antes de acceder al panel.
- El menú lateral es responsive y se construye desde los permisos del JWT; la directiva estructural `*hasPermission` oculta botones no autorizados. Con menos de 900 px el sidenav pasa a modo `over`.
- `Formularios`: alta, edición, borrado con diálogo de confirmación y diseñador basado en `FormArray` para secciones/campos. Permite reordenar, elegir los 7 tipos soportados, definir mínimos/máximos, opciones manuales u opciones de catálogo con campo padre dependiente. El botón *Probar opciones* consulta `GET /api/catalogos/{id}/items`.
- `Catálogos`: consulta de ítems (con indicador de padre) e importación `.csv`/`.xlsx` con la opción `reemplazar`.
- Notificaciones con `MatSnackBar`; los errores `ProblemDetails` se muestran usando `detail`, `title` o `code`.
- La API habilita CORS para `http://localhost:4200` y `http://127.0.0.1:4200`.
- Verificación: `cd frontend && npm run build` genera bundles lazy por ruta sin errores.

> Los tests Karma se omitieron al generar el proyecto porque el entorno no dispone de Chrome/Edge. La comprobación se realizó con `npm run build` y una prueba manual en navegador (login, diseñador, guardado, catálogos, importación y vista móvil).

## Renderizador dinámico (Etapa 6)

- Dos vistas separadas: **Mis formularios** (`/app/mis-formularios`) lista los formularios accesibles del usuario y **Administrar formularios** (`/app/formularios`) mantiene el alta/edición. El menú lateral incluye un submenú desplegable bajo *Mis formularios* con acceso directo a cada formulario.
- **Vista previa**: desde el diseñador (o `/app/formularios/:id/vista-previa`) se renderiza el formulario tal como lo verán los usuarios, sin guardar datos.
- **Diligenciamiento**: `/app/mis-formularios/:id/llenar` construye los controles en tiempo de ejecución según el tipo de campo (`Texto`, `Entero`, `Decimal`, `Email`, `Fecha`, `Hora`, `Moneda`, `Porcentaje`, `Lista`) con validación de obligatorios y formato.
- Las listas de catálogo se cargan desde `GET /api/catalogos/{id}/items`; cuando el campo tiene `CampoPadreId`, las opciones se filtran en cascada y se limpia el valor hijo si deja de ser válido.
- Con `EsPasoAPaso = true` se usa `mat-stepper` lineal: *Guardar y continuar* envía `seccionId` (validación parcial en servidor) y el último paso envía `finalizar: true`.
- Los borradores se reanudan automáticamente; `RespuestaListItemDto` incluye `usuarioId` para que Admin no abra por error el borrador de otro usuario.
- Verificación: `dotnet build` (0 advertencias/errores), `dotnet test` (16/16) y `npm run build` (bundles lazy sin errores).

## Reportes y exportaciones (Etapa 7)

- Vista `Reportes` (`/app/reportes`) con filtros por formulario, estado, rango de fechas y paginación.
- Tabla de respuestas guardadas con columnas dinámicas por campo del formulario seleccionado.
- Los valores de catálogo se muestran con su texto histórico (`ValorTexto`), no con el Id.
- Exportación a Excel (ClosedXML) y PDF (QuestPDF) con diseño corporativo (encabezado azul, bordes, paginación en PDF).
- Permisos `Reportes.Ver` y `Reportes.Exportar`; los usuarios no administradores solo ven sus propias respuestas.
- Endpoints: `GET /api/reportes`, `GET /api/reportes/excel`, `GET /api/reportes/pdf`.

## Estructura

```
src/
├── GestionFormulario.Domain        # entidades, enums y contratos (sin dependencias externas)
├── GestionFormulario.Application   # DTOs, validators y casos de uso
├── GestionFormulario.Infrastructure# EF Core, Identity, migraciones, auditoría, seed
└── GestionFormulario.Api           # controllers, middleware y composición
tests/
└── GestionFormulario.Tests
frontend/
└── src/app/
    ├── core/       # modelos, servicios HTTP, interceptor, guards
    ├── features/   # auth, formularios (lista + diseñador), catálogos
    └── shared/     # layout, menú, directiva hasPermission, diálogo
```

## Decisiones destacadas

- Todos los IDs son `Guid`; los enums se persisten como texto para legibilidad.
- `RespuestaValor` usa clave compuesta `(RespuestaId, CampoId)` y guarda `ValorTexto`: una instantánea del texto del ítem de catálogo, de modo que el histórico no cambia si el ítem se edita o se desactiva.
- Las relaciones de catálogos dependientes usan `DeleteBehavior.Restrict` para evitar borrados accidentales.
- `SaveChangesAsync` rellena auditoría (`FechaCreacion`, `CreadoPorId`, etc.) a partir de `ICurrentUserService`.
- El rol Admin omite las comprobaciones de permiso en el cliente, pero el servidor sigue siendo la única fuente de verdad.
- Se incluye `FormularioRol` desde ahora para no necesitar una migración posterior al implementar permisos por formulario.


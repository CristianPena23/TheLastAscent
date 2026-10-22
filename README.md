# Último Descenso (The Last Ascent)

Metroidvania 2D narrativo con plataformeo y combate ágil (parry), inspirado
en la estructura de La Divina Comedia. Un adolescente asciende nueve círculos
de un infierno construido por su propia culpa.

## Requisitos
- Unity **6000.3.12f1** (todos deben usar exactamente esta versión)
- Plantilla Universal 2D
- Git + **Git LFS** instalado (`git lfs install`)
- GitHub Desktop

## Primeros pasos
1. Clonar el repositorio con GitHub Desktop.
2. En Unity Hub: *Agregar → Agregar proyecto desde disco* y elegir la carpeta del repo.
3. Abrir con 6000.3.12f1 y esperar la primera importación.

## Estructura del repositorio
- `Assets/_Project/` → Art, Audio, Prefabs, Scenes, Scripts, Settings
- `ArtSource/` → archivos fuente (PSD, Krita, Spine sin exportar)
- `Docs/` → ficha técnica, diseño, referencias

## Reglas del equipo

### Ramas
- `main` siempre estable. **Nadie hace push directo.**
- Una rama por tarea: `feature/personaje`, `feature/miniboss`, `feature/escenario`.
- Se une a `main` con Pull Request revisado por otro compañero.

### Flujo diario
1. Antes de empezar: **Fetch/Pull**.
2. Trabajar en tu rama.
3. Commits pequeños y frecuentes, con mensajes claros.
4. Al terminar: push y Pull Request.

### Escenas y prefabs
- **Una persona por escena** a la vez. Avisar en el grupo antes de tocar una escena o un prefab compartido.
- Escenas divididas por zona (una por círculo o sección), no una gigante.
- Todo lo reutilizable (enemigos, plataformas, recuerdos) va en **prefabs**.

### Archivos
- Los `.meta` **siempre se suben**.
- No subir `Library/`, `Temp/`, `Logs/`, `UserSettings/`.
- Ajustes del proyecto: Version Control Mode = *Visible Meta Files*, Asset Serialization = *Force Text*.
- Archivos pesados (PSD, audio, video) con Git LFS. Si son demasiado pesados, compartir por Drive.

### Nombres
- Carpetas y archivos sin espacios ni tildes.
- Scripts en `PascalCase` (`PlayerController.cs`).
- Prefabs con prefijo `PF_`, escenas con prefijo `SC_`.

## Roles
- **Personaje, movimiento y combate:** 
- **Escenarios, parallax y niveles:** 
- **Enemigos, mini-boss, arte y animación:**

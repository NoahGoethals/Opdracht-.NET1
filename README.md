# WorkoutCoach (WPF, .NET 9)

Een eenvoudige persoonlijke workout-tracker met een WPF-frontend (XAML) en C#-backend.  
De app laat je oefeningen beheren, workouts samenstellen (incl. reps & gewicht per oefening), sessies registreren en sets bekijken/bewerken. Inloggen/registreren werkt via ASP.NET Core Identity met rollen.

---

## Solution-structuur
WorkoutCoachV2.sln
├─ WorkoutCoachV2.Model # Class Library: modellen, DbContext, EF Core, Identity, migrations, seeding
└─ WorkoutCoachV2.App # WPF desktop app: XAML views, code-behind, services


- **EF Core + Identity**  
  `AppDbContext : IdentityDbContext<ApplicationUser>` met `DbSet<Exercise>`, `DbSet<Workout>`, `DbSet<WorkoutExercise>`, `DbSet<Session>`, `DbSet<SessionSet>`.

- **Belangrijkste modellen**
  - `Exercise` (naam e.d.)
  - `Workout` (titel, datum, …)
  - `WorkoutExercise` (koppelt Workout–Exercise, met **Reps** en **WeightKg**)
  - `Session` (titel, datum, optionele beschrijving)
  - `SessionSet` (échte uitgevoerde set: Exercise, Reps, Weight, Date, IsPr)

---

## Functies (kort)

- **Authenticatie & Rollen**
  - Registreren, inloggen, afmelden.
  - Rollen: **Admin** en **Member**.
  - Gebruikersbeheer (Admin): displayname wijzigen, gebruiker blokkeren, rollen instellen.

- **Exercises**: basis-CRUD.
- **Workouts**: basis-CRUD + **Inhoud beheren**  
  Oefening selecteren → popup **Reps** → popup **Gewicht** → rij verschijnt rechts (Reps/Weight bewerkbaar).
- **Sessions**: basis-CRUD + **Sessie-details**  
  Bestaan er al `SessionSet`-records? Dan worden die getoond.  
  Zo niet: de app **neemt automatisch** de oefeningen uit je **meest recente Workout** over (incl. geplande `WeightKg`) zodat je meteen kunt invullen/bijwerken.
- **Overzicht & Statistiek**: lijst van `SessionSet`-records met inline bewerken en PR-vinkje. (Basisweergave is aanwezig.)

- **Extra vensters (dialogs)**: o.a. *AskRepsWindow*, *AskWeightWindow*, *SessionDetailsWindow*, *EditWorkoutExercisesWindow*.

---

## Systeemvereisten

- **.NET SDK 9.x**
- **SQL Server (LocalDB of SQL Server Express/Developer)**
- Windows 10/11

---

## Installatie & starten (lokaal)

1. **Clone** de repo.
2. Open de solution in **Visual Studio 2022**.
3. Controleer de **connectiestring** in `WorkoutCoachV2.App/appsettings.json` onder `ConnectionStrings:DefaultConnection`.  
   Bij voorkeur zet gevoelige gegevens in **User Secrets**:
   - Rechtermuisklik op **WorkoutCoachV2.App** → **Manage User Secrets**
   - Voeg toe:
     ```json
     {
       "ConnectionStrings": {
         "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=WorkoutCoachV2;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
       }
     }
     ```

4. **Database aanmaken/updaten** (migrations zitten in het **Model**-project, App is Startup):
   - **Package Manager Console**
     ```powershell
     # Zorg dat StartupProject = WorkoutCoachV2.App
     # en Default Project = WorkoutCoachV2.Model (of geef -Project mee)

     Update-Database -StartupProject WorkoutCoachV2.App
     ```
   - Of via **EF CLI**:
     ```bash
     dotnet ef database update --project WorkoutCoachV2.Model --startup-project WorkoutCoachV2.App
     ```

5. **Run** de WPF-app (`WorkoutCoachV2.App` als Startup).

---

## Eerste login / seeding

De seeder maakt rollen **Admin** en **User** aan, een **admin-account** en enkele **Exercises**.

- **Admin**:  
  - **User**: `admin` of `admin@local`  
  - **Wachtwoord**: `Admin!123`

Log in als admin om gebruikers/rollen te beheren.

---

## Waar staat wat?

- **DbContext & Migrations**  
  `WorkoutCoachV2.Model/Data/AppDbContext.cs`  
  Migrations leven **in het Model-project** (vereiste van de opdracht).
- **Seeding**  
  `WorkoutCoachV2.Model/Data/Seed/DbSeeder.cs` (rollen, admin-user, voorbeeld-oefeningen).
- **Views (WPF)**  
  `WorkoutCoachV2.App/View/*` (o.a. `ExercisesView`, `WorkoutsView`, `SessionsView`, `StatsView`, `UserAdminWindow`, `EditWorkoutExercisesWindow`, `SessionDetailsWindow`, `AskRepsWindow`, `AskWeightWindow`).

---

## Bronnen
https://chatgpt.com/share/690f7b51-8698-8009-a3fe-5e1c69ed8e49
https://chatgpt.com/share/690f8196-5fe0-8009-9ac6-a07a36331e91
https://chatgpt.com/share/690f81be-e1f8-8009-9367-5e6a87085108
https://chatgpt.com/share/690f81d9-fe0c-8009-81f2-da0e6903e456
https://chatgpt.com/share/690f7b51-8698-8009-a3fe-5e1c69ed8e49
https://chatgpt.com/share/690f844f-f0c0-8009-bdd7-112c562a2ce0
https://chatgpt.com/share/690f8451-c46c-8009-91f1-2044fc1b69dc
https://chatgpt.com/share/690f8458-9644-8009-a4e3-a43bbaf0d5d7
https://chatgpt.com/share/690f81be-e1f8-8009-9367-5e6a87085108
https://chatgpt.com/share/690f8454-4f0c-8009-83d5-de1f39157fc4
https://chatgpt.com/share/6910875b-3738-8009-a924-aec76e54ad78
https://chatgpt.com/share/6910875e-b914-8009-8e27-6f3a5f4804e2

Copilot af en gevraagd voor advies.


# WorkoutCoach – Web (ASP.NET Core MVC + REST API)

Dit project is het **webdeel** van het examenproject (.NET Advanced).  
Het is een **ASP.NET Core MVC** applicatie op **.NET 9** met:

- Razor + Bootstrap UI
- Entity Framework Core + SQL Server (Azure SQL in productie / LocalDB lokaal)
- **Identity** met **custom user** (`ApplicationUser` met `DisplayName` + `IsBlocked`)
- **3 rollen**: `Admin`, `Moderator`, `User` (User wordt automatisch toegekend bij registratie)
- **meertaligheid** (NL/EN/FR) via `IStringLocalizer`
- **REST API** (JWT) voor de MAUI-app
- eigen middleware: **BlockedUserMiddleware**
- logging + foutafhandeling (try/catch + user feedback via TempData)

---

## Live Azure omgeving (publiek)

- **Web App (App Service):** `workoutcoachv2-web-noah1`
- **Base URL:** `https://workoutcoachv2-web-noah1.azurewebsites.net/`
- **Azure SQL Database:** `WorkoutCoachV2Db` op SQL Server `workoutcoachv2-sql-noah1`

“De databank is cloud-hosted op Azure SQL (dus niet lokaal) en via het internet bereikbaar
als dienst,maar toegang is beveiligd met credentials en firewallregels.”

> De app gebruikt de connection string die in Azure App Settings staat (niet in GitHub).

---

## Functionaliteiten (web)

### 1) Oefeningen (Exercises)
- CRUD (Create/Read/Update/Delete) per gebruiker (**OwnerId**)
- **Soft delete** (`IsDeleted`)
- **Zoeken + filter op categorie + sorteren**
- Validatie via DataAnnotations (bv. required/max length)

### 2) Workouts
- CRUD per gebruiker (**OwnerId**) + soft delete
- **Koppelen van oefeningen aan workouts** met extra velden:
  - `Reps`
  - `WeightKg`
- Beheerpagina om oefeningen toe te voegen/verwijderen uit een workout

### 3) Sessions (training logs)
- CRUD per gebruiker (**OwnerId**) + soft delete
- Session bevat meerdere **SessionSets** (oefening + setnummer + reps + gewicht)
- **Nieuw session kan automatisch sets opbouwen uit geselecteerde workout(s)** (template)

### 4) Stats (AJAX)
- Stats-pagina met:
  - selectie van oefening
  - datum-range
  - berekeningen: sets, reps, totaal volume, max gewicht
- **Asynchrone (AJAX) view**: resultaat wordt geladen via `fetch()` en een partial view

### 5) Admin (rollen + blokkeren)
- Admin-pagina zichtbaar voor **Admin en Moderator** in het menu
- User management:
  - lijst van gebruikers
  - rol toekennen (Admin/Moderator/User)
  - block/unblock (met bescherming tegen self-block)
- **BlockedUserMiddleware** logt user automatisch uit als `IsBlocked = true`

### 6) Meertaligheid
- Dropdown in navbar: `nl`, `en`, `fr`
- Taalkeuze wordt bewaard in een **culture cookie**
- Teksten zitten in `Resources/SharedResource.*.resx`

---

## REST API (voor MAUI)

De API gebruikt **JWT Bearer authentication**.

### Auth
- `POST /api/auth/register`  → registreert + geeft JWT terug
- `POST /api/auth/login`     → login + JWT
- `GET  /api/auth/me`        → user info + roles
- `POST /api/auth/logout`    → client-side (token vergeten)

### Exercises
- `GET    /api/exercises?search=&category=&sort=`
- `GET    /api/exercises/{id}`
- `POST   /api/exercises`
- `PUT    /api/exercises/{id}`
- `DELETE /api/exercises/{id}`

### Workouts
- `GET    /api/workouts?search=&sort=`
- `GET    /api/workouts/{id}`
- `POST   /api/workouts`
- `PUT    /api/workouts/{id}`
- `DELETE /api/workouts/{id}`

### WorkoutExercises (koppeltabel)
- `GET /api/workouts/{workoutId}/exercises`
- `PUT /api/workouts/{workoutId}/exercises` → **ReplaceAll** (sync-friendly)

### Sessions (+ sets)
- `GET    /api/sessions?search=&from=&to=&sort=&includeSets=true|false`
- `GET    /api/sessions/{id}`
- `POST   /api/sessions`     → met sets
- `PUT    /api/sessions/{id}`→ replace sets
- `DELETE /api/sessions/{id}`

### Admin API (enkel Admin)
- `GET  /api/admin/users`
- `POST /api/admin/users/{id}/toggle-block`
- `PUT  /api/admin/users/{id}/role`

### Health
- `GET /api/ping`  
  Geeft `200` of `401` terug. `401` betekent dat de API bereikbaar is maar je niet geauthenticeerd bent.

---

## Database / Class Library

De database + modellen zitten in **WorkoutCoachV2.Model** (Class Library):

- `Exercise`
- `Workout`
- `WorkoutExercise` (many-to-many)
- `Session`
- `SessionSet`
- Identity tables (AspNetUsers/Roles/...)

**Seeding bij opstart (DbSeeder):**
- Rollen: Admin/Moderator/User
- Users:
  - `admin@local` / `Admin!123`
  - `moderator@local` / `Moderator!123`
  - `user@local` / `User!123`
- Demo data (exercises + “Full Body A” workout) voor (minstens) admin

---

## Bronnen
https://chatgpt.com/share/69526e2c-3d54-8009-999d-600a561fa60f
https://chatgpt.com/share/69526d38-6f0c-8009-a4e5-2ad15a243eb6
https://chatgpt.com/share/69526f89-6568-8009-9e68-8ae323ff543c
https://chatgpt.com/share/69527104-8bd4-8009-83b9-31aca9309fe4
https://chatgpt.com/share/69527ecb-bf3c-8009-84c1-05812c085165
https://chatgpt.com/share/69527ab5-cc94-8009-80e9-db900924257d
https://chatgpt.com/share/69527b86-dc5c-8009-ae1b-e584d8361297
https://chatgpt.com/share/69527ccf-b528-8009-9f48-03295f6663a9
https://chatgpt.com/share/69527ca4-0dbc-8009-b0d3-5ec0a007ac5e
https://chatgpt.com/share/69527c84-5bdc-8009-8b6f-45ff5fe987ad
https://chatgpt.com/share/69527dfe-b694-8009-a354-920ef2138680
https://chatgpt.com/share/69527e4f-e610-8009-b54a-1dd74a846e3c
https://chatgpt.com/share/69527e98-b170-8009-989b-29c0de0c827e
https://chatgpt.com/share/69527ecb-bf3c-8009-84c1-05812c085165
https://chatgpt.com/share/6952811b-cd54-8009-8781-85d47276fcab
https://chatgpt.com/share/69527b86-dc5c-8009-ae1b-e584d8361297
https://chatgpt.com/share/6952804b-552c-8009-85ea-43d32de693e2
https://chatgpt.com/share/6952818b-ca4c-8009-989b-20750815f0a1
https://chatgpt.com/share/695282c9-4214-8009-b64d-d993e4840a67
https://chatgpt.com/share/6952832d-dea8-8009-b6c5-810ab12ff526
https://chatgpt.com/share/695282dc-e7c0-8009-b872-6b5bb7e366bc
https://chatgpt.com/share/69528358-08d4-8009-b353-8f922fdee1db
https://chatgpt.com/share/6952837c-1294-8009-b170-ead71aa06c0b
https://chatgpt.com/share/69527ecb-bf3c-8009-84c1-05812c085165
https://chatgpt.com/share/6952843f-4e30-8009-98d2-0c49efcd3222
https://chatgpt.com/share/69528552-1778-8009-b8f5-effbf68dbc54
https://chatgpt.com/share/695287bf-52ec-8009-a22b-ae6f060d861a
https://chatgpt.com/share/69528861-2150-8009-b5f4-bf4120a0e7e8

https://www.youtube.com/watch?v=6joGkZMVX4o
https://www.youtube.com/watch?v=9ur0OpMADuM

-  De theorie van canvas is gebruikt geweest.
-  Microsoft Azure.
-  Microsoft Learn / .NET Docs
-  Copilot bij het fixen van errors.


# WorkoutCoach (MAUI) – Mobile app (Android + Windows)

Dit project is de **.NET MAUI** (mobile/desktop) versie van mijn WorkoutCoach webapplicatie.  
De app werkt **online én offline** met een **lokale SQLite database** en **automatische synchronisatie** met de REST API van de ASP.NET Core webapp.

- Solution: `WorkoutCoachV2.Model.sln`
- MAUI project: `WorkoutCoachV3.Maui`
- Web/API project (nodig voor online): `WorkoutCoachV2.Web`
- Class Library (gedeelde modellen + API contracts): `WorkoutCoachV2.Model`

---

## Belangrijk (API + Azure)

De app verbruikt de REST API van de webapp. Standaard staat de Base URL al ingesteld op mijn Azure deployment:

- **Default API Base URL:** `https://workoutcoachv2-web-noah1.azurewebsites.net/`

Je kan dit in de app aanpassen via:
- **Login scherm → “API Settings”**

Daar wordt de Base URL opgeslagen in **Preferences** (lokaal op het toestel).

> ℹ️ Tip voor Android emulator met local API: gebruik **`http://10.0.2.2:5162/`** (met `http://`!), want `localhost` verwijst in de emulator naar zichzelf.

---

## Vereisten

- Visual Studio 2022 (met **.NET MAUI workload**)
- .NET **9.x**
- Android emulator of fysieke Android-telefoon (Windows build kan ook)
- Werkende API (Azure of lokaal)

---

## Starten (stap-voor-stap)

1. Open de solution:
   - `WorkoutCoachV2.Model.sln`

2. Zet **Startup Project** op:
   - `WorkoutCoachV3.Maui`

3. Kies target:
   - **Android Emulator** (aanbevolen voor demo)
   - of **Windows Machine**

4. Start de app (F5).

5. Bij eerste start:
   - Je ziet **Login**
   - Optioneel: klik **API Settings** om Base URL te wijzigen
   - Log in met een bestaande gebruiker (web) of maak een account aan via **Create account**

---

## Functionaliteiten (wat kan je in de app doen)

### 1) Authenticatie (via API + Identity)
- **Login** met email + password (`api/auth/login`)
- **Register** (create account) (`api/auth/register`)
- Na login wordt een **JWT token** bewaard (SecureStorage + Preferences fallback)
- Bij volgende opstart:
  - als token nog geldig is → **automatisch her-aanmelden**
  - app haalt user info op via `api/auth/me` (roles + display name)

### 2) Navigatie / UI
Bovenaan werkt de app met een horizontaal “tab” menu (knoppen):
- **Exercises**
- **Workouts**
- **Sessions**
- **Stats**
- **Admin** (alleen zichtbaar als je role `Admin` of `Moderator` hebt)
- **Add** (snelle create)
- **Logout**

De UI is gemaakt voor een standaard Android-scherm en werkt ook op Windows.

### 3) Exercises (offline + sync)
- Overzicht met:
  - **Search** op naam
  - **Category filter** (Picker)
- Acties:
  - **Add**
  - **Edit** (swipe)
  - **Delete** (swipe)
- Sync met API:
  - `GET/POST/PUT/DELETE api/exercises`

### 4) Workouts + WorkoutExercises (offline + sync)
- Workouts CRUD:
  - search in lijst
  - add/edit/delete
  - detail page
- Workout detail bevat “Manage exercises”:
  - selecteer oefeningen in de workout
  - stel reps/gewicht in
  - server sync gebeurt via bulk replace:
    - `GET api/workouts/{id}/exercises`
    - `PUT api/workouts/{id}/exercises` (ReplaceAll)

### 5) Sessions (offline + sync)
- Sessions lijst:
  - **Search**
  - **Tap** om details te openen
  - **Edit/Delete** via swipe
- Een session wordt opgebouwd uit **1 of meerdere geselecteerde workouts**:
  - op de edit/create page kies je workouts (checkbox)
  - de app genereert de sets op basis van de workout-oefeningen
- Sync met API:
  - `GET/POST/PUT/DELETE api/sessions`
  - ondersteunt `includeSets=true` voor pull

### 6) Stats (lokaal berekend + filters)
- Stats worden lokaal berekend vanuit de SQLite data:
  - **Sessions count**
  - **Sets count**
  - **Total reps**
  - **Total volume (kg)**
- Filters:
  - datum range (From/To)
  - exercise filter (All exercises of één exercise)
- “Top exercises” tabel met per oefening o.a. volume en max gewicht

### 7) Admin (alleen voor Admin/Moderator)
- User lijst via API:
  - **role wijzigen**
  - **block/unblock**
- Endpoints:
  - `GET api/admin/users`
  - `POST api/admin/users/{userId}/toggle-block`
  - `PUT api/admin/users/{userId}/role`

---

## Offline werking & automatische synchronisatie

### Lokale database (SQLite)
De app gebruikt een lokale SQLite database in de app data folder:
- bestand: `workoutcoach.local.db3`
- EF Core: `LocalAppDbContext` met tabellen:
  - `Exercises`
  - `Workouts`
  - `WorkoutExercises` (koppeltabel)
  - `Sessions`
  - `SessionSets`

Elke lokale entity heeft sync-velden via `BaseLocalEntity`:
- `LocalId` (Guid)
- `RemoteId` (id op server)
- `IsDeleted` (soft delete)
- `SyncState` (`Dirty` / `Synced`)
- `LastModifiedUtc`, `LastSyncedUtc`

### Offline seed (zodat app bruikbaar blijft zonder internet)
Als er **geen internet** is en de database is leeg, seed de app basisdata:
- Exercises: “Bench Press”, “Back Squat”, “Barbell Row”
- Workout: “Full Body A”

### Synchronisatie flow
- Sync draait **asynchroon** en “best effort” (geen crash als API down is)
- Bij internet:
  1) **Push** lokale wijzigingen (Dirty/Deleted) naar de API
  2) **Pull** server data naar lokaal en merge op `RemoteId`
- Er is ook een connectivity watcher:
  - wanneer internet terugkomt → sync start automatisch (debounced)
- Bij `401 Unauthorized`:
  - token + session worden automatisch gewist → app gaat terug naar login

---

## Configuratie (waar zit wat in de code)

Belangrijkste bestanden in `WorkoutCoachV3.Maui`:

- `MauiProgram.cs`
  - DI setup (HttpClientFactory, AuthHeaderHandler, services, viewmodels, pages)
  - SQLite DbContextFactory
  - API clients (`Api` + `ApiNoAuth`)

- `Services/ApiConfig.cs`
  - Default Azure Base URL + opslag override (Preferences)

- `Services/TokenStore.cs`
  - token opslag + expiry check (SecureStorage + Preferences fallback)

- `Services/SyncService.cs`
  - push/pull sync engine

- `Services/ConnectivitySyncService.cs`
  - triggert sync wanneer internet terugkomt

- `Services/LocalDatabaseService.cs`
  - lokale CRUD + offline seed + stats queries

---

## GDPR / privacy

- Er wordt enkel minimale informatie lokaal opgeslagen:
  - JWT token + expiry (SecureStorage/Preferences)
  - user session info (id/email/display name/roles) in Preferences
  - trainingsdata in lokale SQLite database
- Logout wist token + session info.
- Geen hardcoded geheime API keys in de app.

---


## Bronnen / gebruikte packages (NuGet)

- CommunityToolkit.Maui
- CommunityToolkit.Mvvm
- Microsoft.EntityFrameworkCore.Sqlite
- Microsoft.Extensions.Http
- Microsoft.Extensions.Logging.Debug

(Alle packages staan in de `.csproj` van `WorkoutCoachV3.Maui`.)

---

## Link met Web project

Deze app is afhankelijk van de API uit `WorkoutCoachV2.Web`:
- Auth: `api/auth/login`, `api/auth/register`, `api/auth/me`
- CRUD: `api/exercises`, `api/workouts`, `api/sessions`
- Admin: `api/admin/users`, ...

De data contracts komen uit de Class Library:
- `WorkoutCoachV2.Model.ApiContracts`

## Bronnen
https://chatgpt.com/share/69526e2c-3d54-8009-999d-600a561fa60f
https://chatgpt.com/share/69526d38-6f0c-8009-a4e5-2ad15a243eb6
https://chatgpt.com/share/69526f89-6568-8009-9e68-8ae323ff543c
https://chatgpt.com/share/69527104-8bd4-8009-83b9-31aca9309fe4
https://chatgpt.com/share/69527ecb-bf3c-8009-84c1-05812c085165
https://chatgpt.com/share/69527ab5-cc94-8009-80e9-db900924257d
https://chatgpt.com/share/69527b86-dc5c-8009-ae1b-e584d8361297
https://chatgpt.com/share/69527ccf-b528-8009-9f48-03295f6663a9
https://chatgpt.com/share/69527ca4-0dbc-8009-b0d3-5ec0a007ac5e
https://chatgpt.com/share/69527c84-5bdc-8009-8b6f-45ff5fe987ad
https://chatgpt.com/share/69527dfe-b694-8009-a354-920ef2138680
https://chatgpt.com/share/69527e4f-e610-8009-b54a-1dd74a846e3c
https://chatgpt.com/share/69527e98-b170-8009-989b-29c0de0c827e
https://chatgpt.com/share/69527ecb-bf3c-8009-84c1-05812c085165
https://chatgpt.com/share/6952811b-cd54-8009-8781-85d47276fcab
https://chatgpt.com/share/69527b86-dc5c-8009-ae1b-e584d8361297
https://chatgpt.com/share/6952804b-552c-8009-85ea-43d32de693e2
https://chatgpt.com/share/6952818b-ca4c-8009-989b-20750815f0a1
https://chatgpt.com/share/695282c9-4214-8009-b64d-d993e4840a67
https://chatgpt.com/share/6952832d-dea8-8009-b6c5-810ab12ff526
https://chatgpt.com/share/695282dc-e7c0-8009-b872-6b5bb7e366bc
https://chatgpt.com/share/69528358-08d4-8009-b353-8f922fdee1db
https://chatgpt.com/share/6952837c-1294-8009-b170-ead71aa06c0b
https://chatgpt.com/share/69527ecb-bf3c-8009-84c1-05812c085165
https://chatgpt.com/share/6952843f-4e30-8009-98d2-0c49efcd3222
https://chatgpt.com/share/69528552-1778-8009-b8f5-effbf68dbc54
https://chatgpt.com/share/695287bf-52ec-8009-a22b-ae6f060d861a
https://chatgpt.com/share/69528861-2150-8009-b5f4-bf4120a0e7e8

https://www.youtube.com/watch?v=6joGkZMVX4o
https://www.youtube.com/watch?v=9ur0OpMADuM

-  De theorie van canvas is gebruikt geweest.
-  Microsoft Azure.
-  Microsoft Learn / .NET Docs
-  Copilot bij het fixen van errors.

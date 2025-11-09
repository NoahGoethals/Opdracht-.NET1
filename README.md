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
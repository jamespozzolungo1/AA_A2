# AI Study Planner

AI Study Planner is a desktop application built with C# and Windows Forms. It is designed to help students organise study tasks, manage assignments and timetables, and ask an AI assistant for study support.

> **Project note:** This README documents the setup that can be identified from the current project context. Check `DatabaseService.cs` for the exact database connection configuration and the project file (`.csproj`) for the target .NET version and NuGet dependencies before running the application.

## Features

- **AI Chat:** Sends conversation history to the DeepSeek chat completions API and displays the assistant's response.
- **Chat History:** Loads and saves user/assistant messages in the `chatmessages` database table.
- **Timetable Management:** Supports retrieving, adding, updating, and deleting timetable entries.
- **Study Planning and Assignments:** The project includes study-planning and assignment-related backend services and pages.
- **Timetable Import:** CSV timetable import is part of the planned/implemented project workflow; check the current UI before relying on a specific CSV format.

## Technology Stack

- C#
- .NET / Windows Forms
- MySQL
- `MySqlConnector`
- DeepSeek API for AI chat
- Visual Studio

Check the `.csproj` file to confirm the exact target framework and package versions.

## Prerequisites

Install or prepare the following:

1. **Visual Studio** with the .NET desktop development workload.
2. The .NET SDK version required by the project file.
3. A running MySQL server.
4. Access to the project's database and its required tables.
5. A DeepSeek API key if you want to use AI Chat.

## Setup

### 1. Open the project

1. Download or clone the project repository.
2. Open `AIStudyPlannerApp.sln` in Visual Studio, if the solution file is present. Otherwise, open the `.csproj` file.
3. Allow Visual Studio to restore NuGet packages.
4. Confirm the target framework shown in the project file is installed.

### 2. Configure the database

The backend creates database connections through `DatabaseService`.

1. Open `Backend/DatabaseService.cs` (the folder may differ in your project).
2. Find how the connection string is loaded.
3. Configure the MySQL host, port, database name, username, and password using the method expected by that class.
4. Do not commit real database passwords or other secrets to source control.
5. Make sure the database contains the tables required by the application.

The timetable service uses a `TimetableItems` table with these fields:

- `TimetableItemId`
- `UserId`
- `CourseName`
- `DayOfWeek`
- `StartTime`
- `EndTime`
- `Location`
- `Notes`

The chat history service uses a `chatmessages` table with these fields:

- `ChatMessageId`
- `UserId`
- `Role`
- `Content`
- `CreatedAt`

Other application features may require additional tables. Use the project's database script or schema definition if one is provided. **The table lists above are not a complete database schema.**

### 3. Configure the DeepSeek API key

`AIService` reads the API key from the `DEEPSEEK_API_KEY` environment variable.

On Windows, open PowerShell and run the following command, replacing the example value with your own key:

```powershell
[Environment]::SetEnvironmentVariable("DEEPSEEK_API_KEY", "your_deepseek_api_key", "User")
```

Then completely close and reopen Visual Studio so the application can read the updated environment variable.

Do not paste your real API key into source code, README files, screenshots, or public repositories.

The current service sends requests to:

- Endpoint: `https://api.deepseek.com/chat/completions`
- Model configured in code: `deepseek-flash`

If the API provider changes its available model names, check the current provider documentation and update the `Model` constant if necessary.

## Run the Application

1. Start MySQL and confirm the database configuration is correct.
2. Configure `DEEPSEEK_API_KEY` if you want to use AI Chat.
3. Open the solution or project in Visual Studio.
4. Select the desktop application project as the startup project.
5. Choose **Build > Build Solution**.
6. Fix any build errors reported by Visual Studio.
7. Press **F5** to run with debugging, or **Ctrl+F5** to run without debugging.

The exact startup project and startup form depend on the current solution configuration.

## Basic Verification

After the application starts, check the features available in the current UI:

- Open the timetable page and try loading or adding a timetable entry.
- Try updating and deleting a test entry.
- Open the assignment or study-planning page and verify that its data loads.
- Open AI Chat and send a short test question.
- Restart the application and check whether chat history is loaded again.

Use test data while verifying database operations.

## Troubleshooting

### `DEEPSEEK_API_KEY is missing`

- Confirm that the environment variable is set for your Windows user.
- Restart Visual Studio after setting it.
- Check that the variable name is exactly `DEEPSEEK_API_KEY`.

### MySQL connection errors

- Confirm that the MySQL server is running.
- Check the connection settings used by `DatabaseService`.
- Verify the database name, username, password, host, and port.
- Confirm that the required tables exist and that the database user has permission to access them.

### AI request returns an HTTP error

- Check that the API key is valid and has access to the requested model.
- Check your internet connection and the provider's service status.
- Read the HTTP status and response body reported by the application.
- Confirm that the model configured in `AIService` is currently supported by the API.

### Build errors in Visual Studio

- Restore NuGet packages.
- Confirm that the required .NET SDK is installed.
- Check the **Error List** and **Output** windows for the first relevant error.
- Confirm that referenced backend classes and namespaces are included in the project.

## Security Notes

- Keep API keys and database passwords out of source control.
- Use SQL parameters for user-supplied values.
- Use test accounts and test data during development.
- Do not publish real user chat messages or database credentials in screenshots or logs.

## Project Structure

The project currently includes frontend pages and backend services. Names seen in the code include:

- `AIService.cs` — sends chat requests to DeepSeek.
- `ChatHistoryService.cs` — loads and saves chat history.
- `TimetableService.cs` — manages timetable records.
- `DatabaseService.cs` — creates database connections.

Other pages and services may include assignment management and study planning. The exact folder layout can vary by project version.

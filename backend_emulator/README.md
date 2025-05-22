## Backend Setup

### Run Instructions

Ensure you are in `backend_emulator\hippo-api`

Note: Instructions prefixed to move to hippo-api (so you can just click run button from readme)

```bash
cd hippo-api/
```

```bash 
firebase emulators:start
```

Run in `hippo-api/` folder

```bash
dotnet run
```

### Prerequisites

- .NET 8.0 SDK or later
- Google.Cloud.Firestore v3.8.0
- Microsoft.AspNetCore.OpenApi v8.0.8
- Swashbuckle.AspNetCore v6.4.0
- Firebase CLI v9.23.0

### Testing Prerequisites

- NUnit v3.14.0
- NUnit.Analyzers v3.9.0
- NUnit3TestAdapter v4.5.0
- Microsoft.AspNetCore.Mvc.Testing v8.0.8
- coverlet.collector v6.0.0

### Installation

1. Install dependencies by running `dotnet restore` (may have to be run in both hippo-api and tests folders separately).
2. Install Firebase CLI by running ```npm install -g firebase-tools```.
3. Install and launch the Firestore emulator by running

```bash 
firebase emulators:start --only firestore
```

Note: If setup dialogue appears, choose to install firestore, use "emulator-id" for project id, and choose default
options for everything else.

### Usage

1. From root directory, ensure Firestore emulator is launched by running
   firebase emulators:start```bash

```
![setup_firebase_1.png](assets/read_me/setup_firebase_1.png)

1. From the root directory (backend_emulator), cd into todo-api directory.
3. Run command "dotnet run" to start the server. Server should launch, listening on http://localhost:5000.
   ![setup_firebase_2.png](assets/read_me/setup_firebase_2.png)

4. To run tests, cd into tests directory and run command

```bash
dotnet test
```

5. To test the API endpoints manually, use Postman or any other API testing tool.

####

6. Send a GET request to https://localhost:5000/todo to get all todo items currently stored in
   firestore.  
   Ex. curl http://localhost:5000/todo via cmd

####

7. Send a POST request to https://localhost:5000/todo with valid ToDoItem information
   to add a new todo item to firestore db.     
   ex.
   `curl -X POST http://localhost:5000/todo -H "Content-Type: application/json" -d "{\"Title\":\"Example Task\",\"Description\":\"Example task description\"}"`
   via cmd

### Folder and File Naming Conventions

- Folder names are in lowercase and use dashes to separate words.
- File names are in PascalCase and use no spaces or dashes.

### Folder Structure

/backend - contains version of the API that connects to the actual Firestore database. (No longer in use)

/backend_emulator - contains version of the API that connects to a Firestore emulator. (Current version)  
/config - contains configuration files (firebase-admin token - not in use in emulator).  
/controllers - contains the API controller.  
/models - contains the model for the ToDoItem.  
/services - contains the service with a direct reference to the Firestore db and methods to interact with it.  
/properties - contains launchSettings.json which specifies the port the API runs on.  
appsettings.json - contains configuration settings for the API.  
Program.cs - contains the main method for the API. Starts the API and seeds the Firestore emulator with data.  
/tests - contains unit tests for the controller and model.

### Resources

https://cloud.google.com/dotnet/docs/reference/Google.Cloud.Firestore/latest  
https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-8.0#make-post-put-and-delete-requests

@!format markdown

# GritTrack
Language / platform: <wake reason="mention" current-time="2026-11-11T21:45:54Z">
  <project id="chan_01FtH8TpydyXjTqnc1HJehEg" type="project">
    <message trigger="true" from="human" trust="principal" author-id="user_01EYbYUsoncWvQ7XU1kUNaKV" id="cmsg_01FtH8TpydyXjTqnc1HJehEgURqm3aGsLpQpK7cH4R6BTU" sent-at="2026-11-11T21:45:40Z" mention="true">for grit track and also use the new location to work on. its empty. i got aa new computer and so im really depending on you to get me my projects in full on here

Consider implementing these tests if applicable:
1. SQL Server repository, migrations, startup migration retries, and SQL configuration are tested
2. MAUI authentication flow, bearer-token handler, API response parsing, and local assignment persistence are tested
3. Postman instruction files are structurally valid and contain no contradictory or undefined examples</message>
  </project>
</wake>
Market: <wake reason="mention" current-time="2026-11-11T22:21:17Z">
  <project id="chan_01FtH8TpydyXjTqnc1HJehEg" type="project">
    <thread ts="cmsg_01FtH8TpydyXjTqnc1HJehEgURqm3aGsLpQpK7cH4R6BTU">
      <message trigger="true" from="human" trust="principal" author-id="user_01EYbYUsoncWvQ7XU1kUNaKV" id="cmsg_01FtH8TpydyXjTqnc1HJehEgVUegEv2hzzgfzrRrtuVEiN" sent-at="2026-11-11T22:21:17Z" mention="true">Consider implementing these tests if applicable:
1. Workflow successfully restores, tests, builds, signs, and packages the actual desktop solution.
2. The generated MSIX package is uploaded as a workflow artifact.</message>
    </thread>
  </project>
  <system-note>If one quick read gets you the answer, just answer. Otherwise, before any other tool, instead of a reaction, send one `reply` of at most 20 words saying your first step without restating the message, so the person is not left waiting.</system-note>
</wake>
Timeframe / style: <wake reason="mention" current-time="2026-11-11T22:24:24Z">
  <project id="chan_01FtH8TpydyXjTqnc1HJehEg" type="project">
    <thread ts="cmsg_01FtH8TpydyXjTqnc1HJehEgURqm3aGsLpQpK7cH4R6BTU">
      <message trigger="true" from="human" trust="principal" author-id="user_01EYbYUsoncWvQ7XU1kUNaKV" id="cmsg_01FtH8TpydyXjTqnc1HJehEgXbjAzV4WveE2dg1F43c86h" sent-at="2026-11-11T22:24:24Z" mention="true">https://github.com/MavScriptBlu/GritTrack/pull/93</message>
    </thread>
  </project>
  <system-note>If one quick read gets you the answer, just answer. Otherwise, before any other tool, instead of a reaction, send one `reply` of at most 20 words saying your first step without restating the message, so the person is not left waiting.</system-note>
</wake>
Data source: <wake reason="mention" current-time="2026-11-11T22:41:48Z">
  <project id="chan_01FtH8TpydyXjTqnc1HJehEg" type="project">
    <thread ts="cmsg_01FtH8TpydyXjTqnc1HJehEgURqm3aGsLpQpK7cH4R6BTU">
      <message trigger="true" from="human" trust="principal" author-id="user_01EYbYUsoncWvQ7XU1kUNaKV" id="cmsg_01FtH8TpydyXjTqnc1HJehEg7TcgLTZwv9eqEo3u4MWc5A" sent-at="2026-11-11T22:41:48Z" mention="true">i just want to be done with this assignment man... its so so late. and theres like a few more that build off of it. im lost in how to run it. where it even exists. and what to even turn in anymore.</message>
    </thread>
  </project>
  <system-note>If one quick read gets you the answer, just answer. Otherwise, before any other tool, instead of a reaction, send one `reply` of at most 20 words saying your first step without restating the message, so the person is not left waiting.</system-note>
</wake>
Output format: <wake reason="mention" current-time="2026-11-11T22:52:28Z">
  <project id="chan_01FtH8TpydyXjTqnc1HJehEg" type="project">
    <thread ts="cmsg_01FtH8TpydyXjTqnc1HJehEgURqm3aGsLpQpK7cH4R6BTU">
      <message trigger="true" from="human" trust="principal" author-id="user_01EYbYUsoncWvQ7XU1kUNaKV" id="cmsg_01FtH8TpydyXjTqnc1HJehEgTq87c2iLsSniDKCsKAR1fq" sent-at="2026-11-11T22:52:28Z" mention="true">[Skip to main content](https://alt-5b074caa51830.blackboard.com/bbcswebdav/pid-7045789-dt-content-rid-35545660_1/courses/126662038_26/READ_ONLY/content/_7045789_1/embedded/UNSAFE_HTML/UNSAFE_HTML_20260930125926.html?one_hash=605B42B9A4BF545D1E2D601546464679&amp;f_hash=C61C359DE1C78A0DF94C99A91CC3C14E#main)
WEEK 6 ASSIGNMENTSEMESTER API · PART 1 OF 2
Semester API, Part 1: Design and Build Your API
This week you build the API your mobile app will talk to for the rest of the semester. It stores data in a JSON file, uses the same five layers as the speedrun leaderboard, and ships with a Postman collection that proves every endpoint works, including the ones that are supposed to fail.
1What You&#39;re Building
You&#39;re building the back end for your own semester app, not the leaderboard. Everything in the Week 6 pages applies directly: swap &#34;runs&#34; for your resource and follow the same structure. If you get stuck, find the matching section in the pages and compare your code to the leaderboard&#39;s.
Part 1 is JSON file storage only. Part 2 next week adds SQL Server in Docker, a configuration flag to switch between the two, and logging. If you build Part 1 the way the pages show, Part 2 is a new repository class and one changed line in `Program.cs`. If you take shortcuts here, Part 2 turns into a rewrite.
You don&#39;t touch the MAUI app this week or next. The app starts calling your API once the API is finished.
2Pick Your Primary Resource
Your app probably has several kinds of data. For Part 1, pick one primary resource, the thing your app is mostly about, and build it completely. A finished resource with every layer, rule, and test beats three half-built ones. Here&#39;s what that looks like for the most common project types:
Recipe Collection
Primary: `/api/recipes`: name, ingredients, instructions, image URL, prep minutes.
Business rule idea: no two recipes with the same name (409).
Later: categories, favorites
Personal Finance
Primary: `/api/transactions`: date, amount, category, description.
Business rule idea: no duplicate transaction with the same date, amount, and description (409).
Later: budget categories, goals
Fitness Tracker
Primary: `/api/workouts`: date, type, duration minutes, calories burned.
Business rule idea: only one workout of the same type per day (409).
Later: meals, progress metrics
Your project is something else? Good. Apply the same pattern: one resource, full CRUD, and one business rule that depends on existing data. If you&#39;re not sure which resource is &#34;primary,&#34; ask me before you start building.
3Requirements
Each group below maps to a Week 6 page. Every box is something I check when grading.
A. Design firstPage 1

* Your repository has a `README.md` with an endpoint table: method, URL, what it does, success code, and failure codes, in the same format as the leaderboard table.
* The endpoint table is committed before your controller exists. I&#39;ll check the commit history.
* URLs use plural nouns and no verbs: `/api/recipes/{id}`, not `/api/getRecipe`.

B. Project and layersPage 2

* ASP.NET Core Web API on .NET 10, with controllers and OpenAPI enabled. .NET 8 and 9 reach end of support on November 10, 2026, mid-semester, so they aren&#39;t accepted.
* The WeatherForecast sample files are deleted.
* Folders: `Controllers`, `DTOs`, `Models`, `Repositories`, `Services`, `Data`.
* A Model with at least one field the client can&#39;t set, such as an id, a created date, or a server-controlled flag.
* Separate request and response DTOs. The controller never accepts or returns the Model.
* Interfaces for both the service and the repository. The service depends on the repository interface, and the controller depends on the service interface.
* At least one business rule in the service that depends on existing data (a duplicate check or similar), reported to the controller as an outcome, not an HTTP code.
* DI registrations in `Program.cs`: repository as a singleton, service as scoped.

C. JSON file repositoryPage 3

* Data lives in `Data/&lt;resource&gt;.json` under the content root.
* Every public method that touches the file is guarded by one `SemaphoreSlim(1, 1)`, with `WaitAsync` above the `try` and `Release` in the `finally`.
* Writes go to a temp file first, then `File.Move(..., overwrite: true)`.
* A missing or empty file is treated as an empty list, not a crash.
* Ids are assigned by the repository, inside the guarded section.
* Every method is async and takes a `CancellationToken`.

D. Controller, validation, and errorsPage 4

* `[ApiController]` with a literal route (`[Route(&#34;api/recipes&#34;)]`) and `:int` constraints on id routes.
* GET all, GET by id, POST, PUT, DELETE, plus at least one query-string filter on GET all.
* Correct status codes: 200, 201 via `CreatedAtAction` (with a `Location` header), 204, 400, 404, and 409 for your business rule.
* Actions stay thin: call the service once, then map the result. No file access, no business rules.
* At least four validation rules on the request DTO using DataAnnotations, with your own error messages. Use explicit `[Required]` on strings.
* Validation error keys use JSON names (`SystemTextJsonValidationMetadataProvider`).
* `AddProblemDetails()` and `UseExceptionHandler()` in `Program.cs`.
* Don&#39;t wrap every action in `try/catch`. The exception handler covers unexpected errors in one place. Catching an exception and returning 200, or returning a 500 you built by hand, loses points.

E. Postman collectionPage 5

* Built from your API&#39;s OpenAPI document, with `{{baseUrl}}` and an id variable set from the POST response.
* At least one request for every endpoint, plus at least one failure request for every endpoint that can fail (400, 404, 409).
* Every request has at least one `pm.test` on the status code. POST also checks the `Location` header and that over-posting is ignored.
* The collection cleans up after itself and passes twice in a row in the Collection Runner.
* Exported to a `postman/` folder in your repository.

F. Git

* `Data/*.json` test data and `Data/*.tmp` are in `.gitignore`. If you want starting data, commit a separate seed file.
* Commits throughout the week with messages that say what changed. One giant commit on the due date loses points.

4What to Submit

1. Your GitHub repository link. Make sure I have access.
2. A video, 5 minutes or less, that shows these in order:
   1. Your endpoint table in the README.
   2. The folder structure in Visual Studio, and the DI registrations in `Program.cs`.
   3. Your full Postman collection running in the Collection Runner, all green.
   4. One POST in Postman, then `Data/&lt;resource&gt;.json` opened to show the new record, including a field the client couldn&#39;t set.
   5. One validation failure, with the error body on screen, and one 409 from your business rule.
   6. Thirty seconds on one thing that broke while you built this and how you found it.

Coming in Part 2 (Week 7)
You&#39;ll run SQL Server in a Docker container locally, write a second repository that implements the same interface against it, and add a setting in `appsettings.json` that switches between JSON and SQL Server storage without changing any other layer. You&#39;ll also add structured logging. Your Part 1 Postman collection should pass unchanged against the SQL Server version. That&#39;s the test that proves your layers are separated.
↗Resources

* [Tutorial: Create a controller-based web API with ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api)
* [Dependency injection in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
* [Model validation in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation)
* [Handle errors in ASP.NET Core APIs](https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors)
* [OpenAPI support in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview)
* [Postman: Write test scripts](https://learning.postman.com/docs/tests-and-scripts/write-scripts/test-scripts/)


--------
then this was due october 25th
[Skip to main content](https://alt-5b074caa51830.blackboard.com/bbcswebdav/pid-7045804-dt-content-rid-35717768_1/courses/126662038_26/READ_ONLY/content/_7045804_1/embedded/UNSAFE_HTML/UNSAFE_HTML_20261014162851.html?one_hash=1973E66476FA399300FC02CEF660A3C9&amp;f_hash=C61C359DE1C78A0DF94C99A91CC3C14E#main)
WEEK 8 · PROJECT 4SECURE API + MVVM MAUI CLIENT
Project 4: Secure API Integration with an MVVM MAUI Client
You take the semester API you built and give it the full Horse Racing API treatment: a switchable JSON/SQL data source, SQL Server in a Docker container, and JWT login that guards every write. Then you connect your MVVM MAUI app to it, and every write the app makes is stored in the database through the API. Due October 25, 2026, 11:59 PM.
1What You Are Building
You build this project on your own semester API domain: Finance, Recipe, Fitness/Exercise, or the domain you chose. You do not build a horse API. You copy the pattern from the class Horse Racing API and apply it to your resource.
Follow the class code exactly. The API repository is `horseracingapifall26`. The MAUI client repository is `horseracingappfall26`. When you are unsure how to do something, open the matching file in those two projects and compare it to yours.
The full stack, end to end: your MAUI MVVM app talks to your API over HTTP; the API validates a JWT on every write (POST, PUT, DELETE) and leaves the GETs public; the repository layer stores the data in a JSON file or in the SQL Server database running in your Docker container. The app interacts with the database only through the API — it never opens a SQL connection itself.
Nothing here is new material. Every part maps to something I taught:

* Data source switch, SQL Server in Docker, and logging: Week 6, pages 02, 03, 06, and 07.
* JWT authentication: the Week 7 JWT lesson and the Week 7 page &#34;Testing JWT Authentication With Postman&#34;.
* The MVVM MAUI client: the Week 8 page &#34;Calling the Horse API From MAUI&#34;.

Do not commit secrets. The SQL password, the signing key, and the demo user password never go into Git. Each of them lives in `.env` or User Secrets.
2Requirements
The finished project has two moving parts: a secure API with a switchable JSON/SQL backend, and an MVVM MAUI app that uses it. At a high level, you need:

* A switchable data source. One repository interface behind your controller, with a JSON implementation and an EF Core SQL Server implementation, picked by a setting in `appsettings.json`.
* SQL Server in Docker. A `compose.yaml` that runs the database in a container, with the password kept out of Git and the schema applied by an EF Core migration.
* JWT authentication. A login endpoint that issues tokens, and a write policy so every POST, PUT, and DELETE requires a valid token. GETs stay public.
* Logging. Startup lines that report the active data source and whether auth is on, plus a log entry for every create, update, and delete. Nothing sensitive gets logged.
* An MVVM MAUI client. A login page, list/detail/create/edit/delete screens, and the token handled in `SecureStorage` with a bearer header on every request. All database interaction goes through the API — the app never touches SQL Server itself.
* Proof it all works. A Postman collection that demonstrates the auth rules, and a README that walks a fresh clone from `docker compose up` to a running app, with no committed secrets.

That is the whole assignment. When you are unsure how a piece should work, open the matching file in `horseracingapifall26` or `horseracingappfall26` and match what the class did — the details live there, not on this page.
3What to Submit

1. Your GitHub repository link. Make sure I have access.
2. A video, 8 minutes or less, that shows these in order:
   1. The Postman Runner, all green, with authentication off. Then the Runner again, all green, with authentication on.
   2. Switching from `Json` to `SqlServer`, and the same data endpoints working. Show the container as healthy in `docker compose ps`.
   3. The startup log lines that show the data source and the authentication state.
   4. The MAUI app: browse without a login, then try a write and get sent to the login page.
   5. Log in, then create, edit, and delete a record — created in the app, saved through the API into the SQL Server container.
   6. One validation error shown in the app.
   7. Thirty seconds on one bug you hit and how you found it.

Build Order
Build in the same order as the class: JSON CRUD, then the SQL switch, then authentication in Postman, then logging, then the MVVM MAUI client. Test each layer alone before you add the next one. A MAUI bug is much harder to find when you are not sure the API works.
↳Resources
Class pages to reread:

* Week 6, pages 02, 03, 06, and 07.
* Week 7, &#34;Testing JWT Authentication With Postman&#34;, and the Week 7 JWT lesson.
* Week 8, &#34;Calling the Horse API From MAUI&#34;.

Microsoft Learn:

* [Secure storage (.NET MAUI)](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage)
* [Connect to local web services from Android emulators and iOS simulators](https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/local-web-services)
* [Configure JWT bearer authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication)
* [Safe storage of app secrets in development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
* [The Model-View-ViewModel (MVVM) pattern](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/mvvm)

----------</message>
    </thread>
  </project>
  <system-note>If one quick read gets you the answer, just answer. Otherwise, before any other tool, instead of a reaction, send one `reply` of at most 20 words saying your first step without restating the message, so the person is not left waiting.</system-note>
</wake>

# AI strategy
Use Haiku for simple tasks; Sonnet for main dev; Opus for complex arch or if Sonnet fails 2x.
Keep context low: Grep before Read, targeted reads only.

# Rules
Concise responses. No overengineering. No unrequested extras.
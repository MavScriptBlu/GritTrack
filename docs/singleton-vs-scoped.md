# Singleton vs Scoped (and why my SQL repo is Scoped)

Writing this down so future me stops re-googling it.

## What the words mean

- **Singleton**: the app makes ONE of the thing when it starts and everybody shares it forever.
- **Scoped**: the app makes a NEW one for every web request, then throws it away when the request is done.

Think of it like a group project. Singleton is one shared Google Doc the whole class edits. Scoped is everybody getting their own fresh copy each time they sit down.

## What I registered (Program.cs)

| Repo | Lifetime | Why |
| --- | --- | --- |
| `JsonCourseRepository` | Singleton | One file, so it needs one lock that everybody shares |
| `SqlCourseRepository` | Scoped | It uses `GritTrackDbContext`, which is Scoped |

## Why the JSON one is a Singleton

Every request reads and writes the same `courses.json`. The lock that stops two requests from writing at the same time lives inside the repo. If each request got its own repo, each one gets its own lock, and then the locks don't do anything. So: one repo, one lock.

## Why the SQL one can't be a Singleton

The DbContext (the thing that actually talks to SQL Server) is Scoped, and it's built to be. It isn't safe to share between two requests happening at the same time, and it's meant to be short lived.

If I make the SQL repo a Singleton, it grabs one DbContext at startup and holds onto it forever. That's called a captive dependency. In Development, .NET checks for this and just refuses to start with a scope validation error. If it somehow got past that, I'd get stale data and weird crashes when two requests hit it together.

## Why the rubric wants Singleton

My guess is Singleton is the simple default. It's the easiest thing to explain and it works fine for the JSON repo, since that one is a Singleton for the file lock reason above. I think the rubric is picturing that same setup for every repo.

## Why I went Scoped anyway

Scoped is the only one that works with the DbContext without extra stuff. The way to force a Singleton SQL repo is to inject an `IDbContextFactory` and make a brand new context on every single call. That's a lot more code and it buys me nothing here.

So: the JSON repo is a Singleton, the SQL repo is Scoped, and the comment in `Program.cs` points here.

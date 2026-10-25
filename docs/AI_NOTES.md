# AI notes

Running log of AI output that was rejected or had to be fixed. Feeds the README's AI usage note.

- **Template seeding with `HasData` (Phase 1).** Claude Code first seeded the game templates with EF Core `HasData`. The generated migration looked fine, but the app crashed on startup with `PendingModelChangesWarning`: EF compares the seeded `Formats` lists by reference, so every build reported a model change. Caught by actually running `docker compose up` rather than trusting a successful build. Fixed by switching to EF's `UseSeeding`/`UseAsyncSeeding` hooks, which insert missing templates at startup.

## Summary

<!-- What changed and why (1–3 bullets). Focus on the outcome, not the file list. -->

-

## Test plan

<!-- Checklist for the reviewer. Mark what you ran. -->

- [ ] `docker compose up --build` still starts cleanly (if Compose/Docker touched)
- [ ] `dotnet test` (if backend touched)
- [ ] `npm test` / `npm run build` (if frontend touched)
- [ ] Docs updated when behaviour or setup changed

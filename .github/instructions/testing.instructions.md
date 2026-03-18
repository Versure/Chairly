---
applyTo: "**/*.test.*,**/*.spec.*,**/Tests/**,**/*-e2e/**"
---

# Testing Conventions

## Backend — xUnit

- Unit tests for all handlers in `Chairly.Tests/Features/{Context}/{Entity}HandlerTests.cs`
- Use in-memory database: `new DbContextOptionsBuilder<ChairlyDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())`
- Test happy path, validation failures, and not-found cases
- Use `Validator.TryValidateObject()` for Data Annotation validation tests
- Append `.ConfigureAwait(false)` to all `await` expressions

### Test pattern

```csharp
[Fact]
public async Task HandlerName_Scenario_ExpectedResult()
{
    await using var db = CreateDbContext();
    var handler = new SomeHandler(db);
    var command = new SomeCommand { /* ... */ };

    var result = await handler.Handle(command);

    Assert.Equal(expected, result.Property);
}
```

## Frontend — Vitest (unit)

- Component specs in `{component-name}.component.spec.ts` alongside the component
- Use Angular testing utilities with Vitest
- Test signal inputs, store interactions, and template rendering

## Frontend — Playwright (e2e)

- E2E tests in `apps/chairly-e2e/src/`
- Write e2e tests for all pages/features
- Use `page.keyboard.press('Escape')` to close `showModal()` dialogs (cross-browser reliable)
- All test assertions against Dutch UI text

## Running Tests

Backend:
```bash
dotnet test src/backend/Chairly.slnx
```

Frontend:
```bash
cd src/frontend/chairly
npx nx affected -t test --base=main
npx nx affected -t e2e --base=main
```

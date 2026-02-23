We are going to set up the repository structure.

Before you create anything, walk me through the following points.
Ask one question per point and wait for my answer.

1. **Monorepo structure**: How do I want the top-level folders? Give a proposal
   and ask if I want changes.

2. **Nx workspace**: Which Nx preset do I want? (integrated vs package-based)
   Explain the difference in the context of this project.

3. **.NET solution structure**: Which projects do I want and how should they be
   named? Give a proposal based on Clean Architecture and ask if the naming
   is correct.

4. **.NET Aspire**: Which resources should be configured in the AppHost?
   (PostgreSQL, RabbitMQ, etc.)

5. **Angular app setup**: Which Angular version, which initial configuration?
   (SSR yes/no, routing strategy, styling approach)

6. **Linting & Formatting**: Which rules do I want for ESLint, Prettier,
   and .editorconfig? Give an opinionated proposal and ask if I agree.

7. **Git configuration**: .gitignore, branch strategy, conventional commits
   tooling (commitlint/husky)?

Ask these questions ONE BY ONE. Not all at once.
Wait after each question for my answer before continuing.

After all answers: summarize what you are going to do and ask for confirmation.
Only after "go" do you start creating.
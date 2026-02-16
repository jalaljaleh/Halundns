# Contributing to Halun DNS

Thanks for your interest in contributing. Please follow these guidelines to make collaboration easy.

## Getting the source
1. Fork the repository on GitHub.
2. Clone your fork: `git clone https://github.com/<your-user>/HalunDns.git`
3. Open the solution in Visual Studio (2017/2019/2022).

## Build
- This project targets .NET Framework 4.7.2. Make sure you have the corresponding developer pack installed.
- Build the `Halundns\Halundns.csproj` project in `Release` or `Debug` configuration.

## Development workflow
- Create a feature branch: `git checkout -b feature/my-feature`.
- Commit with clear messages: `git commit -m "Add: brief description"`.
- Push your branch and open a Pull Request against `master`.

## Code style
- Follow the existing project style (C# conventions used in the repository).
- Keep changes minimal and focused per PR.

## Tests
- There are no automated tests in the repository currently. Please include tests if you add logic that benefits from unit coverage.

## Releases
- Use the included script `scripts\create_release.ps1` to build and package a Release ZIP.

## Reporting security issues
- Do not open a public issue for security vulnerabilities. See `SECURITY.md`.

Thank you for helping improve Halun DNS.
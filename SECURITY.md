# Security Policy

## Supported versions

The `main` branch is the supported version for this portfolio example.

## Reporting a vulnerability

Please do not open a public issue for a suspected vulnerability. Contact the repository owner privately through GitHub with:

- a concise description and impact
- reproduction steps or a proof of concept
- affected endpoint, package, or configuration
- a suggested mitigation, if available

Never include real credentials, JWT signing keys, or production data in a report.

## Secure configuration

- Keep `.env` local and ignored.
- Use a managed secret store in deployment.
- Use a unique JWT signing key of at least 32 characters.
- Do not expose MongoDB or Redis publicly without authentication and network controls.
- Rotate credentials if they are ever committed or shared accidentally.

# Security Policy

## Supported versions

DRYL is in early development (`0.x`). Until `1.0`, only the **latest released
version** receives security fixes.

| Version | Supported |
| ------- | --------- |
| latest `0.x` | ✅ |
| older `0.x`  | ❌ |

## Reporting a vulnerability

**Please do not open a public issue for security vulnerabilities.**

Instead, report privately via GitHub's
[Security Advisories](https://github.com/Zimpi/DRYL.Components/security/advisories/new)
("Report a vulnerability"), or email **jan.zimprich@yahoo.de** with:

- a description of the issue and its impact,
- steps to reproduce (a minimal repro or PoC if possible),
- affected version(s).

You can expect an initial acknowledgement within **5 business days**. Once the
issue is confirmed and a fix is ready, we'll coordinate a release and credit you
(unless you prefer to remain anonymous).

## Scope notes

DRYL is a UI component library. Two areas are most security-relevant:

- **`DrylMarkdown`** renders Markdown via Markdig with **raw HTML disabled** to
  mitigate XSS. Reports of rendering paths that bypass this are in scope.
- Any component that emits unescaped user-supplied content into the DOM.

Denial-of-service via pathological inputs to a component is also in scope.

# Security Policy

Luna is built for minors. Security and safety are non-negotiable.

## Reporting a Vulnerability

**Do not open a public issue for security vulnerabilities.**

If you discover a security vulnerability, please email **klondono@stellara.ai** (TBD: set up actual email) with:
- Vulnerability description
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

We take all reports seriously and will:
1. Confirm receipt within 24 hours
2. Investigate and assess severity
3. Develop and test a fix
4. Release a patch and credit you (if desired)

## Security Best Practices

### For Contributors
- **Never log PII** (student names, emails, birthdates)
- **Sanitize all user input** before processing
- **Encrypt sensitive data** (tokens, preferences) at rest and in transit
- **Use HTTPS/WSS only** in production
- **Test with minors' data in mind** — assume hostile input

### For Deployments
- **Keep dependencies updated** — Dependabot alerts enabled
- **Use secrets management** — Environment variables, not hardcoded
- **Enable audit logging** — All session events must be recorded
- **Restrict access** — Parent/teacher/admin roles enforced
- **Monitor for anomalies** — Unusual activity flagged

## Scope

This security policy applies to:
- ✅ Luna codebase and infrastructure
- ✅ Student data and session recordings
- ❌ Third-party services (OpenAI, Azure, etc.) — report to their respective security teams

## Incident Response

If a vulnerability is exploited:
1. **Isolate affected sessions** — Kill-switch capability
2. **Notify parents** — Transparent communication
3. **Audit logs** — Full replay available for investigation
4. **Root cause analysis** — Prevent recurrence
5. **Public disclosure** — After patch is deployed

---

*Luna prioritizes learning effectiveness AND safety. Security is not optional.*

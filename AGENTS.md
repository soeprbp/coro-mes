# CoroMES deployment guidance

Use native Codex agents for delegated work. Do not send code or tasks to external
AI workers. Prefer the existing Blazor host for user-facing application work.

`src/CoroMES.Web` is the deployable Blazor application. The isolated Linux profile
is `infra/docker/blazor-staging.compose.yml`; the root Compose files are older
development examples and are not the deployed stack. Consult the deployment
guide before changing networking, storage, authentication, or feature flags.

Never contact live production databases, OT systems, or partner services without
explicit approval for the endpoint and purpose in the current conversation.
Tests must use local temporary data and mocked integrations. A successful health
check does not prove live data freshness or external integration readiness.

The repository is public. Keep secrets, database files, private host inventories,
and company snapshot data out of Git and container build contexts. Preserve the
prototype volume when testing migrations; seed a separate volume from a verified
backup and validate API reads. Keep Markdown operations documentation current
and explain important configuration, side effects, failures, and recovery.

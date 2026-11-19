# Publishing

The initial version is 1.0.0.

## Credentials

In this repository's Settings > Secrets and variables > Actions, add these repository secrets:

- UPM_PUBLISH_TOKEN: a publish token accepted by the existing registry.
- UPM_SERVICE_ACCOUNT_KEY_ID: Unity Cloud service account key ID.
- UPM_SERVICE_ACCOUNT_KEY_SECRET: the matching key secret.
- UPM_ORG_ID: the Unity organization ID.

The Unity service account needs the Package Manager Package Signer role. Enter credentials directly in the GitHub settings UI; do not put them in source files or chat.

The repository variable UPM_PUBLISH_REGISTRY is https://upm-registry.piotr-zag-ra.workers.dev/. Consumers install from https://upm.zagora.dev.

The registry Worker accepts tokens configured in its PUBLISH_TOKEN secret. In Cloudflare, go to Workers & Pages > upm-registry > Settings > Variables and Secrets. Use type Secret and name PUBLISH_TOKEN, then deploy the change. Existing valid tokens need no Cloudflare change. The Worker supports a comma-separated list; when adding a token, retain the existing tokens used by other packages.

## Registry configuration

Unity-PackageRegistry/wrangler.toml must include com.zagoradev.unity.proceduralui in PUBLIC_PACKAGES for unauthenticated installation. Its deployment workflow applies committed configuration changes on main. A local configuration edit alone does not change the running registry.

## Release

Update package.json and the matching CHANGELOG.md section, using a heading such as `## [1.0.0]`. Push the release commit to main after the credentials and registry access are ready. The Release workflow tags a new version and invokes Publish to sign the UPM tarball, publish it, and attach it to a GitHub release.

Published registry versions are immutable. For a failed initial publish, fix the cause and rerun the failed workflow; do not bump the version solely to retry an unpublished release.

After the first release, verify installation through Package Manager in ReceiptOCR before removing its embedded development copy. Keep the existing asset GUIDs when replacing that copy so migrated prefabs retain their references.

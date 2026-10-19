# Code signing policy

Status: unsigned, local preparation only. No SignPath application has been submitted or approved. Do not describe current builds as signed or sponsored by SignPath.

Before applying, the maintainer must confirm the repository owner and list named committers, reviewers and signing approvers here. All members must enable MFA for repository and signing access. Each signing request requires manual approval. Sign only binaries built from the reviewed source of this project; do not sign vendor drivers or DLLs.

The included CI workflow only builds, tests and stores unsigned artifacts. It has no signing secrets, upload to a signing service, release publication or repository-write permissions. SignPath integration must be configured after acceptance using the assigned organization, project, artifact and signing-policy identifiers.

Application prerequisites still pending: public repository under maintainer control, documented public unsigned release, verifiable project history/reputation, named roles, MFA, verified CI results, and acceptance by SignPath Foundation. Local preparation alone does not guarantee acceptance.

Only after approval, add the attribution required by the provider: “Free code signing provided by SignPath.io, certificate by SignPath Foundation”, with links to https://about.signpath.io and https://signpath.org. This statement is a future publishing requirement, not a claim of current sponsorship.

Official conditions: https://signpath.org/terms

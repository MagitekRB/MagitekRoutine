# Developer Guide

## Releases

Every commit or PR merge to `master` automatically creates a release:
- Auto-increments patch version (v1.0.5 → v1.0.6)
- Builds the solution
- Creates GitHub release with tag and artifacts

No manual tagging needed.

## Pull Requests

Every PR automatically runs a test build. The test must pass before merging.

## Regional Builds (CN/TC)

Magitek supports regional builds for CN (China) and TC (Taiwan/China) regions. These regions may be pinned to older versions for FFXIV compatibility.

### Regional Manifest

The `regional-manifest.json` file in the repo root defines which git tags to checkout when building CN/TC versions:

```json
{
  "ffxiv_versions": {
    "7.0": "v1.0.43",
    "7.1": "v1.0.74",
    "7.2": "v1.0.113",
    "7.3": "v1.0.171"
  },
    "cn": {
        "tag": "master",
        "version": "latest"
    },
    "tc": {
        "tag": "v1.0.43",
        "version": "v1.0.43"
    }
}
```

**Fields:**
- `ffxiv_versions`: Mapping of FFXIV patch versions to Magitek git tags. Use this reference table to find the correct tag when updating regional versions (e.g., if TC updates to 7.3, look up "7.3" to get "v1.0.171").
- `tag`: Git tag or branch name to checkout when building the regional DLL. Use `"master"` when the region should get latest updates (same as global).
- `version`: Version string to write to `Version-CN.txt` or `Version-TC.txt` (format: `CN-v1.0.56` or `TC-v1.0.67`). Use `"latest"` when tag is `"master"` - the workflow will use the current release version.

**Updating the manifest:**
- When CN/TC regions need to be updated to a new pinned version:
  1. Look up the FFXIV patch version in the `ffxiv_versions` mapping (e.g., "7.3" → "v1.0.171")
  2. Update the `tag` and `version` fields in the `cn` or `tc` section with the corresponding tag
  3. The workflow will automatically build from the specified tag on the next release
- When a region should get latest updates (same as global):
  - Set `"tag": "master"` and `"version": "latest"`
  - The workflow will build from master branch and use the current release version for the version file
- Example: If TC updates to FFXIV 7.3, look up "7.3" in `ffxiv_versions` to get "v1.0.171", then set `"tag": "v1.0.171"` and `"version": "v1.0.171"` in the `tc` section
- Example: If CN is on latest FFXIV version, set `"tag": "master"` and `"version": "latest"` in the `cn` section

**Note:** This manifest is only used by the GitHub Actions workflow. Loaders never read it - they download `Version-CN.txt`/`Version-TC.txt` from releases.

### Hotfixing CN/TC Versions

If a critical hotfix is needed for a pinned CN/TC version:

1. Checkout the pinned tag: `git checkout v1.0.56` (or the relevant tag)
2. Create a hotfix branch: `git checkout -b cn-hotfix-v1.0.56` (or `tc-hotfix-v1.0.67`)
3. Make the necessary fixes and commit
4. Update `regional-manifest.json` on `master` to point to the branch instead of tag:
   ```json
   "cn": {
     "tag": "cn-hotfix-v1.0.56",  // branch name instead of tag
     "version": "v1.0.56"
   }
   ```
5. The workflow will build from the branch on the next release

**Merging back:**
- Option 1: Merge hotfix branch into `master`, then create a new tag and update manifest to point to the tag
- Option 2: Keep the branch open if ongoing maintenance is needed (manifest continues pointing to branch)

**Note:** Hotfixes are expected to be rare. In most cases, CN/TC regions will upgrade when they upgrade FFXIV to match the global version, rather than receiving hotfixes for pinned versions. 
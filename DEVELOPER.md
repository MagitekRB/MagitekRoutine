# Developer Guide

## Releases

Every commit or PR merge to `master` automatically creates a release:
- Auto-increments patch version (v1.0.5 → v1.0.6)
- Builds the solution
- Creates GitHub release with tag and artifacts

No manual tagging needed.

## Pull Requests

Every PR automatically runs a test build. The test must pass before merging. 
**Copyright notice**
This template is based on https://github.com/dotnet/runtime/blob/main/CONTRIBUTING.md

# Contribution to `unfoldedcircle-oppo`

You can contribute to `unfoldedcircle-oppo` with issues and PRs. Simply filing issues for problems you encounter is a great way to contribute. Contributing implementations is greatly appreciated.

## Contribution "Bar"

Project maintainers will merge changes that improve the product.

Maintainers will not merge changes that are purely style changes or make the code harder to read and/or understand. We may revert changes if they are found to be breaking.

Contributions must also satisfy the other published guidelines defined in this document.

## DOs and DON'Ts

Please do:

- **DO** follow our code styling as defined in `.editorconfig`.
- **DO** give priority to the current style of the project or file you're changing even if it diverges from the general guidelines.
- **DO** keep the discussions focused. When a new or related topic comes up
it's often better to create new issue than to side track the discussion.

Please do not:

- **DON'T** make PRs for style changes.
- **DON'T** surprise us with big pull requests. Instead, file an issue and start
  a discussion so we can agree on a direction before you invest a large amount
  of time.
- **DON'T** commit code that you didn't write. If you find code that you think is a good fit to add to `unfoldedcircle-oppo`, file an issue and start a discussion before proceeding.
- **DON'T** submit PRs that alter licensing related files or headers. If you believe there's a problem with them, file an issue and we'll be happy to discuss it.
- **DON'T** add new features without filing an issue and discussing with us first.

## Breaking Changes

Breaking changes must be clearly outlined in the PR with justification of why it's needed.

## Suggested Workflow

We use and recommend the following workflow:

1. Create an issue for your work.
    - You can skip this step for trivial changes.
    - Reuse an existing issue on the topic, if there is one.
    - Get agreement from the team and the community that your proposed change is a good one.
    - Clearly state that you are going to take on implementing it, if that's the case. You can request that the issue be assigned to you. Note: The issue filer and the implementer don't have to be the same person.
2. Create a personal fork of the repository on GitHub (if you don't already have one).
3. In your fork, create a branch off of main (`git checkout -b mybranch`).
    - Name the branch so that it clearly communicates your intentions, such as githubhandle/bug-description, githubhandle/feat-issue_id or githubhandle/sec-issue_id-description.
    - Branches are useful since they isolate your changes from incoming changes from upstream. They also enable you to create multiple PRs from the same fork.
4. Make and commit your changes to your branch.
    - [Building from source code](README.md#Building-from-source-code) explains how to build.
    - Please follow our [Commit Messages](#commit-messages) guidance.
5. Add new tests corresponding to your change, if applicable.
6. Build the repository with your changes.
    - Make sure that the builds are clean.
    - Make sure that the tests are all passing, including your new tests.
7. Create a pull request (PR) against the `unfoldedcircle-oppo` repository's **main** branch.
    - State in the description what issue or improvement your change is addressing, type of change and linked issues (if applicable).
    - Check if all the Continuous Integration checks are passing.
8. Wait for feedback or approval of your changes from the owners.
9. When area owners have signed off, and all checks are green, your PR will be merged.
    - The next official build will automatically include your change.
    - You can delete the branch you used for making the change.

## Good first Issues

The team marks the most straightforward issues as `good first issue`. This set of issues is the place to start if you are interested in contributing but new to the codebase.

## Commit Messages

Please format commit messages as follows (based on [A Note About Git Commit Messages](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html)):

```
Summarize change in 50 characters or less

Provide more detail after the first line. Leave one blank line below the
summary and wrap all lines at 72 characters or less.

If the change fixes an issue, leave another blank line after the final
paragraph and indicate which issue is fixed in the specific format
below.

Fix #42
```

Also do your best to factor commits appropriately, not too large with unrelated things in the same commit, and not too small with the same small change applied N times in N different commits.

## PR - CI Process

The Continuous Integration (CI) system will automatically perform the required builds, tests and verifications (including the ones you are expected to run) for PRs. Builds and test runs must be clean.

If the CI build fails for any reason, you are expected to make adjustments to your changes in order for it to work, or ask a community member for assistance.

## PR Feedback

Team and community members will provide feedback on your change. Community feedback is highly valued. You will often see the absence of team feedback if the community has already provided good review feedback.

One or more team members will review every PR prior to merge.

There are lots of thoughts and [approaches](https://github.com/antlr/antlr4-cpp/blob/master/CONTRIBUTING.md#emoji) for how to efficiently discuss changes. It is best to be clear and explicit with your feedback. Please be patient with people who might not understand the finer details about your approach to feedback.
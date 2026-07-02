# ClipKeep Privacy Policy

**Effective date:** July 2, 2026
**Publisher:** Nixon Software Solutions

ClipKeep ("the app") is a local-first Windows clipboard manager. This policy
explains what data the app handles and how. In short: **your data never leaves
your device, and Nixon Software Solutions never receives, stores, or has access
to any of it.**

## The short version

- ClipKeep runs entirely **on your device**. It has no servers and makes no
  network connections.
- We (the developer) **collect nothing**. There are **no accounts, no cloud
  sync, no analytics, and no telemetry**.
- Everything the app stores stays in a local database on your own PC, under your
  control, and can be deleted by you at any time.

## What the app stores locally

To do its job — keeping a searchable history of what you copy — ClipKeep saves
the following **on your computer only**:

- **Clipboard content you copy**: text, links, code, and images.
- **Basic context for each item**: the date/time copied, how many times it was
  copied, and the name of the application and window it was copied from.
- **Your settings**: preferences, favorites, excluded applications, and
  retention choices.

This data is stored in a local SQLite database and image files under your
Windows user profile (`%AppData%\ClipForge`). It is accessible only to your
Windows user account, subject to your operating system's protections.

## What the app does NOT do

- It does **not** transmit your clipboard, settings, or any other data over the
  internet or to the developer or any third party.
- It does **not** use accounts, sign-in, cloud storage, or backup services.
- It does **not** include advertising, analytics, tracking, or telemetry SDKs.
- It does **not** sell or share personal information — because it never collects
  any.

## Sensitive content

ClipKeep tries to help you avoid capturing secrets:

- **Sensitive-content filtering** (on by default) prevents items that look like
  payment card numbers, IBANs, API keys, tokens, and private keys from being
  saved to history.
- An **application exclusion list** (with common password managers included by
  default) stops the app from capturing clipboard content while those apps are
  in the foreground.
- You can **pause** capture at any time.

These features reduce the chance of storing sensitive data, but no automatic
filter is perfect. Because clipboard content can contain personal or sensitive
information, you remain in control: review, delete, or clear items as needed.

## Your control over your data

- **Delete** any item, or clear items, from within the app.
- **Retention cleanup** automatically removes older, non-favorited items on the
  schedule you choose (favorites are never auto-deleted).
- **Uninstalling** the app and deleting its data folder (`%AppData%\ClipForge`)
  removes the stored history from your device.

## Permissions

ClipKeep is a packaged desktop application and runs with standard desktop
permissions (`runFullTrust`). It uses these solely to read the Windows clipboard
and to provide a global paste shortcut. It requests no location, camera,
microphone, contacts, or network capabilities.

## Children

ClipKeep is a general-purpose utility and is not directed at children. It does
not knowingly collect any personal information from anyone, including children.

## Changes to this policy

If this policy changes, the updated version will be published at the same
location with a new effective date.

## Contact

Questions about this policy or the app:

- Website: https://nixonsolutions.org/
- Email: privacy@nixonsolutions.org

*(Nixon Software Solutions — please confirm the contact email above is monitored,
or replace it with your preferred address before publishing.)*

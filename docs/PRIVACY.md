# Privacy

The application contains no telemetry, account login or network-based update service. It does not send hardware readings or profile data to a remote service. Profiles are stored locally in `%LOCALAPPDATA%\G870ControlCenterWindows`.

Windows startup registration invokes the local Windows Task Scheduler. MUX switching reads local BitLocker status but does not upload it or change encryption settings. Crash reports or diagnostic files are only shared if the user chooses to share them.

The operating system may independently perform certificate validation and security reputation checks. The application does not disable those checks.

# Validation UX

Forms show you WHERE and WHAT went wrong, big enough to read.

## Pieces

- `Controls/ValidatedField.xaml`: label + text box + error line. Give it `LabelText`, `Text` (two way), `ErrorText`. Empty `ErrorText` = normal. Any text = thick blush pink outline, a pink tint, and the message right under the box. While focused with no error it gets a thick accent outline.
- `Controls/ErrorBanner.xaml`: the big error card (22pt title, 16pt message). Optional button when `ActionText` and `ActionCommand` are set (Try Again).

## Where they're used

- `AddCoursePage`: name, code, instructor, credits each have their own error property (`NameError`, `CourseCodeError`, `InstructorError`, `CreditsError`). `Validate()` checks all of them at once (same rules as the API: name and code required, max lengths, credits 0 to 6). The banner sits right above Save and says "N things to fix" or the server's message. Try Again only shows when the server couldn't be reached.
- `LoginPage`: `UserNameError` / `PasswordError` for empty boxes. Server problems (wrong password, API down) use the banner above Sign In.
- `CourseListPage`: starting GPA and credits get their own errors (`StartingGpaError`, `StartingCreditsError`). Load and remove failures use the banner at the top.

## Rules

- Typing in a box clears that box's error.
- Screen readers hear the label plus the error.

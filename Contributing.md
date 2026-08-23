# Contributing

This file lists the contributing guidelines that are used in the project.

### Commit style guide

Commits start with a capital letter and don't end in a punctuation mark.

Right:
```
Treat usernames as case-insensitive in user collections
```

Wrong:
```
treat usernames as case-insensitive in user collections.
```

Use imperative present tense in commit messages instead of past tense.

Right:
```
Add null-check for GameMode
```

Wrong:
```
Added null-check for GameMode
```

### Pull Requests

Make sure that the scope of your pull request is well defined. Pull requests can take significant developer time to review and very large pull requests or pull requests with poorly defined scope can be difficult to review.

One pull request should _only implement one feature_ or _fix one bug_, unless there is a good reason for grouping the changes together.

Do not heavily refactor the style of existing code in a pull request, unless the refactored code fits to the scope of the pull request (feature or bug fix). Rather, if you want to refactor existing code just for the sake of refactoring or getting rid of technical debt, create a secondary pull request for that purpose.

If you have introduced a new DLL dependency, check [README for Build Scripts](./Scripts/README.md) to determine whether you need to update the common assembly list and how to do that.

**Make sure your code and commits match this style guide before you create your pull request.**

Pull requests that are not well defined in their scope or pull requests that don't match the style guide can end up rejected and closed by the staff.

### Code style guide

We have established a couple of code style rules to keep things consistent. Please check your code style before committing the code.
- We use spaces instead of tabs to indent code.
- Curly braces are always to be placed on a new line. One of the reasons for this is to clearly separate the end of the code block head and body in case of multiline bodies:
```cs
if (SomeReallyLongCondition() ||
    ThatSplitsIntoMultipleLines())
{
    DoSomethingHere();
    DoSomethingMore();
}
```
- Braceless code block bodies should be made only when both code block head and body are single line. Statements that split into multiple lines and nested braceless blocks are not allowed within braceless blocks:
```cs
// OK
if (Something())
    DoSomething();

// OK
if (SomeReallyLongCondition() ||
    ThatSplitsIntoMultipleLines())
{
    DoSomething();
}

// OK
if (SomeCondition())
{
    if (SomeOtherCondition())
        DoSomething();
}

// OK
if (SomeCondition())
{
    return VeryLongExpression()
        || ThatSplitsIntoMultipleLines();
}
```
- Only empty curly brace blocks may be left on the same line for both opening and closing braces (if appropriate).
- If you use `if`-`else` you should either have all of the code blocks braced or braceless to keep things consistent.
- Code should have empty lines to make it easier to read. Use an empty line to split code into logical parts. It's mandatory to have empty lines to separate:
  - `return` statements (except when there is only one line of code except that statement);
  - local variable assignments that are used in the further code (you shouldn't put an empty line after one-line local variable assignments that are used only in the following code block though);
  - code blocks (braceless or not) or anything using code blocks (function or hook definitions, classes, namespaces etc.)
```cs
// OK
int localVar = Something();
if (SomeConditionUsing(localVar))
    ...

// OK
int localVar = Something();
int anotherLocalVar = OtherSomething();

if (SomeConditionUsing(localVar, anotherLocalVar))
    ...

// OK
int localVar = Something();

if (SomeConditionUsing(localVar))
    ...

if (SomeOtherConditionUsing(localVar))
    ...

localVar = OtherSomething();

// OK
if (SomeCondition())
{
    Code();
    OtherCode();

    return;
}

// OK
if (SomeCondition())
{
    SmallCode();
    return;
}
```
- Use `var` with local variables when the type of the variable is obvious from the code or the type is not relevant. Never use `var` with primitive types.
- A space must be put between braces of empty curly brace blocks.
```cs
// OK
var list = new List<int>();

// Not OK
var something = 6;
```
- Local variables, function/method args and private class fields are named in `camelCase` and a descriptive name, like `ircUser` for a local `IrcUser` variable.
- Classes, namespaces, and properties are always written in `PascalCase`.
- Class fields that can be set via INI tags should be named exactly like ini tags with dots replaced with underscores.

#### Formatter requirements

- If you have made medium or significant changes to a file (> 25%), you should run the code formatter on the whole file using Visual Studio.
- If you have only made minor changes to a file (≤ 25%), you should only format the lines that you have changed to keep the style of the file consistent.

- You should apply the removal and sorting of `using` directives to the whole file if one of the following is true, and you should not apply it otherwise:
    - You have reached the threshold for running the code formatter on the whole file, or
    - You have added and/or removed `using` directives, especially if you have added AND removed `using` directives.

#### C# nullability requirements

The project has mixed usages of nullability annotations. 

- When you are adding new `.cs` files, you must write `#nullable enable` at the top of the file and make sure that all code in that file is null-safe.
- When you are modifying existing `.cs` files, if you have made significant changes to the file (> 75%), you should write `#nullable enable` at the top of the file and make sure that all code in that file is null-safe. If you are only making minor or medium changes to an existing `.cs` file (≤ 75%) that does not start with `#nullable enable`, you should write code without nullability annotations to keep the style of the file consistent.

### Forbidden APIs

- You should not use `BitConverter`, because its behavior depends on platform endianness via `BitConverter.IsLittleEndian`. Instead, you should use `BinaryPrimitives` for byte conversions.

### Text encoding
- Before converting between byte arrays and strings, you should always think carefully about the encoding to be used.
    - For client-side text, you should use UTF-8 encoding without BOM, unless you have a good reason not to.
    - For game-related text, you should carefully examine the encoding used by the game and use that encoding for conversions, and you MUST also check if the encoding is the retrieved encoding or the system ANSI encoding (which varies by system locale), and use the correct one accordingly. Use ASCII encoding if you can't determine the encoding and the string seems to only contain ASCII characters.
        - Example: if you get a Windows-1252 encoding from the game, it might be either a constant usage of Windows-1252 encoding or the system ANSI encoding, so you should check by making sure the string contains at least one non-ASCII character, running the game in a virtual machine with a different system locale (e.g. Russian, Chinese, Polish) and observing whether the encoding changes.

### Literal strings
- This codebase contains a literal string localization system. Use `"literal string".L10N("key")` to mark literal strings for localization. This extension method requires `using ClientCore.Extensions;` to be in scope.

- You must make sure both the literal string and the key are compile-time constant and they must be consistent across all platforms. Use `/` for the path separator and `\n` for the line break in the literal string, instead of using `Environment.NewLine` or `Path.DirectorySeparatorChar`. The key must be in the format of `Namespace:SubNamespace:...:KeyName` and should be as descriptive as possible to make it easier for translators to understand the context. Below demonstrates some examples of bad usages violating the constant requirement:
    - Do not localize non-literal strings.
    ```cs
    // OK
    string greetingText = string.Format("Hello, {0}!".L10N("Client:Main:GreetingMessage"), userName);

    // Not OK
    string greetingText = string.Format("Hello, {0}!", userName).L10N("Client:Main:GreetingMessage");

    // Not OK
    string greetingText = $"Hello, {userName}!";

    // Not OK
    string greetingText = $"Hello, {userName}!".L10N("Client:Main:GreetingMessage");
    ```
    - Do not conditionally determine the key or the literal string.
    ```cs
    // OK
    bool isSuccess = DoSomething();
    string message = isSuccess
        ? "Operation succeeded.".L10N("Client:Main:OperationSucceededMessage")
        : "Operation failed.".L10N("Client:Main:OperationFailedMessage");

    // Not OK
    bool isSuccess = DoSomething();
    string message = (isSuccess ? "Operation succeeded." : "Operation failed.").L10N(isSuccess ? "Client:Main:OperationSucceededMessage" : "Client:Main:OperationFailedMessage");

    // OK
    int resultErrorCode = DoSomething();
    string errorMessage = resultErrorCode switch
    {
        0 => "Operation succeeded.".L10N("Client:Main:OperationSucceededMessage"),
        1 => "Operation failed.".L10N("Client:Main:OperationFailedMessage"),
        2 => "Operation failed due to file not found.".L10N("Client:Main:FileNotFoundMessage"),
        _ => "Operation failed due to unknown error.".L10N("Client:Main:UnknownErrorMessage")
    };

    // Not OK
    int resultErrorCode = DoSomething();
    string errorMessage = resultErrorCode switch
    {
        0 => "Operation succeeded.",
        1 => "Operation failed.",
        2 => "Operation failed due to file not found.",
        _ => "Operation failed due to unknown error."
    }.L10N($"Client:Main:ResultErrorCode{resultErrorCode}Message");
    ```

- Consider the timing when a static class member gets initialized. Use getters `=>` instead of fields `=` for static class members that are initialized with literal strings to make sure the localization system is properly initialized before the literal strings get localized.
```cs
// OK
class MyClass
{
    public static string GreetingMessage => "Hello, world!".L10N("Client:MyClass:GreetingMessage");
}

// Not OK
class MyClass
{
    public static string GreetingMessage = "Hello, world!".L10N("Client:MyClass:GreetingMessage");
}
```
- The literal string must not start or end with whitespace. Use `"literal string".L10N("key") + " "` if you need to add whitespace at the end of the literal string for formatting reasons.
```cs
// OK
string message = "An error occurred. Error:".L10N("Client:Main:ErrorMessage") + " " + errorDetails;

// Not OK
string message = "An error occurred. Error: ".L10N("Client:Main:ErrorMessage") + errorDetails;

// Not OK
string message = "An error occurred. Error:".L10N("Client:Main:ErrorMessage") + errorDetails; // This violates the English punctuation rules
```

Note: This guide is not exhaustive and may be adjusted in the future.

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

Note: The style guide is not exhaustive and may be adjusted in the future.

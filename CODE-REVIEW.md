# Code Review: CodeToReview.cs

Thanks for the submission! The intent of the code is clear — a small utility for creating
people with randomized names/ages and composing a married name. Before this can merge,
there is one compile blocker and a number of correctness bugs that change behavior in
non-obvious ways. I've grouped everything by severity, with line references and suggested
fixes, so it can be worked through top-down. Happy to pair on any of these.

**Suggested order of attack:** fix the blocker → fix the bugs (ideally adding a unit test
alongside each one) → then tackle the design and naming items as a follow-up refactor.

---

## 🚫 Blocker — won't compile

### 1. Typo in using directive (line 2)
`System.Collegctions.Generic` should be `System.Collections.Generic`. The file does not
compile as-is, so nothing below it has ever actually run.

---

## 🐛 Bugs — compiles but behaves incorrectly

### 2. `random.Next(0, 1)` can only return 0 — everyone is named "Bob" (line 44)
`Random.Next(min, max)` is **exclusive** of the upper bound, so `Next(0, 1)` always
returns 0 and the `else` branch ("Betty") is unreachable.

```csharp
// Before
if (random.Next(0, 1) == 0)

// After
if (random.Next(0, 2) == 0)
```

### 3. `new Random()` created inside the loop (line 43)
On .NET Framework, `Random` instances created in quick succession share the same
time-based seed and produce identical sequences — so every person in a batch gets the
same name and age. (.NET Core/5+ improved the default seeding, but creating an instance
per iteration is still wasteful.) Create one instance outside the loop, or use
`Random.Shared` on .NET 6+.

```csharp
var random = Random.Shared; // or: a single new Random() hoisted above the loop
for (int j = 0; j < count; j++) { ... }
```

### 4. Age calculated as `years * 356` days (line 51)
356 appears to be a typo for 365 — each simulated year is ~9 days short. Even with 365,
leap years are ignored, so ages drift by roughly a day per 4 years. Calendar arithmetic
should use calendar methods:

```csharp
// Before
DateTime.UtcNow.Subtract(new TimeSpan(random.Next(18, 85) * 356, 0, 0, 0))

// After
DateTimeOffset.UtcNow.AddYears(-random.Next(18, 86))
```

Note the second fix folded in above: `Next(18, 85)` excludes 85, so the maximum age was
84. If the intended range is 18–85 inclusive, the upper bound must be 86.

### 5. `GetBobs` age filter is inverted (line 64)
`x.DOB >= DateTime.Now.Subtract(...)` selects people born **after** the cutoff — i.e.
*younger* than 30 — the opposite of what `olderThan30` promises. The `30 * 356` day-math
bug from finding #4 appears here too.

```csharp
// Before
_people.Where(x => x.Name == "Bob" && x.DOB >= DateTime.Now.Subtract(new TimeSpan(30 * 356, 0, 0, 0)))

// After
var cutoff = DateTimeOffset.UtcNow.AddYears(-30);
_people.Where(x => x.Name == "Bob" && x.DOB <= cutoff)
```

### 6. Length check concatenates an int with a string (line 71)
`p.Name.Length + lastName` is `int + string`, which C# compiles as string concatenation:
for `p.Name = "Bob"`, `lastName = "Smith"` it produces `"3Smith"` and checks *its* length
(6). The check measures the wrong thing entirely.

```csharp
// Before
if ((p.Name.Length + lastName).Length > 255)

// After
if ((p.Name + " " + lastName).Length > 255)
```

### 7. `Substring` result discarded — truncation never happens (line 73)
Strings are immutable; `(p.Name + " " + lastName).Substring(0, 255);` computes a
truncated copy and throws it away, then line 76 returns the full, untruncated name.

```csharp
// Before
(p.Name + " " + lastName).Substring(0, 255);

// After
return (p.Name + " " + lastName).Substring(0, 255);
```

### 8. Catch block destroys the original exception (lines 53–57)
`e` is caught but never used — the real failure and its stack trace are lost, replaced by
a bare `Exception` with a vague message. Either pass the original as the inner exception,
or (given the comment says this "should never happen") remove the try/catch and let the
real exception surface.

```csharp
// If the catch stays:
throw new InvalidOperationException("Failed to create person.", e);
```

### 9. Static `Under16` is evaluated once and goes stale (lines 9, 12)
Three issues in one field:
- It's `static readonly`, so it's computed once at type load. In a long-running process
  the "under 16" cutoff drifts — a day later it's wrong by a day.
- It computes `AddYears(-15)`, so the name (`Under16`) doesn't match the value.
- It's used as the **default DOB** in the single-arg constructor, meaning every
  default-constructed person is silently ~15 years old — a surprising implicit default.

Additionally, `Under16.Date` returns a `DateTime` with `Kind.Unspecified`; assigning it
to the `DateTimeOffset DOB` property implicitly converts using the **local** timezone
offset, even though the value originated from `UtcNow`. Compute the cutoff on demand and
stay in `DateTimeOffset` end-to-end.

### 10. Magic `"test"` check in production logic (lines 69–70)
`lastName.Contains("test")` silently returns the first name only. This looks like
test-environment plumbing leaked into production code: it's case-sensitive (misses
`"Test"`), and any real surname containing "test" (e.g. "Testerman") silently skips the
marriage logic. Test behavior belongs in tests — remove this branch.

### 11. `GetPeople` accumulates state across calls and is misnamed (lines 35–59)
Each call appends to the shared `_people` field and returns the whole list — call it
twice with 5 and you get 10. The name says *get* but the method *creates*. Either return
a fresh list, or rename to `CreatePeople` and make the accumulation an explicit,
documented behavior. As written, callers get surprising cumulative results.

---

## 🏗️ Design & robustness

### 12. `People` should be `Person`
The class models a single person; `List<People>` reads as "list of lists of people".
Singular class names are the .NET convention.

### 13. `BirthingUnit` — naming and mixed responsibilities
The name is odd for what it does, and the class mixes three jobs: creating people,
querying them, and marriage name logic. Consider splitting (e.g. a factory/repository and
a naming service), or at minimum renaming to something like `PersonService`.

### 14. `GetBobs` is private and never called (line 62)
Dead code — nothing can reach it. Delete it, or make it part of the public surface
intentionally (with tests).

### 15. `GetMarried` validates nothing and doesn't do what it says (lines 67–77)
- `p` and `lastName` are unchecked: `lastName.Contains(...)` throws
  `NullReferenceException`-style failures on null input. Add guard clauses
  (`ArgumentNullException.ThrowIfNull(p)`, `ArgumentException.ThrowIfNullOrEmpty(lastName)`).
- The method *returns a string* but never updates the person — it can't, since `Name`
  has a private setter. The name promises a state change that doesn't happen; either
  rename (e.g. `GetMarriedName`) or make it actually update the person.

### 16. Inconsistent time handling
`DateTime.UtcNow` (line 51) and `DateTime.Now` (line 64) are mixed, and `DateTime` values
are implicitly converted to `DateTimeOffset` (picking up the local offset). Standardize
on `DateTimeOffset.UtcNow` throughout to avoid timezone-dependent behavior.

### 17. Magic numbers throughout
`255`, `18`, `85`, `30`, `356` carry business meaning but no names. Extract constants
(`MaxNameLength`, `MinAdultAge`, …) so intent is visible and the `356` typo (finding #4)
could never have hidden.

---

## 🎨 Style & documentation

### 18. XML docs are wrong everywhere
- Line 21: `_people` is documented as "MaxItemsToRetrieve".
- Lines 30–34: documents a param `j` (the method takes `i`) and claims `List<object>`
  is returned. Wrong docs are worse than no docs — they get trusted.

### 19. Parameter `i`, loop variable `j` (lines 35–37)
The conventional roles are swapped, which makes the loop genuinely hard to read. Name the
parameter for what it means: `count`.

### 20. Typos and formatting
"dandon" (line 41, presumably "random"); inconsistent indentation and brace placement in
the `People` class (lines 9–16, including the `}}` on line 16).

---

## Summary

| Severity | Count |
|---|---|
| Blocker | 1 |
| Bug | 10 |
| Design & robustness | 6 |
| Style & docs | 3 |

The highest-value next step after the compile fix is a small unit-test suite around
`GetPeople`, `GetBobs`, and `GetMarried` — most of the bugs above (always-Bob, inverted
filter, broken truncation, cumulative results) would have been caught by the first round
of tests.

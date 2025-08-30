# Jinja2.NET

**Jinja2.NET** is a native .NET implementation of the popular [Jinja2](https://jinja.palletsprojects.com/) Python templating language. It enables powerful, expressive, and maintainable text rendering directly within .NET applications — without relying on embedded Python runtimes or limited Liquid-like engines.

## Why Jinja2 in .NET?

While existing .NET templating libraries like [Scriban](https://github.com/scriban/scriban) and [Fluid](https://github.com/sebastienros/fluid) are performant and well-suited for many use cases, they do not offer full Jinja2 compatibility. For developers working with AI models (e.g. [GGUF](https://ggml.ai/format/gguf/) metadata), machine learning pipelines, or shared infrastructure that depends on Jinja-based prompt templates, this limitation can be a major blocker.

**Jinja2.NET** was created to fill this gap — to allow seamless rendering of Jinja templates in C#, for both general templating and AI-related workflows like:

* Rendering chat prompts for LLaMA and GGUF models (`chat_template`)
* Generating config files, SQL, or dynamic documentation
* Rendering infrastructure-as-code or CI/CD pipelines using templates

---

## Features

*  Full Jinja2 syntax: Includes `{{ ... }}`, `{% ... %}`, `{# ... #}` blocks
*  Attribute and dictionary access: Support for nested data
*  Chained filters: Compatible with built-in filters like `upper`, `join`, `trim`, etc.
*  Loops and conditionals: `for`, `if`, `elif`, `else`, `set`, `break`, `continue`
*  Extensibility: Add custom filters or functions
*  Safe execution: Sandboxed evaluation model
*  No Python dependency: Written in native C# (WIP)
*  Spaces, tabs, and newlines outside `{{ ... }}` and `{% ... %}` are preserved by default, matching Jinja2.
*  Jinja2-style whitespace control (`{%- ... -%}` and `{{- ... -}}`) trims whitespace only outside the tag, not inside.
---

## Installation

You can install via NuGet (coming soon):

```bash
dotnet add package Jinja2.NET
```
---

## Basic Syntax Overview

### Variable Substitution

```jinja
Hello {{ name }}!
```

### Attribute and Dictionary Access

```jinja
User: {{ user.name }} ({{ user['email'] }})
```

### Filters

```jinja
{{ message | upper }}
{{ text | replace(" ", "_") | lower }}
```

### Control Flow

```jinja
{% if is_admin %}
  Welcome, admin.
{% else %}
  Access denied.
{% endif %}
```

### Loops

```jinja
{% for item in items %}
  - {{ item }}
{% endfor %}
```

### Variable Assignment

```jinja
{% set message = "Hello " + name %}
{{ message }}
```

---

## Example Usage

### Input Template

```jinja
Hello {{ name }}! You are {{ age }} years old.
```

### C# Usage

```csharp
var template = new Template("Hello {{ name }}! You are {{ age }} years old.");
var context = new Dictionary<string, object> {
    ["name"] = "Alice",
    ["age"] = 30
};

string result = template.Render(context);
// Output: Hello Alice! You are 30 years old.
```

---

## Jinja2.NET Grammar & Syntax Reference

Jinja2.NET implements a large subset of the Jinja2 template language. Below is a summary of the supported grammar and syntax:

### Expressions

- **Variable substitution:**  
  `{{ variable }}`  
  `{{ user.name }}`  
  `{{ user['email'] }}`

- **Filters:**  
  `{{ value | filter1 | filter2(arg) }}`

- **Arithmetic and logic:**  
  `{{ a + b }}`  
  `{{ count > 1 }}`  
  `{{ not is_admin }}`

- **Literals:**  
  Strings: `"text"` or `'text'`  
  Numbers: `123`, `3.14`  
  Lists: `[1, 2, 3]`  
  Dictionary literals: not yet (roadmap)

### Statements

- **Comments:**  
  `{# this is a comment #}`

- **Control flow:**  
```
{% if condition %}
  ...
{% elif other_condition %}
  ...
{% else %}
  ...
{% endif %}
```

- **Loops:**  
```
{% for item in items %}
  {{ loop.index0 }}:{{ item }}
{% endfor %}
```
- `loop` variables: `index0`, `index`, `first`, `last`

- **Variable assignment:**  
  `{% set var = expression %}`

- **Raw blocks:**  
```
{% raw %}
  {{ not evaluated }}
{% endraw %}
```

### Whitespace Control

- Spaces, tabs, and newlines outside tags are preserved by default.
- Jinja2-style whitespace trimming is supported:  
  `{%- ... -%}` and `{{- ... -}}` will trim whitespace to the left/right of the tag, but only outside the tag.
- Whitespace inside tags or blocks is always preserved.
- **Example:**  
  Template:
```
Hello    {%- if true -%}   World   {%- endif -%}    !
```
Output:
```
Hello   World   !
```
(Spaces inside the block are preserved, only spaces outside the tags are trimmed.)

### Operator Support

- Arithmetic: `+`, `-`
- Comparison: `==`, `!=`, `<`, `>`, `<=`, `>=`
- Logical: `and`, `or`, `not`

### Data Access

- Attribute: `user.name`
- Dictionary indexing: `user['email']`
- List/array indexing: `items[0]`

### Filters

- Built-in:
  - `capitalize`
  - `default`
  - `first`
  - `join`
  - `last`
  - `length`
  - `lower`
  - `replace`
  - `reverse`
  - `sort`
  - `title`
  - `trim`
  - `upper`
- Custom: Register your own filters in C#
- Chained filters: `{{ value | filter1 | filter2(arg) }}`

### Registering Custom Filters

You can add custom filters by calling the `RegisterFilter` method on your `Template` instance before rendering.  
A filter is a function that takes an input value and an array of arguments, and returns the transformed value.

#### Example

```csharp
var template = new Template("{{ 'hello' | shout }}");
template.RegisterFilter("shout", (input, args) => input?.ToString().ToUpper() + "!");
var result = template.Render(new Dictionary<string, object>());
// result: "HELLO!"
```

- The first argument (`input`) is the value before the pipe (`|`).
- The second argument (`args`) is an array of any arguments passed to the filter.

#### Example with arguments

```csharp
var template = new Template("{{ 'foo' | surround('!') }}");
template.RegisterFilter("surround", (input, args) =>
{
    var s = args.Length > 0 ? args[0]?.ToString() ?? "" : "";
    return s + input?.ToString() + s;
});
var result = template.Render(new Dictionary<string, object>());
// result: "!foo!"
```


**Summary:**  
- Use `template.RegisterFilter("name", (input, args) => ...)` to add a filter.
- The filter function receives the input value and an array of arguments.

---

**Note:**  
- Advanced features like macros, includes, blocks, and template inheritance are on the roadmap.
- For full compatibility details, see the [tests](./Jinja2.NET.Tests/TemplateTests.cs) or open an issue.

---

## Use Cases

*  **AI Prompt Templating** (e.g., LLaMA `chat_template` rendering)
*  **Configuration generation**
*  **Template-driven document generation**
*  **Infrastructure as Code (IaC)** templating
*  **Test data or code generation**
*  **Web UI rendering with strict logic control**

---

## Extending with Custom Filters

You can define your own filters to enrich your template language:

```csharp
template.RegisterFilter("shout", (input, args) => input.ToString().ToUpper() + "!");
```

```jinja
{{ "hello" | shout }}  =>  HELLO!
```

---
### Deep Dive into Jinja2 Loop Scope Management

Jinja2's scoping rules for `for` loops and `set` blocks are strict and well-defined (e.g., Jinja2 v3.1.x). Here's how loops manage scopes:

1. **Global Scope**:
   - Variables set at the top level (e.g., `{% set x = 1 %}`) are stored in the global scope and accessible throughout the template unless shadowed.
   - Example: `{% set x = 1 %}{{ x }}` → Outputs `1`.

2. **For Loop Scope**:
   - Each `for` loop creates a **new scope** for its iterations.
   - Variables set inside a loop (e.g., `{% set x = i %}` in `{% for i in ... %}`) are local to that loop's scope and do not affect the outer scope (global or parent loop).
   - The loop variable (e.g., `i` in `{% for i in ... %}`) and the `loop` object (e.g., `loop.index`) are local to the loop scope.
   - Example:
     ```jinja
     {% set x = 1 %}
     {% for i in [1, 2] %}
       {% set x = i %}
       {{ x }}
     {% endfor %}
     {{ x }}
     ```
     Output: `121` (loop sets `x = 1`, `x = 2` locally; global `x = 1` is unchanged).

3. **Nested Loops**:
   - Each nested loop creates its own scope, nested within the parent loop's scope.
   - A `set` in an inner loop updates the parent loop's scope (not global) for variables that exist in the parent scope.
   - Example:
     ```jinja
     {% set x = 1 %}
     {% for i in [1, 2] %}
       {% for j in [3, 4] %}
         {% set x = j %}
         {{ x }}
       {% endfor %}
     {% endfor %}
     {{ x }}
     ```
     Output: `34341` (`x = 3`, `x = 4` in inner loop; `x = 1` globally).
   - Here, `x = j` updates the outer loop's scope, so `x` persists as `4` within the outer loop's iteration but doesn't affect the global `x`.

4. **Variable Lookup**:
   - Variables are resolved by checking the **current scope** (e.g., inner loop), then **parent scopes** (e.g., outer loop), up to the **global scope**.
   - Example:
     ```jinja
     {% set x = 1 %}
     {% for i in [1] %}
       {{ x }}  {# Resolves to global x = 1 #}
       {% set x = 2 %}
       {{ x }}  {# Resolves to loop x = 2 #}
     {% endfor %}
     {{ x }}    {# Resolves to global x = 1 #}
     ```
     Output: `121`.

5. **Edge Cases**:
   - Empty Iterables: If the iterable is empty, the loop body is skipped, and the `else` block (if present) is rendered.
     ```jinja
     {% for i in [] %}
       {{ i }}
     {% else %}
       empty
     {% endfor %}
     ```
     Output: `empty`.
   - **Shadowing**: A loop variable can shadow a global variable, but the global variable is unaffected after the loop.
     ```jinja
     {% set x = 1 %}
     {% for x in [2] %}
       {{ x }}
     {% endfor %}
     {{ x }}
     ```
     Output: `21`.
   - Multiple Variables: Loops can unpack multiple variables (e.g., `{% for k, v in dict.items() %}`), each local to the loop scope.
   - Deep Nesting: Scoping rules apply recursively for deeper nested loops.
   - Loop Variables: The `loop` object (`loop.index`, `loop.first`, etc.) is only available within the loop scope.

---

## Status

Jinja2.NET is under active development with a focus on correctness, extensibility, and compatibility with standard Jinja2 behavior. It is not a drop-in parser for all templates yet, but already supports:

* Most expressions and filters
* Set and loop blocks
* Escaping and whitespace control

Advanced features such as macros, includes, blocks and template inheritance are planned for the future. They will be implemented if you are interested.



---

## License

This project is licensed under the MIT License — free for commercial and personal use.

---

## Contributing

We welcome contributions! If you find a bug or want to help implement a missing feature, feel free to open an issue or submit a pull request.

For compatibility details, see the tests: [Jinja2.NET.Tests/TemplateTests.cs](./Jinja2.NET.Tests/TemplateTests.cs)

## History
- 1.4.0: Initial version. (Sorry, it was a copy/paste error, but I cannot roll it back on NuGet)
- 1.4.1: nuget package links update
- 
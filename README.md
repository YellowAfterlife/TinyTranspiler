# A tiny transpiler

Sometimes you have to ask yourself: what does it mean to write a compiler in modern C#? This is an exploration of that concept.

## Input

Code in a language that vaguely resembles GameMaker (or JavaScript).

It supports a rather arbitrary set of syntactic constructs, such as:

- String `"hi!"`, decimal `1`, and hexadecimal `0x10` literals.
- Local variables `var a;` .. `a = 1`.
- Common decimal (`+`, `-`, etc.), bitwise (`~`,`&`,`|`, etc.), and boolean (`&&`, `||`) operators, along with keyword aliases (`and`, `or`).
- Blocks of statements `{ a = 1; b = 2 }` and parentheses `(val)`.
- If-then-else `if condition doSomething();`, opt. `else doSomethingElse();`
- Function calls `fn(a, b, c)`.
- Array literals `a = [1, 2, 3]` and array access `a[ind] = val`.
- Loops (`for`, `while`, `do`-`while`) and `break`/`continue`.
- Field access `obj.field`.
- `return value`, `return;`, `exit`
- Other things that I'll forget to add here.

Sample snippet:
```js
// hi!
var arr = [1, 2, 3, 4, "hi!\nhello!"];
for (var i = 0; i < 5; i += 1) {
	if (i >= 2) {
		trace(arr[i]);
	}
	if (i == 4) continue;
}
```

## Output

Your same code, as seen by the compiler (with original comments removed and some comments annotating context inserted).

See my [blog post series](https://yal.cc/interpreters-guide/#compile) for notes on turning this into something that can be subsequently executed.

## Conclusions

### The good:

- Pattern matching is really nice!  
  ```cs
  case Token.Keyword kw when kw.word == "for"
  ```
  sure is much nicer than
  ```cs
  if (tk is Token.Keyword kw && kw.word == "for")
  ```
  or even scarier legacy alternatives
- "one class per token/node type" approach works pretty well, despite the verbosity.
- I was able to use generics, lambdas, and local functions to remove some of the repetetive code

### The bad:

- Writing out constructors that just invoke the parent constructor gets old quickly.
- Source generators are cool, but are tricky to use for adding to existing types 
- `switch` cases not being block-scoped upsets me dearly, even more so with Visual Studio adding an extra indentation layer if you do
  ```cs
  case Token.ParOpen: {
  	  // ...
  } break
  ```
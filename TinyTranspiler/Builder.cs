using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTranspiler {
	class Builder {
		public List<Token> tokens;
		public int pos = 0;
		public int len;
		public Node.Block node;
		public Script script;
		public List<Script> scripts;
		public Builder(Source source) {
			tokens = source.tokens;
			len = tokens.Count() - 1; // last token being EOF
			node = new Node.Block(source.start);
			script = new Script(source.name, node);
			scripts = new List<Script>() { script };
		}
		
		/// <summary>
		/// Forms you an "Expected X, got Y" exception, ready to throw.
		/// </summary>
		Exception expect(string what, Token got) {
			return new Exception($"Expected {what}, got {got}");
		}

		/// <summary>
		/// Skips the current token if it is T.
		/// </summary>
		bool skipIf<T>() where T : Token {
			var tk = tokens[pos];
			if (tk is T) {
				pos++;
				return true;
			} else return false;
		}

		/// <summary>
		/// Skips the current token if it is T and passes the test.
		/// </summary>
		bool skipIf<T>(Func<T, bool> fn) where T : Token {
			var tk = tokens[pos];
			if (tk is T t && fn(t)) {
				pos++;
				return true;
			} else return false;
		}

		/// <summary>
		/// Skips the current token if it is T, throws an error otherwise.
		/// </summary>
		void skipReq<T>(string what) where T : Token {
			if (!skipIf<T>()) throw expect(what, tokens[pos]);
		}

		/// <summary>
		/// Skips the current token if it is T and passes the test, throws an error otherwise.
		/// </summary>
		void skipReq<T>(string what, Func<T, bool> fn) where T : Token {
			if (!skipIf(fn)) throw expect(what, tokens[pos]);
		}
		
		/// <summary>
		/// Reads a chain of binary ooperators and merges them based on priority.
		/// </summary>
		Node buildBinOps(Token.Pos firstPos, Node firstNode, Token.BinOpType firstType) {
			List<Node> nodes = new List<Node>() { firstNode };
			List<Token.BinOpType> ops = new List<Token.BinOpType>() { firstType };
			List<Token.Pos> locs = new List<Token.Pos>() { firstPos };
			var proc = true;
			while (proc && pos < len) {
				nodes.Add(buildExpr(BuildExprFlags.NoBinOps));
				if (pos >= len) break;
				switch (tokens[pos]) {
					case Token.BinOp binop:
						pos++;
						locs.Add(binop.pos);
						ops.Add(binop.type);
						break;
					default: proc = false; break;
				}
			}
			//
			var pmin = (int)Token.BinOpType.MaxPriority;
			var pmax = 0;
			var n = ops.Count;
			for (var i = 0; i < n; i++) {
				var pcur = (int)ops[i] >> 4;
				if (pcur < pmin) pmin = pcur;
				if (pcur > pmax) pmax = pcur;
			}
			//
			for (; pmin <= pmax; pmin++) {
				for (var i = 0; i < n; i++) {
					if (((int)ops[i] >> 4) != pmin) continue;
					nodes[i] = new Node.BinOp(locs[i], ops[i], nodes[i], nodes[i + 1]);
					nodes.RemoveAt(i + 1);
					ops.RemoveAt(i);
					locs.RemoveAt(i);
					n--; i--;
				}
			}
			//
			return nodes[0];
		}

		/// <summary>
		/// Reads arguments for function calls and array literals, up and after the closing token.
		/// </summary>
		void buildCallArgs(Token.Pos start, List<Node> args, bool square) {
			var wantComma = false;
			while (pos < len) {
				var tk = tokens[pos];
				switch (tk) {
					case Token.ParClose when square == false:
					case Token.SquareClose when square == true:
						pos++;
						return;
					case Token.Comma:
						if (wantComma) {
							wantComma = false;
							pos++;
							break;
						} else throw expect("an argument or a closing token", tk);
					default:
						if (wantComma) throw expect("a comma or a closing token", tk);
						args.Add(buildExpr());
						wantComma = true;
						break;
				}
			}
			var kind = square ? "[]" : "()";
			throw new Exception($"Unclosed {kind} starting at {start}");
		}

		[Flags] enum BuildExprFlags {
			None = 0,
			NoBinOps = 1,
			NoSuffixes = 2,
			AsStatement = 4,
		}

		Node buildExprImpl(BuildExprFlags flags = BuildExprFlags.None) {
			var tk = tokens[pos++];
			var tp = tk.pos;
			Node node;
			switch (tk) {
				case Token.Number n: return new Node.Number(tp, n.value);
				case Token.CString s: return new Node.CString(tp, s.value);
				case Token.Ident id: return new Node.Ident(tp, id.value);
				case Token.ParOpen: // (val)
					node = buildExpr();
					skipReq<Token.ParClose>("a closing `)`");
					return node;
				case Token.SquareOpen: // [v1, v2, v3]
					var arr = new Node.ArrayLiteral(tp);
					buildCallArgs(tp, arr.values, true);
					return arr;
				case Token.BinOp minus when minus.type == Token.BinOpType.Subtract: // -val
					node = buildExpr(BuildExprFlags.NoBinOps);
					return new Node.UnOp(tp, Token.UnOpType.Negate, node);
				case Token.UnOp unop: // ~val, !val
					node = buildExpr(BuildExprFlags.NoBinOps);
					return new Node.UnOp(tp, unop.type, node);
				default:
					var kind = (flags & BuildExprFlags.AsStatement) != 0 ? "a statement" : "an expression";
					throw expect(kind, tk);
			}
		}

		/// <summary>
		/// Reads an expression - either inline or inline-as-statement (like `func()`)
		/// </summary>
		Node buildExpr(BuildExprFlags flags = BuildExprFlags.None) {
			var node = buildExprImpl(flags);
			bool hasFlag(BuildExprFlags flag) => (flags & flag) == flag;
			bool noFlag(BuildExprFlags flag) => (flags & flag) == 0;
			while (pos < len) {
				var tk = tokens[pos++];
				switch (tk) {
					case Token.Period when noFlag(BuildExprFlags.NoSuffixes): // a.b
						var fdtk = tokens[pos++];
						if (fdtk is Token.Ident id) {
							node = new Node.Field(tk.pos, node, id.value);
						} else throw expect("a field name", fdtk);
						break;
					case Token.SetOp setop when hasFlag(BuildExprFlags.AsStatement): // a = b
						if (!node.isSettable()) throw new Exception($"Expression is not settable: {node}");
						node = new Node.SetOp(tk.pos, setop.type, node, buildExpr());
						flags |= BuildExprFlags.NoBinOps | BuildExprFlags.NoSuffixes;
						break;
					case Token.BinOp binop when noFlag(BuildExprFlags.NoBinOps): // a + b
						node = buildBinOps(tk.pos, node, binop.type);
						flags |= BuildExprFlags.NoBinOps;
						break;
					case Token.ParOpen when noFlag(BuildExprFlags.NoSuffixes): // fn(...)
						var call = new Node.Call(tk.pos, node);
						buildCallArgs(tk.pos, call.args, false);
						node = call;
						break;
					case Token.SquareOpen when noFlag(BuildExprFlags.NoSuffixes): // arr[ind]
						node = new Node.ArrayAccess(tk.pos, node, buildExpr());
						skipReq<Token.SquareClose>("a closing `]`");
						break;
					default: // bye!
						pos--;
						return node;
				}
			}
			return node;
		}

		Node buildStatementImpl() {
			var tk = tokens[pos++];
			var tp = tk.pos;
			switch (tk) {
				case Token.CurlyOpen: // {}
					var block = new Node.Block(tp);
					while (pos < len) {
						if (skipIf<Token.CurlyClose>()) return block;
						block.nodes.Add(buildStatement());
					}
					throw new Exception("Unclosed {} starting at " + tp);
				case Token.Keyword kw when kw.word == "if": // if val [then] stat [else stat]
					var @if = buildExpr();
					skipIf((Token.Keyword kw) => kw.word == "then");
					var @then = buildStatement();
					Node @else;
					if (skipIf((Token.Keyword kw) => kw.word == "else")) {
						@else = buildStatement();
					} else @else = null;
					return new Node.If(tp, @if, then, @else);
				case Token.Keyword kw when kw.word == "exit": // exit
					return new Node.Return(tp, null);
				case Token.Keyword kw when kw.word == "return": // return [val]
					Node retNode;
					switch (tokens[pos]) {
						case Token.Keyword:
						case Token.Semico: // return; / return <unrelated keyword>
							retNode = null;
							break;
						default:
							retNode = buildExpr();
							break;
					}
					return new Node.Return(tp, retNode);
				case Token.Keyword kw when kw.word == "var": // var name[= val][, name2[= val2]]
					var varBlock = new Node.Block(tp);
					var loop = true;
					while (loop && pos < len) {
						tk = tokens[pos++];
						if (!(tk is Token.Ident id)) throw expect("a variable name", tk);
						script.locals[id.value] = tk.pos;
						var varNode = new Node.Var(tk.pos, id.value, null);
						switch (tokens[pos]) {
							case Token.SetOp setop when setop.type == Token.BinOpType.Set:
								pos++;
								varNode.value = buildExpr();
								if (tokens[pos] is Token.Comma) pos++; else loop = false;
								break;
							case Token.Comma: break;
							default: loop = false; break;
						}
						varBlock.nodes.Add(varNode);
					}
					return varBlock.nodes.Count != 1 ? varBlock : varBlock.nodes[0];
				case Token.Keyword kw when kw.word == "for": // for (init; cond; post) loop
					skipReq<Token.ParOpen>("an opening parenthesis in for");
					var init = buildStatement();
					var cond = buildExpr();
					skipIf<Token.Semico>();
					var post = buildStatement();
					skipReq<Token.ParClose>("a closing parenthesis in for");
					var forNode = buildStatement();
					return new Node.For(tp, init, cond, post, forNode);
				case Token.Keyword kw when kw.word == "while": // while cond stat
					var whileCond = buildExpr();
					var whileStat = buildStatement();
					return new Node.While(tp, whileCond, whileStat);
				case Token.Keyword kw when kw.word == "do": // do stat while cond
					var doStat = buildStatement();
					skipReq("a `while` in do-while", (Token.Keyword w) => w.word == "while");
					var doCond = buildExpr();
					return new Node.DoWhile(tp, doStat, doCond);
				case Token.Keyword kw when kw.word == "break": return new Node.Break(tp);
				case Token.Keyword kw when kw.word == "continue": return new Node.Continue(tp);
				default:
					pos--;
					var expr = buildExpr(BuildExprFlags.AsStatement);
					if (expr.isStatement()) {
						return expr;
					} else throw new Exception($"Expected a statement, got {expr}");
			}
		}
		Node buildStatement() {
			var node = buildStatementImpl();
			// consume subsequent semicolons:
			while (pos < len && tokens[pos] is Token.Semico) pos++;
			return node;
		}

		/// <summary>
		/// Reads statements and adds them to the top-level node until end-of-file is reached.
		/// </summary>
		public void build() {
			while (pos < len) {
				if (tokens[pos] is Token.EOF) break;
				node.nodes.Add(buildStatement());
			}
		}
	}
}

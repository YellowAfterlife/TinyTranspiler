using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTranspiler {
	class Parser {
		Source source;
		string code;
		int pos;
		int len;
		List<Token> tokens;
		//
		private static Dictionary<string, bool> keysToIs(List<string> keys) {
			var d = new Dictionary<string, bool>();
			foreach (var k in keys) d[k] = true;
			return d;
		}
		private static Dictionary<string, Func<Token.Pos, string, Token>> initKeywords() {
			var r = new Dictionary<string, Func<Token.Pos, string, Token>>();
			Token kw(Token.Pos tp, string word) {
				return new Token.Keyword(tp, word);
			}
			foreach (var w in "if|then|else|return|exit".Split('|')) r[w] = kw;
			foreach (var w in "for|do|while|continue|break".Split('|')) r[w] = kw;
			r["var"] = kw;
			r["div"] = (tp, _) => new Token.BinOp(tp, Token.BinOpType.IntDiv);
			r["mod"] = (tp, _) => new Token.BinOp(tp, Token.BinOpType.Modulo);
			r["and"] = (tp, _) => new Token.BinOp(tp, Token.BinOpType.BoolAnd);
			r["or"] = (tp, _) => new Token.BinOp(tp, Token.BinOpType.BoolOr);
			r["not"] = (tp, _) => new Token.UnOp(tp, Token.UnOpType.Not);
			//
			return r;
		}
		static Dictionary<string, Func<Token.Pos, string, Token>> keywordMap = initKeywords();

		public Parser() {

		}
		string slice(int start, int end) {
			return code.Substring(start, end - start);
		}
		void add(Token tk) {
			tokens.Add(tk);
		}

		/// <summary>
		/// `+` => BinOp, `+=` => SetOp
		/// </summary>
		void readBinOp(Token.Pos tp, Token.BinOpType op) {
			if (pos < len && code[pos] == '=') {
				pos++;
				add(new Token.SetOp(tp, op));
			} else add(new Token.BinOp(tp, op));
		}

		/// <summary>
		/// Reading `<` or `<=` accordingly
		/// </summary>
		void readCmpOp(Token.Pos tp, Token.BinOpType opEq, Token.BinOpType op) {
			if (pos < len && code[pos] == '=') {
				pos++;
				add(new Token.BinOp(tp, opEq));
			} else add(new Token.BinOp(tp, op));
		}

		void readNumber(Token.Pos tp, char first) {
			var seenDot = first == '.';
			while (pos < len) {
				var c = code[pos];
				if (c >= '0' && c <= '9') {
					pos++;
				} else if (c == '.') {
					if (seenDot) break;
					seenDot = true;
					pos++;
				} else break;
			}
			add(new Token.Number(tp, slice(tp.pos, pos)));
		}

		public List<Token> parse(Source _source) {
			tokens = new List<Token>();
			source = _source;
			pos = 0;
			len = _source.code.Length;
			code = _source.code + "\x1B";
			//
			var rowStart = 0;
			var row = 0;
			while (pos < len) {
				var start = pos;
				var tp = new Token.Pos(_source, pos, row, pos - rowStart);
				switch (code[pos++]) {
					case ' ': case '\t': case '\r': break;
					case '\n':
						row++;
						rowStart = pos;
						break;
					case '(': add(new Token.ParOpen(tp)); break;
					case ')': add(new Token.ParClose(tp)); break;
					case '[': add(new Token.SquareOpen(tp)); break;
					case ']': add(new Token.SquareClose(tp)); break;
					case '{': add(new Token.CurlyOpen(tp)); break;
					case '}': add(new Token.CurlyClose(tp)); break;
					case ';': add(new Token.Semico(tp)); break;
					case ',': add(new Token.Comma(tp)); break;
					case '+': readBinOp(tp, Token.BinOpType.Add); break;
					case '-': readBinOp(tp, Token.BinOpType.Subtract); break;
					case '%': readBinOp(tp, Token.BinOpType.Modulo); break;
					case '~': add(new Token.UnOp(tp, Token.UnOpType.BitNot)); break;
					case '*':
						if (code[pos] == '*') {
							pos++;
							readBinOp(tp, Token.BinOpType.Power);
						} else readBinOp(tp, Token.BinOpType.Multiply);
						break;
					case '/':
						if (code[pos] == '*') {
							pos += 1;
							while (pos < len - 1) {
								if (code[pos] == '*' && code[pos + 1] == '/') {
									pos += 2;
									break;
								} else pos += 1;
							}
						} else if (code[pos] == '/') { // comment!
							while (pos < len) {
								if (code[pos] == '\n') break;
								pos += 1;
							}
						} else readBinOp(tp, Token.BinOpType.Divide);
						break;
					case '<':
						if (code[pos] == '<') {
							pos++;
							readBinOp(tp, Token.BinOpType.BitShiftLeft);
						} else readCmpOp(tp, Token.BinOpType.LessThanOrEqual, Token.BinOpType.LessThan);
						break;
					case '>':
						if (code[pos] == '>') {
							pos++;
							readBinOp(tp, Token.BinOpType.BitShiftRight);
						} else readCmpOp(tp, Token.BinOpType.GreaterThanOrEqual, Token.BinOpType.GreaterThan);
						break;
					case '=':
						if (code[pos] == '=') {
							pos++;
							add(new Token.BinOp(tp, Token.BinOpType.Equals));
						} else add(new Token.SetOp(tp, Token.BinOpType.Set));
						break;
					case '!':
						if (code[pos] == '=') {
							pos++;
							add(new Token.BinOp(tp, Token.BinOpType.NotEquals));
						} else add(new Token.UnOp(tp, Token.UnOpType.Not));
						break;
					case '"': // a string! with escape characters!
						var sb = new StringBuilder();
						var strFound = false;
						while (pos < len) {
							var c = code[pos++];
							if (c == '"') {
								strFound = true;
								add(new Token.CString(tp, sb.ToString()));
								break;
							} else if (c == '\\') {
								switch (code[pos++]) {
									case 'r': sb.Append('\r'); break;
									case 'n': sb.Append('\n'); break;
									case 't': sb.Append('\t'); break;
									case 'b': sb.Append('\b'); break;
									case '"': sb.Append('"'); break;
									case 'x':
										var hc = (char)int.Parse(code.Substring(pos, 2), System.Globalization.NumberStyles.HexNumber);
										sb.Append(hc);
										pos += 2;
										break;
									case '\\': sb.Append('\\'); break;
									case var c1: sb.Append(c1); break;
								}
							} else sb.Append(c);
						}
						if (!strFound) throw new Exception($"Unclosed string starting at ${tp}");
						break;
					case '.': // . or .1
						if (code[pos] >= '0' && code[pos] <= '9') {
							readNumber(tp, '.');
						} else add(new Token.Period(tp));
						break;
					case var c when c == '0' && code[pos] == 'x': // hex literal!
						pos++;
						while (pos < len) {
							c = code[pos];
							if (c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F') {
								pos++;
							} else break;
						}
						add(new Token.Number(tp, slice(start, pos)));
						break;
					case var c when c >= '0' && c <= '9': // decimal!
						readNumber(tp, c);
						break;
					case var c when c == '_' || c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z': // words!
						while (pos < len) {
							c = code[pos];
							if (c == '_' || c >= '0' && c <= '9' || c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z') {
								pos++;
							} else break;
						}
						//
						var word = slice(start, pos);
						if (keywordMap.TryGetValue(word, out var fn)) {
							add(fn(tp, word));
						} else add(new Token.Ident(tp, word));
						break;
					case var c: throw new Exception($"Unknown character `{c}`");
				}
			}
			add(new Token.EOF(_source.end));
			return tokens;
		}
	}
}

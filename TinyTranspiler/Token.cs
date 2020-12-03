using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTranspiler {
	public class Token {
		public Pos pos;
		public Token(Pos tp) {
			pos = tp;
		}
		public override string ToString() {
			return pos.ToString() + base.ToString();
		}

		public class Pos {
			public Source source;
			public int pos;
			public int row;
			public int col;
			public Pos(Source _source, int _pos, int _row, int _col) {
				source = _source;
				pos = _pos;
				row = _row;
				col = _col;
			}
			public override string ToString() {
				return $"[L{row + 1},c{col + 1}]";
			}
		}
		//
		public class EOF : Token { public EOF(Pos tp) : base(tp) { } }
		// lonely glyphs (aka: I'm not a huge fan of neither these constructors nor `case Glyph g when g.@char == '('`)
		public class Semico : Token { public Semico(Pos tp) : base(tp) { } }
		public class Comma : Token { public Comma(Pos tp) : base(tp) { } }
		public class Period : Token { public Period(Pos tp) : base(tp) { } }
		public class ParOpen : Token { public ParOpen(Pos tp) : base(tp) { } }
		public class ParClose : Token { public ParClose(Pos tp) : base(tp) { } }
		public class SquareOpen : Token { public SquareOpen(Pos tp) : base(tp) { } }
		public class SquareClose : Token { public SquareClose(Pos tp) : base(tp) { } }
		public class CurlyOpen : Token { public CurlyOpen(Pos tp) : base(tp) { } }
		public class CurlyClose : Token { public CurlyClose(Pos tp) : base(tp) { } }
		// literals:
		public class Literal : Token {
			public string value;
			public Literal(Pos tp, string value) : base(tp) { this.value = value; }
			public override string ToString() => base.ToString() + $"({value})";
		}
		public class Number : Literal {
			public Number(Pos tp, string value) : base(tp, value) { }
		}
		public class CString : Literal {
			public CString(Pos tp, string value) : base(tp, value) { }
		}
		// words:
		public class Ident : Literal {
			public Ident(Pos tp, string value) : base(tp, value) { }
		}
		public class Keyword : Token {
			public string word;
			public Keyword(Pos tp, string _word) : base(tp) { word = _word; }
			public override string ToString() {
				return base.ToString() + $"({word})";
			}
		}
		//
		[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
		public class OpAttribute : Attribute {
			public readonly string name;
			public OpAttribute(string name) {
				this.name = name;
			}
		}

		public enum BinOpType : short {
			[Op("=")] Set = -1,
			[Op("*")] Multiply = 0x00,
			[Op("/")] Divide,
			[Op("%")] Modulo,
			[Op("div")] IntDiv,
			[Op("**")] Power,
			[Op("+")] Add = 0x10,
			[Op("-")] Subtract,
			[Op("..")] Concat,
			[Op("<<")] BitShiftLeft = 0x20,
			[Op(">>")] BitShiftRight,
			[Op("|")] BitOr = 0x30,
			[Op("&")] BitAnd,
			[Op("^")] BitXor,
			[Op("==")] Equals = 0x40,
			[Op("!=")] NotEquals,
			[Op("<")] LessThan,
			[Op(">")] GreaterThan,
			[Op("<=")] LessThanOrEqual,
			[Op(">=")] GreaterThanOrEqual,
			[Op("&&")] BoolAnd = 0x50,
			[Op("||")] BoolOr = 0x60,
			MaxPriority = 7, // (max >> 4) + 1
		}

		/// `+`, `-`, etc.
		public class BinOp : Token {
			public BinOpType type;
			public BinOp(Pos tp, BinOpType _op) : base(tp) { type = _op; }
			public override string ToString() {
				return base.ToString() + $"({type})";
			}
		}

		/// `=`, `+=`, etc.
		public class SetOp : Token {
			public BinOpType type;
			public SetOp(Pos tp, BinOpType _op) : base(tp) { type = _op; }
			public override string ToString() {
				return base.ToString() + $"({type})";
			}
		}

		public enum UnOpType : byte {
			[Op("!")] Not,
			[Op("-")] Negate,
			[Op("~")] BitNot,
		}

		/// `!`, `~`
		public class UnOp : Token {
			public UnOpType type;
			public UnOp(Pos tp, UnOpType _op) : base(tp) { type = _op; }
			public override string ToString() {
				return base.ToString() + $"({type})";
			}
		}
	}
}

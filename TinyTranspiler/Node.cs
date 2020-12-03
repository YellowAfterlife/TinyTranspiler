using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTranspiler {
	public class Node {
		public Token.Pos pos;
		public virtual bool isStatement() => false;
		public virtual bool isSettable() => false;
		public Node(Token.Pos tp) {
			pos = tp;
		}
		public virtual void print(Printer printer, Printer.Flags flags) {
			throw new NotImplementedException();
		}
		//
		public delegate bool SeekFunc(ref Node node, List<Node> st);
		protected bool seekList(List<Node> list, SeekFunc fn, List<Node> st) {
			var n = list.Count;
			for (var i = 0; i < n; i++) {
				// this nonsense is dedicated to CS0206
				var node = list[i];
				var found = fn(ref node, st);
				list[i] = node;
				if (found) return true;
			}
			return false;
		}
		/// Should call fn with each of inner nodes, stopping if any return `true`.
		protected virtual bool seekRec(SeekFunc fn, List<Node> st) => false;
		public bool seek(SeekFunc fn, List<Node> st = null) {
			if (st != null) st.Insert(0, this);
			var result = seekRec(fn, st);
			if (st != null) st.RemoveAt(0);
			return result;
		}
		//
		public delegate void IterFunc(ref Node node, List<Node> st);
		public virtual void iter(IterFunc fn, List<Node> st = null) {
			seek((ref Node node, List<Node> st) => {
				fn(ref node, st);
				return false;
			}, st);
		}
		//
		public override string ToString() {
			return $"{pos} {base.ToString()}";
		}
		//
		public class Literal : Node {
			public string value;
			public Literal(Token.Pos tp, string value) : base(tp) { this.value = value; }
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addString(value);
			}
			public override string ToString() => base.ToString() + $"({value})";
		}
		public class Number : Literal {
			public Number(Token.Pos tp, string text) : base(tp, text) { }
		}
		public class CString : Literal {
			public CString(Token.Pos tp, string text) : base(tp, text) { }
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addString("\"" + value + "\"");
			}
		}
		public class Ident : Literal {
			public Ident(Token.Pos tp, string text) : base(tp, text) { }
			public override bool isSettable() => true;
		}
		public class Local : Literal {
			public Local(Token.Pos tp, string text) : base(tp, text) { }
			public override bool isSettable() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addString("/* local */");
				base.print(printer, flags);
			}
		}
		public class Field : Node {
			public Node node;
			public string field;
			public Field(Token.Pos tp, Node node, string field) : base(tp) {
				this.field = field;
				this.node = node;
			}
			public override bool isSettable() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addNode(node);
				printer.addChar('.');
				printer.addString(field);
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) => fn(ref node, st);
		}

		public class UnOp : Node {
			public Token.UnOpType type;
			public Node node;
			public UnOp(Token.Pos tp, Token.UnOpType type, Node node) : base(tp) {
				this.type = type;
				this.node = node;
			}
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addString(type.GetAttribute<Token.OpAttribute>()?.name);
				printer.addNode(node);
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) => fn(ref node, st);
		}

		public class BinOp : Node {
			public Token.BinOpType type;
			public Node left, right;
			public BinOp(Token.Pos tp, Token.BinOpType type, Node left, Node right) : base(tp) {
				this.type = type;
				this.left = left;
				this.right = right;
			}
			public override void print(Printer printer, Printer.Flags flags) {
				var wrap = (flags & Printer.Flags.InPar) == 0;
				if (wrap) printer.addString("(");
				printer.addNode(left);
				printer.addString(" ");
				printer.addString(type.GetAttribute<Token.OpAttribute>()?.name);
				printer.addString(" ");
				printer.addNode(right);
				if (wrap) printer.addString(")");
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) => fn(ref left, st) || fn(ref right, st);
		}

		public class SetOp : Node {
			public Token.BinOpType type;
			public Node left, right;
			public SetOp(Token.Pos tp, Token.BinOpType type, Node left, Node right) : base(tp) {
				this.type = type;
				this.left = left;
				this.right = right;
			}
			public override bool isStatement() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addNode(left);
				printer.addString(" ");
				if (type != Token.BinOpType.Set) {
					printer.addString(type.GetAttribute<Token.OpAttribute>()?.name);
				}
				printer.addString("= ");
				printer.addArg(right);
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) => fn(ref left, st) || fn(ref right, st);
		}

		public class Block : Node {
			public List<Node> nodes = new List<Node>();
			public Block(Token.Pos tp) : base(tp) { }
			public override bool isStatement() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				switch (nodes.Count) {
					case 0:
						if ((flags & Printer.Flags.InBlock) == 0) printer.addString("{}");
						break;
					case 1:
						printer.addNode(nodes[0]);
						break;
					default:
						var wrap = (flags & Printer.Flags.InBlock) == 0;
						if (wrap) {
							printer.addString("{");
							printer.addLine(1);
						}
						//
						var sep = false;
						foreach (var node in nodes) {
							if (sep) printer.addLine(); else sep = true;
							printer.addNode(node, Printer.Flags.InBlock);
						}
						//
						if (wrap) {
							printer.addLine(-1);
							printer.addString("}");
						}
						break;
				}
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) {
				for (var i = 0; i < nodes.Count; i++) {
					// this nonsense is dedicated to CS0206
					var node = nodes[i];
					var z = fn(ref node, st);
					nodes[i] = node;
					if (z) return true;
				}
				return false;
			}
		}

		public class ArrayLiteral : Node {
			public List<Node> values = new List<Node>();
			public ArrayLiteral(Token.Pos tp) : base(tp) {  }
			public override bool isStatement() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addString("[");
				var sep = false;
				foreach (var arg in values) {
					if (sep) printer.addString(", "); else sep = true;
					printer.addArg(arg);
				}
				printer.addString("]");
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) => seekList(values, fn, st);
		}

		public class ArrayAccess : Node {
			public Node node, index;
			public ArrayAccess(Token.Pos tp, Node node, Node index) : base(tp) { this.node = node; this.index = index; }
			public override bool isSettable() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addNode(node);
				printer.addChar('[');
				printer.addArg(index);
				printer.addChar(']');
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) => fn(ref node, st) || fn(ref index, st);
		}

		public class Call : Node {
			public Node node;
			public List<Node> args = new List<Node>();
			public Call(Token.Pos tp, Node node) : base(tp) { this.node = node; }
			public override bool isStatement() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addNode(node);
				printer.addString("(");
				var sep = false;
				foreach (var arg in args) {
					if (sep) printer.addString(", "); else sep = true;
					printer.addArg(arg);
				}
				printer.addString(")");
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) => fn(ref node, st) || seekList(args, fn, st);
		}

		public class Return : Node {
			public Node node;
			public Return(Token.Pos tp, Node node) : base(tp) { this.node = node; }
			public override bool isStatement() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				if (node != null) {
					printer.addString("return ");
					printer.addArg(node);
				} else printer.addString("exit");
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) => node != null && fn(ref node, st);
		}

		public class If : Node {
			public Node @if;
			public Node then;
			public Node @else;
			public If(Token.Pos tp, Node @if, Node then, Node @else) : base(tp) {
				this.@if = @if;
				this.then = then;
				this.@else = @else;
			}
			public override bool isStatement() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addString("if (");
				printer.addArg(@if);
				printer.addString(") ");
				// prefer {} if there is an else-branch
				if (@else != null) {
					printer.addBlock(then);
				} else printer.addNode(then);
				if (@else != null) {
					printer.addString(" else ");
					printer.addNode(@else);
				}
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) {
				return fn(ref @if, st) || fn(ref then, st) || (@else != null && fn(ref @else, st));
			}
		}

		#region Loops
		public class For : Node {
			public Node init, cond, post, loop;
			public For(Token.Pos tp, Node init, Node cond, Node post, Node loop) : base(tp) {
				this.init = init;
				this.cond = cond;
				this.post = post;
				this.loop = loop;
			}
			public override bool isStatement() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addString("for (");
				printer.addNode(init);
				printer.addString("; ");
				printer.addArg(cond);
				printer.addString("; ");
				printer.addNode(post);
				printer.addString(") ");
				printer.addNode(loop);
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) {
				return fn(ref init, st) || fn(ref cond, st) || fn(ref post, st) || fn(ref loop, st);
			}
		}

		public class While : Node {
			public Node cond, loop;
			public While(Token.Pos tp, Node cond, Node loop) : base(tp) {
				this.cond = cond;
				this.loop = loop;
			}
			public override bool isStatement() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addString("while (");
				printer.addArg(cond);
				printer.addString(") ");
				printer.addNode(loop);
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) {
				return fn(ref cond, st) || fn(ref loop, st);
			}
		}

		public class DoWhile : Node {
			public Node loop, cond;
			public DoWhile(Token.Pos tp, Node loop, Node cond) : base(tp) {
				this.loop = loop;
				this.cond = cond;
			}
			public override bool isStatement() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addString("do ");
				printer.addBlock(loop);
				printer.addString(" while (");
				printer.addArg(cond);
				printer.addString(")");
			}
			protected override bool seekRec(SeekFunc fn, List<Node> st) {
				return fn(ref loop, st) || fn(ref cond, st);
			}
		}

		public class Break : Node {
			public Break(Token.Pos tp) : base(tp) { }
			public override bool isStatement() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addString("break");
			}
		}

		public class Continue : Node {
			public Continue(Token.Pos tp) : base(tp) { }
			public override bool isStatement() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addString("continue");
			}
		}
		#endregion

		public class Var : Node {
			public string name;
			public Node value;
			public Var(Token.Pos tp, string name, Node value) : base(tp) {
				this.name = name;
				this.value = value;
			}
			public override bool isStatement() => true;
			public override void print(Printer printer, Printer.Flags flags) {
				printer.addString($"var {name}");
				if (value != null) {
					printer.addString(" = ");
					printer.addArg(value);
				}
			}
			public override string ToString() => base.ToString() + $"({name}, {value})";
			protected override bool seekRec(SeekFunc fn, List<Node> st) => value != null && fn(ref value, st);
		}
	}
}

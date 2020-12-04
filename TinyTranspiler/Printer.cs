using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTranspiler {
	/// <summary>
	/// A convenience wrapper around StringBuilder.
	/// </summary>
	public class Printer {
		public StringBuilder buf = new StringBuilder();
		public int depth = 0;
		public Printer() {

		}

		public void addChar(char val) {
			buf.Append(val);
		}

		public void addString(string val) {
			buf.Append(val);
		}

		/// <summary>
		/// Adds a linebreak, optionally also changing indentation level prior to that
		/// </summary>
		public void addLine(int depthDelta = 0) {
			buf.AppendLine();
			depth += depthDelta;
			for (var i = 0; i < depth; i++) {
				buf.Append("\t");
			}
		}

		/// <summary>
		/// Adds a node to this printer by calling node.print
		/// </summary>
		public void addNode(Node node, Flags flags = Flags.None) {
			node.print(this, flags);
		}

		/// <summary>
		/// Shorthand for addNode with flags |= Flags.InPar
		/// </summary>
		public void addArg(Node node, Flags flags = Flags.None) {
			node.print(this, flags | Flags.InPar);
		}

		/// <summary>
		/// Adds a expression wrapped in {}
		/// </summary>
		public void addBlock(Node node) {
			addString("{");
			addLine(1);
			node.print(this, Flags.InBlock);
			addLine(-1);
			addString("}");
		}
		
		public override string ToString() {
			return buf.ToString();
		}

		[Flags] public enum Flags {
			None = 0,
			/// Is inside {}
			InBlock = 1,
			/// Is inside ()
			InPar = 2, 
		}
	}
}

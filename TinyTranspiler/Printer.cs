using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTranspiler {
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
		public void addNode(Node node, Flags flags = Flags.None) {
			node.print(this, flags);
		}
		/// Shorthand for addNode with flags |= Flags.InPar
		public void addArg(Node node, Flags flags = Flags.None) {
			node.print(this, flags | Flags.InPar);
		}
		/// Adds a expression, warranted wrap in {}
		public void addBlock(Node node) {
			if (node is Node.Block b && b.nodes.Count != 1) {
				node.print(this, Flags.None);
			} else {
				addString("{");
				addLine(1);
				node.print(this, Flags.InBlock);
				addLine(-1);
				addString("}");
			}
		}
		public void addLine(int depthDelta = 0) {
			buf.AppendLine();
			depth += depthDelta;
			for (var i = 0; i < depth; i++) {
				buf.Append("\t");
			}
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

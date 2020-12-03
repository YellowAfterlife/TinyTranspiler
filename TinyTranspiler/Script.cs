using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTranspiler {
	public class Script {
		public string name;
		public Node.Block node;
		public Dictionary<string, Token.Pos> locals = new Dictionary<string, Token.Pos>();
		public Script(string name, Node.Block node) {
			this.name = name;
			this.node = node;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTranspiler {
	/// <summary>
	/// A script represents a single file-scope in the program.
	/// </summary>
	public class Script {
		public string name;

		/// <summary>
		/// Contains top-level nodes of this script
		/// </summary>
		public Node.Block node;

		/// <summary>
		/// A map of local variables and where they are last defined.
		/// </summary>
		public Dictionary<string, Token.Pos> locals = new Dictionary<string, Token.Pos>();
		public Script(string name, Node.Block node) {
			this.name = name;
			this.node = node;
		}
	}
}

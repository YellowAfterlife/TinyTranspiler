using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTranspiler {
	public class Source {
		/// <summary>
		/// [File]name of this source
		/// </summary>
		public string name;

		/// <summary>
		/// Original source code as text
		/// </summary>
		public string code;

		/// <summary>
		/// Tokens of this file, once parsed
		/// </summary>
		public List<Token> tokens = null;

		/// <summary>
		/// Start-of-file marker
		/// </summary>
		public Token.Pos start;

		/// <summary>
		/// End-of-file marker
		/// </summary>
		public Token.Pos end;

		public Source(string _name, string _code) {
			name = _name;
			code = _code;
			start = new Token.Pos(this, 0, 0, 0);
			{
				var row = 0;
				int np = 0;
				var lastRowStart = 0;
				while ((np = _code.IndexOf('\n', np)) != -1) {
					lastRowStart = np++;
					row++;
				}
				end = new Token.Pos(this, _code.Length, row, _code.Length - lastRowStart);
			};
		}

		public void parse() {
			var parser = new Parser();
			tokens = parser.parse(this);
		}
	}
}

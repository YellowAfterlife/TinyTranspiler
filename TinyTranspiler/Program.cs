using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTranspiler {
	public class Program {
		public List<Source> sources;
		public List<Script> scripts = new List<Script>();
		public Program(List<Source> sources) {
			this.sources = sources;
		}
		public void compile() {
			foreach (var src in sources) src.parse();
			foreach (var src in sources) {
				var b = new Builder(src);
				b.build();
				foreach (var scr in b.scripts) scripts.Add(scr);
			}
			check();
		}
		//
		public void iter(Action<Script, Node> fn) {
			foreach (var scr in scripts) {
				fn(scr, scr.node);
			}
		}
		//
		void check() {
			iter((scr, node) => { // tag locals
				void iter(ref Node node, List<Node> st) {
					if (node is Node.Ident id) {
						if (scr.locals.ContainsKey(id.value)) {
							node = new Node.Local(node.pos, id.value);
						}
					} else node.iter(iter, st);
				}
				node.iter(iter);
			});
			iter((scr, node) => { // verify break/continue
				var canBreak = false;
				var canContinue = false;
				void iter(ref Node node, List<Node> st) {
					switch (node) {
						case Node.Break when !canBreak: throw new Exception($"Can't break here {node}");
						case Node.Break when !canContinue: throw new Exception($"Can't continue here {node}");
						case Node.For:
							var couldBreak = canBreak;
							var couldContinue = canContinue;
							canBreak = true;
							canContinue = true;
							node.iter(iter, st);
							canBreak = couldBreak;
							canContinue = couldContinue;
							break;
						default: node.iter(iter, st); break;
					}
				}
				node.iter(iter);
			});
		}
		public string print() {
			var p = new Printer();
			foreach (var scr in scripts) {
				p.addString($"/// {scr.name}:");
				p.addLine();
				p.addNode(scr.node, Printer.Flags.InBlock);
				p.addLine();
			}
			return p.ToString();
		}

		static void Main(string[] args) {
			string code;
			try {
				code = System.IO.File.ReadAllText("test.gml"); // not actually GML
			} catch {
				Console.WriteLine("Well why, you should probably provide a test file.");
				Console.WriteLine("Put your code in `test.gml` next to the executable.");
				Console.ReadLine();
				return;
			}
			var s1 = new Source("test", code);
			var pg = new Program(new List<Source>() { s1 });
			void CompileAndPrint() {
				pg.compile();
				Console.WriteLine(pg.print());
			}
			//
			#if false // allow exceptions
			CompileAndPrint();
			#else
			try {
				CompileAndPrint();
			} catch (Exception e) {
				Console.WriteLine(e.Message);
			}
			#endif
			Console.ReadLine();
		}
	}
}

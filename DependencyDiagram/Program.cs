using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DependencyDiagram {
	class Program {
		private const string ITEM_RANDO = @"C:\Users\sloan\source\repos\VSR.ItemRandomizer\ItemRandomizer\bin\Debug\net46\ItemRandomizer.dll";
		private const string DEPDIA = @"C:\Users\sloan\source\repos\VSR.DependencyDiagramsAndOtherFunThings\DependencyDiagram\bin\Debug\DependencyDiagram.exe";
		private const string VSR = @"C:\Users\sloan\source\repos\VSR.ItemRandomizer\LIB\Assembly-CSharp.dll";

		private const string DOTPATH = @"C:\Users\sloan\source\repos\VSR.ItemRandomizer\";

		private static List<(string OrigMethod, string CalledMethod)> _methodMethodList = new List<(string, string)>();
		private static List<(string TypeFullName, string MethodName)> _typeMethodList = new List<(string, string)>();
		static void Main(string[] args) {
			Console.WriteLine("Beginning Diagram PNG!");
			string assemblyFile = DEPDIA;
			string outFileName = assemblyFile.Split('\\').Last().Split('.').First() + ".dot";
			string outFile = DOTPATH + outFileName;

			//expandedScopes parameter determines which parts of the code will resolve the method details, or just show an arrow to a type node instead.
			List<string> expandedScopes = new List<string>() { "Assembly-CSharp" };
			CodeVisualizer viz = new CodeVisualizer(assemblyFile, expandedScopes);

			viz.ExpandAnyDotDllScope = true;
			viz.ShowOnlyExpanded = true;
			viz.SubgraphsByScope = false;
			viz.ShowDisconnectedTypes = false;
			viz.ShowDisconnectedMethods = false;
			viz.IncludeConstructors = true;

			File.WriteAllText(outFile, viz.CompileDotLang());

			//Make the PNG from the dot lang
			string strCmdText;
			strCmdText = @"/c ""C:\Program Files\Graphviz\bin\dot"" -Tpng -O " + outFile;
			Process.Start("CMD.exe", strCmdText);
			Console.WriteLine("Finished Writing Diagram PNG!");
		}
	}
}

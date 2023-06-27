using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DependencyDiagram {
	public partial class CodeVisualizer {
		public bool ShowOnlyExpanded { get; set; } = true;
		public bool SubgraphsByScope { get; set; } = true;
		public bool ShowDisconnectedTypes { get; set; } = true;
		public bool ShowDisconnectedMethods { get; set; } = true;
		public bool ExpandAnyDotDllScope { get; set; } = false;
		public bool IncludeConstructors { get; set; } = false;

		public string TargetFilePath { get; }

		private List<string> _expandedScopes;

		private List<CallDef> _callDefs = new List<CallDef>();

		public CodeVisualizer(string target, List<string> expandedScopes) {
			TargetFilePath = target;
			_expandedScopes = expandedScopes;
		}

		public string CompileDotLang() {
			_LoadAssembly();

			GraphData data = _LoadGraphData(); // =>CodeVisualizer-GraphData.cs

			return _CreateDotString(data); // =>CodeVisualizer-DotStrings.cs
		}

		private void _LoadAssembly() {
			AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(this.TargetFilePath);
			IEnumerable<TypeDefinition> types = assembly.Modules.SelectMany(m => m.GetTypes());
			foreach (TypeDefinition type in types) {
				foreach (MethodDefinition originatingMethod in type.Methods) {
					if (originatingMethod.HasBody && (!originatingMethod.IsConstructor || IncludeConstructors)) {
						foreach (Instruction il in originatingMethod.Body.Instructions) {
							if (il.OpCode == OpCodes.Call) {
								MethodReference calledMethod = il.Operand as MethodReference;

								_AddCallDef(originatingMethod, calledMethod);
							}
						}
					}
				}
			}
		}

		private void _AddCallDef(MethodDefinition from, MethodReference to) {
			CallDef cd = new CallDef(from, to);
			if (_callDefs.Any(c => c.From == cd.From && c.To == cd.To)) {
				return;
			}

			_callDefs.Add(cd);
		}

		private class CallDef {
			public MethodDefinition From { get; }
			public MethodReference To { get; }

			public CallDef(MethodDefinition from, MethodReference to) {
				From = from;
				To = to;
			}
		}
	}
}

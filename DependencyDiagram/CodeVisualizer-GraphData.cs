using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DotNetGraph;
using DotNetGraph.Attributes;
using Mono.Cecil;

namespace DependencyDiagram {
	public partial class CodeVisualizer {
		private GraphData _LoadGraphData() {
			GraphData data = new GraphData(_expandedScopes);

			data.ExpandAnyDotDllScope = ExpandAnyDotDllScope;

			foreach (CallDef call in _callDefs) {
				data.Add(call);
			}

			return data;
		}



		private class GraphData {
			public List<Scope> Scopes => _scopes;
			public List<External> Externals => _externals;
			public List<Scope.Type.Method> MethodIndex => _methodIndex;

			public bool ExpandAnyDotDllScope { get; set; }

			private readonly List<Scope> _scopes = new List<Scope>();
			private readonly List<External> _externals = new List<External>();
			private readonly List<string> _expandedScopes;
			private readonly List<Scope.Type.Method> _methodIndex = new List<Scope.Type.Method>();

			public GraphData(List<string> expandedScopes) {
				_expandedScopes = expandedScopes;
			}

			internal void Add(CallDef call) {
				Scope.Type.Method toMethod = _LoadDataByMethod(call.To);
				_LoadDataByMethod(call.From, toMethod);
			}

			internal void AddMethodToIndex(Scope.Type.Method method) {
				_methodIndex.Add(method);
			}

			private Scope.Type.Method _LoadDataByMethod(MethodReference method, Scope.Type.Method toMethod = null) {
				string scopeName = method.DeclaringType.Scope.Name;

				Scope scope;
				if (_expandedScopes.Contains(scopeName) || (ExpandAnyDotDllScope && (scopeName.Contains(".dll") || scopeName.Contains(".exe")))) {
					scope = _scopes.AddGetUnique(s => s.Identifier == scopeName, () => new Scope(method.DeclaringType.Scope.Name, this));
				} else {
					scope = _externals.AddGetUnique(e => e.Identifier == scopeName, () => new External(method.DeclaringType.Scope.Name, this));
				}

				return scope.AddMethod(method, toMethod);
			}

			internal class External : Scope {
				public External(string identifier, GraphData gd) : base(identifier, gd) { }
			}


			internal enum MethodDirection {
				From,
				To
			}

			internal class Scope {
				public List<Type> Types => _types;
				public string Identifier { get; }
				public GraphData Data { get; }

				private readonly List<Type> _types = new List<Type>();

				public Scope(string identifier, GraphData gd) {
					Identifier = identifier;
					Data = gd;
				}

				public Type.Method AddMethod(MethodReference method, Type.Method toMethod) {
					Type type = _types.AddGetUnique(t => t.Identifier == method.DeclaringType.FullName, () => new Type(method.DeclaringType.FullName, method.DeclaringType.Name, this));

					return type.AddMethod(method, toMethod);
				}

				internal class Type {
					public List<Method> Methods => _methods;
					public string Identifier { get; }
					public string Name { get; }
					public Scope Scope { get; }
					public TypeReference TypeRef { get; private set; }

					private readonly List<Method> _methods = new List<Method>();

					public Type(string identifier, string name, Scope scope) {
						Identifier = identifier;
						Name = name;
						Scope = scope;
					}

					public Method AddMethod(MethodReference method, Method toMethod) {
						Method newMethod = _methods.AddGetUnique(m => m.Identifier == method.FullName, () => new Method(method, this));
						TypeRef = method.DeclaringType;

						if (toMethod != null) {
							newMethod.LinkTo(toMethod);
						}

						return newMethod;
					}

					internal class Method {
						public string Identifier => MethodRef.FullName;
						public string Name => MethodRef.Name;
						public MethodReference MethodRef { get; }
						public Type Type { get; }
						public List<Method> LinkedTo { get; } = new List<Method>();

						public Method(MethodReference method, Type type) {
							MethodRef = method;
							Type = type;

							Type.Scope.Data.AddMethodToIndex(this);
						}

						internal void LinkTo(Method toMethod) {
							LinkedTo.Add(toMethod);
						}
					}
				}
			}
		}
	}
}

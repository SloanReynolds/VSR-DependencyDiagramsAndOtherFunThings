using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using DotNetGraph;
using DotNetGraph.Extensions;
using DotNetGraph.SubGraph;
using Mono.Cecil;
using DotNetGraph.Node;
using DotNetGraph.Edge;
using System;

namespace DependencyDiagram {
	public partial class CodeVisualizer {
		private List<GraphData.Scope.Type> _connectedTypes = null;

		private string _CreateDotString(GraphData data) {
			__Reset_CreateDotString();

			DotGraph dot = new DotGraph("dependency_diagram", true);
			dot.AddLine("rankdir = LR");

			//Expanded Scope Definitions
			foreach (GraphData.Scope scope in data.Scopes) {
				DotSubGraph sub = new DotSubGraph("cluster_" + scope.Identifier) {
					Style = DotSubGraphStyle.Filled,
					Color = Color.AliceBlue,
					Label = scope.Identifier
				};

				foreach (GraphData.Scope.Type type in scope.Types) {
					DotSubGraph sub2 = new DotSubGraph("cluster_" + type.Identifier) {
						Style = DotSubGraphStyle.Filled,
						Color = Color.CadetBlue,
						Label = type.Name
					};

					if (!SubgraphsByScope) {
						string prefix = type.TypeRef.Namespace;
						if (string.IsNullOrWhiteSpace(prefix)) {
							prefix = scope.Identifier;
						}
						sub2.Label = prefix + " " + type.Name;
					}

					foreach (GraphData.Scope.Type.Method method in type.Methods) {
						DotNode node = new DotNode(method.Identifier) {
							Style = DotNodeStyle.Filled,
							Color = Color.White,
							Label = method.Name
						};

						if (!_ShouldSkipMethod(method)) {
							sub2.Elements.Add(node);
						}
					}

					if (!_ShouldSkipType(type)) {
						if (SubgraphsByScope) {
							sub.Elements.Add(sub2);
						} else {
							dot.Elements.Add(sub2);
						}
					}
				}
				if (SubgraphsByScope) {
					dot.Elements.Add(sub);
				} else {
					//
				}
			}

			//External Scope Definitions
			if (!ShowOnlyExpanded) {
				foreach (GraphData.External external in data.Externals) {
					DotNode node = new DotNode(external.Identifier) {
						Style = DotNodeStyle.Filled,
						Color = Color.Yellow,
						Shape = DotNodeShape.Diamond
					};

					dot.Elements.Add(node);
				}
			}

			//Edges!
			foreach (GraphData.Scope.Type.Method fromMethod in data.MethodIndex.Where(m => m.LinkedTo.Count > 0)) {
				foreach (GraphData.Scope.Type.Method toMethod in fromMethod.LinkedTo) {
					if (_ShouldSkipMethod(fromMethod)
						|| _ShouldSkipMethod(toMethod)
						|| _ShouldSkipType(fromMethod.Type)
						|| _ShouldSkipType(toMethod.Type)) {
						continue;
					}

					DotEdge edge = null;
					if (toMethod.Type.Scope is GraphData.External) {
						if (!ShowOnlyExpanded) {
							edge = new DotEdge(fromMethod.Identifier, toMethod.Type.Scope.Identifier);
						}
					} else {
						edge = new DotEdge(fromMethod.Identifier, toMethod.Identifier);
					}
					if (edge != null) dot.Elements.Add(edge);
				}
			}

			return dot.Compile(true);

			void __Reset_CreateDotString() {
				_connectedTypes = null;
			}
		}

		private bool _ShouldSkipType(GraphData.Scope.Type type) {
			if (!ShowDisconnectedTypes) {
				__Init_ConnectedTypes();

				if (!_connectedTypes.Contains(type)) {
					return true;
				}
			}

			return false;

			void __Init_ConnectedTypes() {
				GraphData data = type.Scope.Data;
				if (_connectedTypes == null) {
					this._connectedTypes = new List<GraphData.Scope.Type>();

					foreach (var from in data.MethodIndex) {
						foreach (var to in from.LinkedTo) {
							if (from.Type != to.Type) {
								if (ShowOnlyExpanded && (from.Type.Scope is GraphData.External || to.Type.Scope is GraphData.External)) {
									//
								} else {
									_connectedTypes.AddUnique(from.Type);
									_connectedTypes.AddUnique(to.Type);
								}
							}
						}
					}
				}
			}
		}

		private bool _ShouldSkipMethod(GraphData.Scope.Type.Method method) {
			GraphData data = method.Type.Scope.Data;
			if (!ShowDisconnectedMethods) {
				List<GraphData.Scope.Type.Method> linked = method.LinkedTo.Where(
					m => ShowOnlyExpanded ? !(m.Type.Scope is GraphData.External) : true
				).ToList();

				if (linked.Count == 0 && !data.MethodIndex.Any(m => m.LinkedTo.Contains(method)))
					return true;
			}

			return false;
		}
	}
}

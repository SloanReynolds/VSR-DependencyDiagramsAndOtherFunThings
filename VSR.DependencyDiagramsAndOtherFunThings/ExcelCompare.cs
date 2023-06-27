using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using ClosedXML.Excel;
using DapperLogic;
using DocumentFormat.OpenXml.Vml.Spreadsheet;
using ExcelDataReader;
using ItemRandomizer;
using Newtonsoft.Json;
using VSR.DependencyDiagramsAndOtherFunThings;

namespace ExcelLogicBridge {
	class ExcelCompare {
		private const string SHEET_PATH = @"E:\OneDrive\Documents\VSR Logic.xlsx";
		private const string LOCATIONS_PATH = @"C:\Users\sloan\source\repos\VSR.ItemRandomizer\ItemRandomizer\Data\Logic\locations.json";
		private const string MACROS_PATH = @"C:\Users\sloan\source\repos\VSR.ItemRandomizer\ItemRandomizer\Data\Logic\macros.json";
		private const string VSRIR_PATH = @"http://www.lanimals.com/vsrir/?raw=true&type=OutOfLogic";

		internal List<ExcelData.LocationRow> Locations = new List<ExcelData.LocationRow>();
		private List<ExcelData.MacroRow> _macros = new List<ExcelData.MacroRow>();
		private DateTime _latestImport;

		private List<string> _macrosUsed = new List<string>();

		private static WebClient _http = new WebClient();

		public void ParseExcelSheet() {
			using (var stream = File.Open(SHEET_PATH, FileMode.Open, FileAccess.Read)) {
				using (var reader = ExcelReaderFactory.CreateReader(stream)) {
					ExcelData data = new ExcelData(reader);

					//Macros
					_ParseMacros(data.Macros);

					//Locations
					_ParseLocations(data.Locations);

					_latestImport = Locations.OrderByDescending(l => l.LastImported).FirstOrDefault().LastImported;

					//Console.WriteLine("Finished Writing Logic!");

					foreach (var item in _macros.Select(m => m.RandoMacro)) {
						if (!_macrosUsed.Contains(item.Name)) {
							Console.WriteLine($"!! Unused Macro! {item.Name}");
							Console.ReadKey();
						}
					}
				}
			}
		}

		internal void ForceAddSymbol(string symbol, bool isOr = false) {
			foreach (ExcelData.LocationRow loc in Locations) {
				if (loc.LogicString.Trim() != "") {
					loc.LogicString = $"{symbol} {(isOr ? "|" : "&")} ({loc.LogicString})";
				}
			}
		}

		internal void ImportToExcel() {
			XLWorkbook book = new XLWorkbook(SHEET_PATH);
			foreach (ExcelData.ExcelRow.Diff diff in Locations.Where(l => l.HasDiffs).SelectMany(l => l.Diffs)) {
				var sheet = book.Worksheet(diff.SheetNum);
				sheet.Cell(diff.RowNum, diff.ColNum).Value = diff.Value;
			}
			book.SaveAs(SHEET_PATH);
		}

		private string _GetPuzzleOption(bool enabled = false) {
			return enabled ? "opt_randomized_puzzles" : "!opt_randomized_puzzles";
		}

		public void DownloadFromLanimals() {
			string result = _http.DownloadString(VSRIR_PATH);

			List<LanimalsData.LocationRow> lanimalsLocations = _GetLocationRowsFromResult(result);

			foreach (var lanimalsRow in lanimalsLocations) {
				if (lanimalsRow.DateTime <= _latestImport) {
					//Skip old data
					continue;
				}

				//Get Excel Current Logic
				var excelRows = Locations.Where(l => l.Scene == lanimalsRow.Scene && l.GameObject == lanimalsRow.GameObject);
				if (excelRows.Count() != 1) {
					throw new Exception("Something Problem Loction.");
				}
				ExcelData.LocationRow excelRow = excelRows.First();
				excelRow.AddLogic(lanimalsRow.Logic);
			}

			{ //Simplify Updated Rows
				int count = 0;
				foreach (var excelRow in Locations.Where(l => l.NeedsSimplify)) {
					excelRow.SimplifyLogic();
					count++;
				}

				Console.WriteLine($"{count} locations updated!");
			}
		}

		internal static List<LanimalsData.LocationRow> AllLanimalsRowsForIDStr(string idStr) {
			do {
				try {
					string result = _http.DownloadString(VSRIR_PATH + "&idstr=" + idStr);

					return _GetLocationRowsFromResult(result);
				} catch (Exception ex) {
					Console.WriteLine(ex.Message + "... I sleep");
					Thread.Sleep(500);
				}
			} while (true);
		}

		internal static string SimplifyFromList(List<string> newLogics) {
			string newLogic = BuildNewLogicStr(newLogics);
			return SimplifyFromString(newLogic);
		}

		internal static string SimplifyFromString(string newLogic) {
			string postfix = InfixHelper.ToPostfix(newLogic);
			string replaced = LogicHelper.ReplaceMacros(postfix);
			return PositiveLogicMinimizer.FullyFlattenPostfix(replaced, "opt_randomized_puzzles", "!opt_randomized_puzzles");
		}

		public static string BuildNewLogicStr(List<string> list) {
			if (list.Count == 0) throw new Exception("Something About Not Enough Minerals maybe there's idk man stupid...");
			if (list.Contains("")) return "";

			string newStr = $"({list[0]})";
			for (int i = 1; i < list.Count; i++) {
				newStr += $" | ({list[i]})";
			}

			return newStr;
		}

		

		private static List<LanimalsData.LocationRow> _GetLocationRowsFromResult(string result) {
			List<LanimalsData.LocationRow> locations = new List<LanimalsData.LocationRow>();
			string[] lines = result.Replace("\r\n", "\n").Split('\n');
			foreach (var line in lines) {
				if (line.Trim() != "") locations.Add(new LanimalsData.LocationRow(HttpUtility.ParseQueryString(line)));
			}

			return locations;
		}

		private void _PopulateLocations(Dictionary<string, string> lanimalData) {
			foreach (KeyValuePair<string, string> item in lanimalData) {
				string[] locationPart = item.Key.Split(':');
				var jLocs = Locations.Where(l => l.Scene == locationPart[0] && l.GameObject == locationPart[1]);
				if (jLocs.Count() != 1) {
					throw new Exception("Something Problem Loction.");
				}
				ExcelData.LocationRow jLoc = jLocs.First();

				string newInfix = jLoc.LogicString.Trim() == "" ? item.Value : $"({PostfixHelper.ToInfix(jLoc.RandoLocation.Logic.PostfixAfterMacros)}) | ({item.Value})";
				string simplified = string.Join(" ", PositiveLogicMinimizer.FullyFlatten(newInfix, "opt_randomized_puzzles", "!opt_randomized_puzzles"));

				jLoc.AutoMagic = jLoc.LogicString;
				jLoc.LogicString = simplified;
				jLoc.LastImported = DateTime.Now;
			}
		}

		private string _MakeLineLogic(string decryptors, bool puzzleOption) {
			string retVal = $"{_GetPuzzleOption(puzzleOption)}";
			if (decryptors != "") {
				retVal += $" & {decryptors.Replace(",", " & ")}";
			}

			return retVal;
		}

		private void _ParseLocations(List<ExcelData.LocationRow> excel) {
			for (int i = 0; i < excel.Count; i++) {
				var loc = excel[i];

				_ValidateTokens(loc.LogicString);

				Location jLoc = new Location(loc.Number, loc.Area, loc.Scene, loc.MapX, loc.MapY, loc.GameObject, loc.IDString, loc.LogicString);
				loc.RandoLocation = jLoc;
				Locations.Add(loc);
			}
		}

		public void ExportToProjectJSON() {
			_SerializeAndWrite(Locations.Select(l => l.RandoLocation), LOCATIONS_PATH);
			_SerializeAndWrite(_macros.Select(m => m.RandoMacro), MACROS_PATH);
		}

		private void _ParseMacros(List<ExcelData.MacroRow> excel) {
			for (int i = 0; i < excel.Count; i++) {
				var mac = excel[i];

				_ValidateTokens(mac.LogicString);

				Macro jMac = new Macro(mac.Name, mac.LogicString);
				mac.RandoMacro = jMac;
				_macros.Add(mac);
			}
		}

		private void _ValidateTokens(string logicString) {
			var list = logicString.Replace("(", "").Replace(")", "").Replace("?", "").Split(' ');
			foreach (string item in list) {
				var str = item.Trim();
				if (str.StartsWith("!")) str = str.Substring(1);
				if (str != "" && str != "|" && str != "&" && str != "+") {
					if (_macros.Any(m => m.RandoMacro.Name == str)) {
						//we good
						_macrosUsed.Add(str);
					} else if (Definitions.ValidTokens.Contains(str)) {
						//still good
					} else {
						switch (str) {
							case "cards>23":
							case "hearts>3":
							case "phase>3":
							case "orbs>3":
							case "opt_randomized_puzzles":
								break;
							default:
								throw new Exception($"Invalid token found! '{str}'");
						}
					}
				}
			}
		}

		private void _SerializeAndWrite(object obj, string path) {
			FileInfo fi = new FileInfo(path);
			string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
			Console.WriteLine(json);
			Console.WriteLine();
			Console.WriteLine($"Save this ({json.Length} bytes) to {path} ({(fi.Exists ? fi.Length : 0)} bytes)? (y/n)");
			if (Console.ReadKey().Key == ConsoleKey.Y) {
				File.WriteAllText(path, json);
				Console.WriteLine($"New Locations Data => {path}");
				Console.WriteLine("File Saved!");
			} else {
				Console.WriteLine("Fine, nothing was written.");
			}
		}
	}
}
